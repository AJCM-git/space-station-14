using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.UserInterface.Systems.Onboarding.Controls;

[GenerateTypedNameReferences]
public sealed partial class OnboardingCategorySelector : OptionButton
{
    public int DefaultIndex = -1;

    public OnboardingCategorySelector()
    {
        RobustXamlLoader.Load(this);
        var controller = UserInterfaceManager.GetUIController<OnboardingUIController>();
        var userLevel = controller.GetUserHelpLevel();
        var protos = controller.GetOnboardingCategoryPrototypes();
        for (var i = 0; i < protos.Count; i++)
        {
            var proto = protos[i];

            AddItem(proto.Data.Name, i);
            SetItemMetadata(i, proto);

            if (proto.Data.HelpLevel == userLevel)
            {
                DefaultIndex = i;
                Select(i);
            }
        }

        OnItemSelected += args => SelectId(args.Id);
    }
}
