using Content.Client.Audio;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Onboarding;
using Content.Client.Voting;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Lobby;

public sealed class PreLobbyState : Robust.Client.State.State
{
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;

    protected override void Startup()
    {
        if (_userInterfaceManager.ActiveScreen == null)
            return;

        var onboardingController = _userInterfaceManager.GetUIController<OnboardingUIController>();
        onboardingController.OpenSelector();

        _voteManager.SetPopupContainer(Lobby.VoteContainer);
        LayoutContainer.SetAnchorPreset(Lobby, LayoutContainer.LayoutPreset.Wide);
    }

    protected override void Shutdown()
    {
    }
}
