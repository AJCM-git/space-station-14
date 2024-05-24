using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.MassMedia.Components;

///<summary>
/// The state of the messages app interface.
///</summary>
[Serializable, NetSerializable]
public sealed class MessengerUiState : BoundUserInterfaceState;

[Serializable, NetSerializable]
public sealed class MessengerSendMessageEvent(MessengerMessageData message) : CartridgeMessageEvent
{
    /// <summary>
    /// The list of all users and messages the UI currently has available
    /// </summary>
    public MessengerMessageData Message = message;
}

///<summary>
/// Data of a message in the system, contains the ids of the receiver and owner, the text content and the time it was sent.
///</summary>
[Serializable, NetSerializable]
[DataRecord]
public record struct MessengerMessageData(int UserId, int ReceiverId, int MessageId, string Message, TimeSpan Time);

///<summary>
/// Data of a user profile
///</summary>
/// <remarks>We use int instead of NetEntity because the only thing the client will do with this is display it as a string</remarks>
[Serializable, NetSerializable]
[DataRecord]
public record struct MessengerProfileData(int UserId, string UserName, SpriteSpecifier JobIcon, string LastMessage, bool Hide = false);
