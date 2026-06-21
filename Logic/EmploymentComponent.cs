using Bindito.Core;
using System;
using Timberborn.Buildings;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation.Logic;

public class EmploymentComponent : TickableComponent, IPersistentEntity
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManager");
    private static readonly PropertyKey<int> MinWorkerLimitKey = new("MinWorkerLimit");
    private static readonly PropertyKey<int> MaxWorkerLimitKey = new("MaxWorkerLimit");

    private Workplace workplace;
    private PowerComponent powerComponent;
    private ProductComponent productComponent;
    private IngredientComponent ingredientComponent;
    private PausableBuilding pausableBuilding;

    public int MinWorkerLimit { get; set; } = 0;
    public int MaxWorkerLimit { get; set; } = int.MaxValue;

    public void Save(IEntitySaver entitySaver)
    {
        var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
        component.Set(MinWorkerLimitKey, MinWorkerLimit);
        component.Set(MaxWorkerLimitKey, MaxWorkerLimit);
    }

    public void Load(IEntityLoader entityLoader)
    {
        try
        {
            var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
            MinWorkerLimit = component.Get(MinWorkerLimitKey);
            MaxWorkerLimit = component.Get(MaxWorkerLimitKey);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [Inject]
    public void InjectDependencies(
        EventBus eventBus)
    {
        eventBus.Register(this);
    }

    private void UpdateComponents()
    {
        workplace = GetComponent<Workplace>();
        powerComponent = GetComponent<PowerComponent>();
        productComponent = GetComponent<ProductComponent>();
        ingredientComponent = GetComponent<IngredientComponent>();
        pausableBuilding = GetComponent<PausableBuilding>();
    }

    public override void StartTickable() => UpdateComponents();

    public override void Tick()
    {
        var ingredientsEnabled = ingredientComponent is { Available: true, Active: true };
        var productEnabled = productComponent is { Available: true, Active: true };
        var powerEnabled = powerComponent is { Available: true, Active: true };
        var hasToTick = workplace is not null && pausableBuilding is not null &&
                        (ingredientsEnabled || productEnabled || powerEnabled);
        if (!hasToTick) return;
        // employment trigger bounds
        var bounds = new Vector2Int(workplace.MaxWorkers, workplace.MaxWorkers);
        if (powerEnabled) bounds = Vector2Int.Min(bounds, powerComponent.EmploymentBounds);
        if (ingredientsEnabled) bounds = Vector2Int.Min(bounds, ingredientComponent.EmploymentBounds);
        if (productEnabled) bounds = Vector2Int.Min(bounds, productComponent.EmploymentBounds);

        // perform employment
        var currentDesiredWorkers = GetDesiredWorkers();
        if (currentDesiredWorkers < bounds.x)
            IncreaseDesiredWorkers();
        else if (currentDesiredWorkers > bounds.y)
            DecreaseDesiredWorkers();
    }

    private int GetDesiredWorkers() => workplace.DesiredWorkers;


    private void IncreaseDesiredWorkers()
    {
        workplace.IncreaseDesiredWorkers();
    }

    private void DecreaseDesiredWorkers()
    {
        if (workplace.DesiredWorkers > 0)
        {
            workplace.DecreaseDesiredWorkers();
        }
    }
}