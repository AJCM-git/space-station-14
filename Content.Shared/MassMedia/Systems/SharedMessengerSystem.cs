namespace Content.Shared.MassMedia.Systems;

public abstract class SharedMessengerSystem : EntitySystem
{
    public const int MessengerUpdateRate = 5;

    public const int MessagePreviewLimit = 30;
    public const int MessageLengthLimit = 128;
}
