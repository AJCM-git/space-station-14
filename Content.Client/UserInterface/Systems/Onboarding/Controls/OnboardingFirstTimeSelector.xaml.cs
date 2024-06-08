using Content.Shared.Onboarding;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Onboarding.Controls;

[GenerateTypedNameReferences]
public sealed partial class OnboardingFirstTimeSelector : BoxContainer
{
    public OnboardingFirstTimeSelector()
    {
        RobustXamlLoader.Load(this);

        Message.SetMessage(FormattedMessage.FromMarkupPermissive(Loc.GetString("onboarding-selector-message")));

        ConfirmButton.OnPressed += _ =>
        {
            if (CategorySelector.GetItemMetadata(CategorySelector.SelectedId) is not OnboardingCategoryPrototype onboardCategory)
                return;

            var controller = UserInterfaceManager.GetUIController<OnboardingUIController>();
            controller.ChangeConfiguration(onboardCategory.Data);
            Dispose();
        };

    }
}
