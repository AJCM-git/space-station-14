using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.DeviceNetwork.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Server.Radio.Components;
using Content.Server.GameTicking;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessengerSystem : SharedMessengerSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing= default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string UnknownJobIcon = "JobIconUnknown";

    private const string ClientSendProfile = "ClientSendProfile";
    private const string ClientSendMessage = "ClientSendMessage";

    private const string ServerSendProfiles = "SendProfileData";
    private const string ServerSendMessage = "ServerSendMessage";

    public override void Initialize()
    {
        SubscribeLocalEvent<MessengerClientComponent, MapInitEvent>(OnClientMapInit);
        SubscribeLocalEvent<MessengerClientComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessengerClientComponent, DeviceNetworkPacketEvent>(OnPacketReceivedClient);

        SubscribeLocalEvent<MessengerServerComponent, MapInitEvent>(OnServerMapInit);
        SubscribeLocalEvent<MessengerServerComponent, DeviceNetworkPacketEvent>(OnPacketReceivedServer);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MessengerClientComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            var curTime = _timing.CurTime;
            if (curTime < comp.RefreshCooldownEnd)
                continue;

            comp.RefreshCooldownEnd = curTime + comp.RefreshCooldownLength;

            if (cartridge.LoaderUid is not {} pdaUid)
                continue;

            if (_stationSystem.GetOwningStation(uid) is not { } stationId ||
                !_singletonServerSystem.TryGetActiveServerAddress<MessengerServerComponent>(stationId, out var address))
            {
                comp.Error = true;
                Dirty(uid, comp);
                UpdateUiState((uid, comp), pdaUid);
                continue;
            }

            var packet = new NetworkPayload()
            {
                [ClientSendProfile] = comp.UserProfile.UserId
            };
            _deviceNetworkSystem.QueuePacket(uid, address, packet);

            Dirty(uid, comp);
            UpdateUiState((uid, comp), pdaUid);
        }
    }


    #region Messenger Client

    private void OnClientMapInit(Entity<MessengerClientComponent> ent, ref MapInitEvent args)
    {
        var userId = (int) ent.Owner;
        var nameAndIcon = GetUserData(ent, userId);
        ent.Comp.UserProfile = new(userId, nameAndIcon.UserName, nameAndIcon.JobIcon, string.Empty);
    }

    private void OnUiMessage(Entity<MessengerClientComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not MessengerSendMessageEvent sendMessageEvent)
            return;

        if (_stationSystem.GetOwningStation(ent) is not { } stationId ||
            !_singletonServerSystem.TryGetActiveServerAddress<MessengerServerComponent>(stationId, out var address))
        {
            ent.Comp.Error = true;
            return;
        }

        if (ent.Comp.UserProfile.UserId != sendMessageEvent.Message.UserId)
            return;

        sendMessageEvent.Message.Time = _gameTicker.RoundDuration();
        ent.Comp.CachedMessages.Add(sendMessageEvent.Message);

        var packet = new NetworkPayload()
        {
            [ClientSendMessage] = sendMessageEvent.Message
        };
        _deviceNetworkSystem.QueuePacket(ent, address, packet);
    }

    /// <summary>
    /// React and respond to packets from the server
    /// </summary>
    private void OnPacketReceivedClient(Entity<MessengerClientComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (Comp<CartridgeComponent>(ent).LoaderUid is not { } loaderUid)
            return;

        var ourUserId= ent.Comp.UserProfile.UserId;
        if (args.Data.TryGetValue<MessengerProfileData>(ServerSendProfiles, out var receivedProfile))
        {
            // filter our own user profile and update it
            if (receivedProfile.UserId == ourUserId)
            {
                ent.Comp.UserProfile = receivedProfile;
                return;
            }

            ent.Comp.Error = false;
            ent.Comp.CachedProfiles.Add(receivedProfile);
            return;
        }

        if (args.Data.TryGetValue<MessengerMessageData>(ServerSendMessage, out var receivedMessage))
        {
            for (var i = 0; i < ent.Comp.CachedProfiles.Count; i++)
            {
                var profile = ent.Comp.CachedProfiles[i];
                if (profile.UserId == receivedMessage.UserId)
                {
                    profile.LastMessage = receivedMessage.Message;
                    ent.Comp.CachedProfiles[i] = profile;
                    break;
                }
            }

            var subtitleString = $"New message from: #{receivedMessage.UserId}";
            _cartridgeLoaderSystem.SendNotification(loaderUid, subtitleString, receivedMessage.Message);
            ent.Comp.Error = false;
            ent.Comp.CachedMessages.Add(receivedMessage);
            return;
        }
    }

    #endregion

    #region Messenger Server

    private void OnServerMapInit(Entity<MessengerServerComponent> ent, ref MapInitEvent args)
    {
        if (_stationSystem.GetOwningStation(ent) is not { } station)
            return;

        EnsureComp<StationMessengerComponent>(station);
        ent.Comp.MainServer = station;
    }

    /// <summary>
    /// Reacts to packets received from clients
    /// </summary>
    private void OnPacketReceivedServer(Entity<MessengerServerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (ent.Comp.MainServer is not { } mainServer)
            return;

        if (!_singletonServerSystem.IsActiveServer(ent))
            return;

        var mainServerComp = Comp<StationMessengerComponent>(mainServer);
        if (args.Data.TryGetValue<int>(ClientSendProfile, out var clientId))
        {
            var nameAndIcon = GetUserData(new(clientId), clientId);
            MessengerProfileData userData = new(clientId, nameAndIcon.UserName, nameAndIcon.JobIcon, string.Empty);
            if (!TryAddProfile(userData, mainServerComp))
                return;

            var packet = new NetworkPayload()
            {
                [ServerSendProfiles] = userData
            };
            _deviceNetworkSystem.QueuePacket(ent, null, packet);
            return;
        }

        if (args.Data.TryGetValue<MessengerMessageData>(ClientSendMessage, out var message))
        {
            message.Time = _gameTicker.RoundDuration();
            message.MessageId = mainServerComp.Messages.Count+1;
            mainServerComp.Messages.Add(message);
            var packet = new NetworkPayload()
            {
                [ServerSendMessage] = message
            };
            var address = Comp<DeviceNetworkComponent>(new(message.ReceiverId)).Address;
            _deviceNetworkSystem.QueuePacket(ent, address, packet);
            return;
        }
    }
    #endregion

    /// <summary>
    /// Returns the user's name and job icon
    /// </summary>
    private (string UserName, SpriteSpecifier JobIcon) GetUserData(EntityUid clientUid, int userId)
    {
        var userName = Loc.GetString("generic-unknown-title");

        var unknownJobIcon = _prototype.Index<StatusIconPrototype>(UnknownJobIcon).Icon;
        if (Comp<CartridgeComponent>(clientUid).LoaderUid is not { } pdaUid)
            return (userName + $" #{userId}", unknownJobIcon);

        if (Comp<PdaComponent>(pdaUid).ContainedId is not { } idCard)
            return (userName + $" #{userId}", unknownJobIcon);

        var idCardComp = Comp<IdCardComponent>(idCard);
        if (!string.IsNullOrEmpty(idCardComp.FullName))
            userName = idCardComp.FullName + $" ({idCardComp.JobTitle})" + $" #{userId}";
        else
            userName += $" ({idCardComp.JobTitle})" + $" #{userId}";

        var sprite = _prototype.Index<StatusIconPrototype>(idCardComp.JobIcon).Icon;
        return (userName, sprite);
    }

    private bool TryAddProfile(MessengerProfileData userProfile, StationMessengerComponent mainServer)
    {
        var failed = false;
        for (var i = 0; i < mainServer.Profiles.Count; i++)
        {
            var profile = mainServer.Profiles[i];
            // check if we already have added this profile
            if (profile.UserId != userProfile.UserId)
                continue;

            // check if there is information to update
            if (profile.UserName == userProfile.UserName &&
                profile.JobIcon == userProfile.JobIcon &&
                profile.Hide == userProfile.Hide)
            {
                failed = true;
                continue;
            }

            // update existing profiles information
            mainServer.Profiles[i] = userProfile;
            return true;
        }

        if (failed)
            return false;

        mainServer.Profiles.Add(userProfile);
        return true;
    }

    private void UpdateUiState(Entity<MessengerClientComponent> ent, EntityUid loaderUid)
    {
        var state = new MessengerUiState();
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

    private bool TryGetProfile(int userId, StationMessengerComponent mainServer, [NotNullWhen(true)] out MessengerProfileData? userProfile)
    {
        foreach (var profile in mainServer.Profiles)
        {
            if (profile.UserId != userId || profile.Hide)
                continue;

            userProfile = profile;
            return true;
        }

        userProfile = null;
        return false;
    }

    private List<MessengerProfileData> GetAllProfiles(StationMessengerComponent mainServer)
    {
        var list = new List<MessengerProfileData>();
        foreach (var profile in mainServer.Profiles)
        {
            if (profile.Hide)
                continue;

            list.Add(profile);
        }

        return list;
    }

    private List<MessengerMessageData> GetAllMessages(int userId, int? receiverId, StationMessengerComponent mainServer)
    {
        var list = new List<MessengerMessageData>();
        foreach (var message in mainServer.Messages)
        {
            if (receiverId != null && message.UserId == userId && message.ReceiverId == receiverId)
            {
                list.Add(message);
                continue;
            }

            // if receiver is null, get all the messages that this userId owns or received
            if (message.UserId == userId || message.ReceiverId == userId)
                list.Add(message);
        }

        return list;
    }
}
