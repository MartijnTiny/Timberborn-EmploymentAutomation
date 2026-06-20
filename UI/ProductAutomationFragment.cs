using System.Linq;
using EmploymentAutomation.Logic;
using Timberborn.CoreUI;
using Timberborn.GameDistricts;
using Timberborn.GoodsUI;
using Timberborn.Localization;
using Timberborn.TooltipSystem;
using Timberborn.Workshops;

namespace EmploymentAutomation.UI;

public class ProductAutomationFragment(
    ILoc loc,
    VisualElementInitializer initializer,
    ITooltipRegistrar tooltipRegistrar,
    GoodDescriber goodDescriber) : BaseAutomationFragment<ProductComponent>(loc, initializer, tooltipRegistrar)
{
    protected override string GetToggleLocalizationKey() => "Ximsa.EmploymentAutomation.ProductToggle";

    protected override string GetComponentName() => "Product";

    protected override void SetupTooltips(ProductComponent component)
    {
        base.SetupTooltips(component);

        var manufactory = component.GetComponent<Manufactory>();
        var districtBuilding = component.GetComponent<DistrictBuilding>();

        if (manufactory?.CurrentRecipe?.Products == null || districtBuilding == null)
            return;

        var productIds = manufactory.CurrentRecipe.Products.Select(p => p.Id).ToList();
        StorageTooltipHelper.AddProductStorageTooltip(panel, districtBuilding, productIds, TooltipRegistrar, goodDescriber);
    }
}
