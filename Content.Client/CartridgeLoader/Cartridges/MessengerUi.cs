using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.MassMedia.Components;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class MessengerUi : UIFragment
{
    private MessengerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (fragmentOwner is not { } owner)
            return;

        _fragment = new MessengerUiFragment(owner);
        _fragment.OnMessageSent += (msg, ourId, receiverId) =>
        {
            var messagesMessage = new MessengerSendMessageEvent(new(ourId, receiverId, 0, msg, TimeSpan.Zero));
            var message = new CartridgeUiMessage(messagesMessage);
            userInterface.SendMessage(message);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessengerUiState uiState)
            return;

        _fragment?.UpdateState(uiState);
    }
}
