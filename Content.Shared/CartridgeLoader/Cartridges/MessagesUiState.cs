﻿using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessagesUiState(MessagesUiStateMode mode, List<(string, int?)> contents = null, string? name = null) : BoundUserInterfaceState
{
    public List<(string, int?)>? Contents = contents;
    public MessagesUiStateMode Mode = mode;
    public string? Name = name;
}

[Serializable, NetSerializable]
public enum MessagesUiStateMode
{
    UserList,
    Chat
}

[Serializable, NetSerializable]
public partial struct MessagesMessageData
{
    public int SenderId;
    public int ReceiverId;
    public string Content;
    public TimeSpan Time;
}

[Serializable, NetSerializable]
public enum MessagesKeys
{
    Nanotrasen,
    Syndicate
}
