using Robust.Shared.Prototypes;

namespace Content.Shared.Onboarding;

/// <summary>
/// This is a prototype for categorizing the many onboardings by their usefulness for new players
/// </summary>
[Prototype]
public sealed partial class OnboardingCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public OnboardingCategoryData Data;
}

[DataRecord]
public partial record struct OnboardingCategoryData(string Name, int HelpLevel);
