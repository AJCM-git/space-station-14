using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Onboarding.Controls;
using Content.Shared.CCVar;
using Content.Shared.Onboarding;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Client.State;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Onboarding;

public sealed class OnboardingUIController : UIController
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IStateManager _state = default!;

    private FancyWindow? _levelSelectorWindow;

    public override void Initialize()
    {
        base.Initialize();

        _client.PlayerJoinedServer += OnJoinedServer;
        _state.OnStateChanged += OnStateChanged;
    }

    private void OnStateChanged(StateChangedEventArgs obj)
    {
        throw new NotImplementedException();
    }

    private void OnJoinedServer(object? sender, PlayerEventArgs e)
    {
        if (!_cfg.GetCVar(CCVars.OnboardingEnabled))
            return;

        if (_prototype.TryIndex<OnboardingCategoryPrototype>(_cfg.GetCVar(CCVars.OnboardingUserLevel), out var level))
            return;

        _levelSelectorWindow = new() { MinSize = new(400, 70) };
        var selectorControl = new OnboardingSelector();
        _levelSelectorWindow.ContentsContainer.AddChild(selectorControl);
        _levelSelectorWindow.OpenCentered();
    }

    public List<OnboardingCategoryPrototype> GetOnboardingCategoryPrototypes()
    {
        int Compare(int x, int y) { return x > y ? -1 : 1;}

        var categoryProtos = _prototype.EnumeratePrototypes<OnboardingCategoryPrototype>().ToList();
        categoryProtos.Sort((x,y) => Compare(x.Data.HelpLevel, y.Data.HelpLevel));
        return categoryProtos;
    }

    public void ChangeConfiguration(ProtoId<OnboardingCategoryPrototype> containedCategory)
    {
        _cfg.SetCVar(CCVars.OnboardingUserLevel, containedCategory);
        _cfg.SaveToFile();
    }
}

internal sealed class UITest3Command : LocalizedCommands
{
    public override string Command => "uitest3";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var popup = new Popup() { MinSize = new(500, 500), CloseOnClick = false, CloseOnEscape = false, Visible = true};
        var control = new OnboardingSelector();
        popup.AddChild(control);

    }
}
