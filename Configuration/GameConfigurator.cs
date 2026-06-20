using Bindito.Core;
using EmploymentAutomation.Logic;
using EmploymentAutomation.UI;
using Timberborn.TemplateInstantiation;
using Timberborn.Workshops;
using TimberUi.CommonProviders;

namespace EmploymentAutomation.Configuration;

[Context("Game")]
public class GameConfigurator : Configurator
{
    private class FragmentsProvider(
        EmploymentAutomationFragment employmentAutomationFragment,
        IngredientAutomationFragment ingredientAutomationFragment,
        ProductAutomationFragment productAutomationFragment,
        PowerAutomationFragment powerAutomationFragment) : EntityPanelFragmentProvider([
        employmentAutomationFragment, ingredientAutomationFragment, productAutomationFragment, powerAutomationFragment
    ]);

    protected override void Configure()
    {
        Bind<DistrictResourceCounterService>().AsSingleton();
        Bind<EmploymentComponent>().AsTransient();
        Bind<PowerComponent>().AsTransient();
        Bind<IngredientComponent>().AsTransient();
        Bind<ProductComponent>().AsTransient();
        MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
        this.BindFragments<FragmentsProvider>();
    }

    private static TemplateModule ProvideTemplateModule()
    {
        var builder = new TemplateModule.Builder();
        builder.AddDecorator<Manufactory, EmploymentComponent>();
        builder.AddDecorator<Manufactory, PowerComponent>();
        builder.AddDecorator<Manufactory, IngredientComponent>();
        builder.AddDecorator<Manufactory, ProductComponent>();
        return builder.Build();
    }
}