using System.Linq;
using EmploymentAutomation.Logic;
using Timberborn.CoreUI;
using Timberborn.GameDistricts;
using Timberborn.GoodsUI;
using Timberborn.Localization;
using Timberborn.TooltipSystem;
using Timberborn.Workshops;

namespace EmploymentAutomation.UI;

public class IngredientAutomationFragment(
    ILoc loc,
    VisualElementInitializer initializer,
    ITooltipRegistrar tooltipRegistrar,
    GoodDescriber goodDescriber) : BaseAutomationFragment<IngredientComponent>(loc, initializer, tooltipRegistrar)
{
    protected override string GetToggleLocalizationKey() => "Ximsa.EmploymentAutomation.IngredientToggle";

    protected override string GetComponentName() => "Ingredient";

    protected override void SetupTooltips(IngredientComponent component)
    {
        base.SetupTooltips(component);

        var manufactory = component.GetComponent<Manufactory>();
        var districtBuilding = component.GetComponent<DistrictBuilding>();

        if (manufactory?.CurrentRecipe?.Ingredients == null || districtBuilding == null)
            return;

        var ingredientIds = manufactory.CurrentRecipe.Ingredients.Select(i => i.Id).ToList();
        StorageTooltipHelper.AddIngredientStorageTooltip(panel, districtBuilding, ingredientIds, TooltipRegistrar, goodDescriber);
    }
} 
