using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Onboarding.Controls;
using Content.Shared.CCVar;
using Content.Shared.Onboarding;
using Robust.Client;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Onboarding;

public sealed class OnboardingUIController : UIController
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        _client.PlayerJoinedServer += OnJoinedServer;
    }

    private void OnJoinedServer(object? sender, PlayerEventArgs e)
    {
        if (!_cfg.GetCVar(CCVars.OnboardingEnabled))
            //|| GetUserHelpLevel() != -1)
            return;

        OpenSelector();
    }

    public List<OnboardingCategoryPrototype> GetOnboardingCategoryPrototypes()
    {
        int Compare(int x, int y) { return x > y ? -1 : 1;}

        var categoryProtos = _prototype.EnumeratePrototypes<OnboardingCategoryPrototype>().ToList();
        categoryProtos.Sort((x,y) => Compare(x.Data.HelpLevel, y.Data.HelpLevel));
        return categoryProtos;
    }

    private void OpenSelector()
    {
        var control = new OnboardingFirstTimeSelector();
        UIManager.WindowRoot.AddChild(control);
        LayoutContainer.SetAnchorPreset(control, LayoutContainer.LayoutPreset.Wide);
    }

    public void ChangeConfiguration(OnboardingCategoryData containedCategory)
    {
        _cfg.SetCVar(CCVars.OnboardingHelpLevel, containedCategory.HelpLevel);
        _cfg.SaveToFile();
    }

    public int GetUserHelpLevel()
    {
        return _cfg.GetCVar(CCVars.OnboardingHelpLevel);
    }
}

internal sealed class UITest3Command : LocalizedCommands
{
    public override string Command => "uitest3";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var popup = new Popup() { MinSize = new(500, 500), CloseOnClick = false, CloseOnEscape = false, Visible = true};
        var control = new OnboardingCategorySelector();
        popup.AddChild(control);

    }
}
