﻿// TODO Validate on server, sending messages has a lot of latency, dirtying with the component and the UI is fucking cursed, make the vertical scrollbar go to the bottom...

using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Robust.Shared.Utility;

namespace Content.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class MessengerUiFragment : BoxContainer
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public event Action<string, int, int>? OnMessageSent;

    private readonly Entity<MessengerClientComponent> _owner;
    /// <summary>
    /// User id of whoever we are currently chatting with
    /// </summary>
    private MessengerProfileData? _receiverUserProfile;
    private bool _messageLimitReached;

    public MessengerUiFragment()
    {
        RobustXamlLoader.Load(this);
    }

    public MessengerUiFragment(EntityUid owner)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        var comp = _entManager.GetComponent<MessengerClientComponent>(owner);
        _owner = (owner, comp);

        // Profile list setup
        FetchingLabel.SetMessage(FormattedMessage.FromMarkup(Loc.GetString("messenger-waiting-state")));
        ErrorLabel.SetMessage(FormattedMessage.FromMarkup(Loc.GetString("messenger-error-state")));

        SearchBar.OnTextChanged += search => FilterContacts(search.Text);
        ReturnButton.OnPressed += _ =>
        {
            _receiverUserProfile = null;
            ContactsContainer.Visible = true;
            ChatContainer.Visible = false;
        };

        // Message list setup
        MessageInput.HidePlaceHolderOnFocus = true;
        MessageInput.OnTextChanged += msg =>
        {
            if (msg.Text.Length >= SharedMessengerSystem.MessageLengthLimit)
            {
                MessageInput.Modulate = Color.Red;
                _messageLimitReached = true;
            }
            else
            {
                MessageInput.Modulate = Color.White;
                _messageLimitReached = false;
            }
        };
        MessageInput.OnTextEntered += msg =>
        {
            if (_messageLimitReached)
                return;

            var text = msg.Text;
            if (text.Length >= SharedMessengerSystem.MessageLengthLimit)
                text = $"{msg.Text[..SharedMessengerSystem.MessageLengthLimit]}-";

            if (_receiverUserProfile != null)
                OnMessageSent?.Invoke(text, _owner.Comp.UserProfile.UserId, _receiverUserProfile.Value.UserId);
            SwitchMessageInput(false);
            MessageInput.Clear();
        };
    }

    public void UpdateState(MessengerUiState messagesState)
    {
        PopulateProfiles();
        ErrorLabel.Visible = _owner.Comp.Error;
        FetchingLabel.Visible = false;
        FilterContacts(SearchBar.Text);

        OurUser.Text = Loc.GetString("messenger-user-label", ("userName", _owner.Comp.UserProfile.UserName));
    }

    private void PopulateProfiles()
    {
        ContactList.RemoveAllChildren();

        if (_owner.Comp.CachedProfiles.Count == 0)
            return;

        foreach (var (userId, userName, sprite, lastMessage, _) in _owner.Comp.CachedProfiles)
        {
            var contactEntry = new MessengerContactEntry(userName, sprite, lastMessage);
            contactEntry.OnPressed += _ =>
            {
                _receiverUserProfile = new(userId, userName, sprite, lastMessage);

                UserName.Text = contactEntry.UserName.Text;
                ContactsContainer.Visible = false;
                ChatContainer.Visible = true;
                MessagesScrollbar.SetScrollbarPosition(MessagesScrollbar.Size);
                PopulateMessages();
            };
            ContactList.AddChild(contactEntry);
        }

        PopulateMessages();
    }

    private void PopulateMessages()
    {
        if (_receiverUserProfile == null)
            return;

        MessagesContainer.RemoveAllChildren();
        SwitchMessageInput(true);
        var ourUserId = _owner.Comp.UserProfile.UserId;
        // TODO note: assume the list is always ordered by the time of the message AND that ProfilesToSend dont include OurProfile
        foreach (var message in _owner.Comp.CachedMessages)
        {
            if (message == null)
                continue;

            // FAIL
            if (!IsMessageForContact(ourUserId, message.Value.UserId, _receiverUserProfile.Value.UserId, message.Value.ReceiverId))
                continue;

            var messageEntry = new MessengerMessageEntry(_owner.Comp.UserProfile.JobIcon, message.Value.Message, message.Value.Time, true);
            if (message.Value.UserId != ourUserId)
                messageEntry = new MessengerMessageEntry(_receiverUserProfile.Value.JobIcon, message.Value.Message, message.Value.Time, false);
            MessagesContainer.AddChild(messageEntry);
        }
    }

    private void FilterContacts(string searchBarText)
    {
        foreach (var uncastedChildren in ContactList.Children)
        {
            var children = (MessengerContactEntry) uncastedChildren;
            children.Visible = children.UserName.Text != null &&
                               children.UserName.Text.Contains(searchBarText, StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool IsMessageForContact(int ourUserId, int messageOwnerId, int receiverUserId, int  messageReceiverId)
    {
        var ourMessage = messageOwnerId == ourUserId && messageReceiverId == receiverUserId;
        var receiverMessage = messageOwnerId == receiverUserId && messageReceiverId == ourUserId;
        return receiverMessage || ourMessage;
    }

    private void SwitchMessageInput(bool enable)
    {
        MessageInput.Editable = enable;
        MessageInput.CanKeyboardFocus = enable;
        MessageInput.PlaceHolder = enable ? Loc.GetString("messenger-message-placeholder") : Loc.GetString("messenger-waiting-message");
    }
}