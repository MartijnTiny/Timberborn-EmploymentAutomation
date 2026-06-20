using EmploymentAutomation.Logic;
using Timberborn.CoreUI;
using Timberborn.Localization;
using Timberborn.TooltipSystem;

namespace EmploymentAutomation.UI;

public class PowerAutomationFragment(
    ILoc loc,
    VisualElementInitializer initializer,
    ITooltipRegistrar tooltipRegistrar) : BaseAutomationFragment<PowerComponent>(loc, initializer, tooltipRegistrar)
{
    protected override string GetToggleLocalizationKey() => "Ximsa.EmploymentAutomation.PowerToggle";

    protected override string GetComponentName() => "Power";
}
