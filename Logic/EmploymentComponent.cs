using Bindito.Core;
using System;
using System.Reflection;
using Timberborn.DuplicationSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation.Logic;

public class EmploymentComponent : TickableComponent, IPersistentEntity, IDuplicable<EmploymentComponent>
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManager");
    private static readonly PropertyKey<int> MinWorkerLimitKey = new("MinWorkerLimit");
    private static readonly PropertyKey<int> MaxWorkerLimitKey = new("MaxWorkerLimit");

    private Workplace workplace;

    static EmploymentComponent()
    {
    }

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
    public void InjectDependencies(EventBus eventBus)
    {
        eventBus.Register(this);
    }

    private void UpdateComponents()
    {
        workplace = GetComponent<Workplace>();
    }

    public override void StartTickable() => UpdateComponents();

    public override void Tick()
    {
        var powerComponent = GetComponent<PowerComponent>();
        var productComponent = GetComponent<ProductComponent>();
        var ingredientComponent = GetComponent<IngredientComponent>();

        var ingredientsEnabled = ingredientComponent is { Available: true, Active: true };
        var productEnabled = productComponent is { Available: true, Active: true };
        var powerEnabled = powerComponent is { Available: true, Active: true };
        if (workplace is null || !(ingredientsEnabled || productEnabled || powerEnabled)) return;

        var bounds = new Vector2Int(workplace.MaxWorkers, workplace.MaxWorkers);
        if (powerEnabled) bounds = Vector2Int.Min(bounds, powerComponent.EmploymentBounds);
        if (ingredientsEnabled) bounds = Vector2Int.Min(bounds, ingredientComponent.EmploymentBounds);
        if (productEnabled) bounds = Vector2Int.Min(bounds, productComponent.EmploymentBounds);

        var target = Mathf.Clamp(bounds.y, 0, workplace.MaxWorkers);
        if (workplace.DesiredWorkers == target) return;

        Debug.Log($"[EmploymentAutomation] {workplace.GameObject.name} DesiredWorkers {workplace.DesiredWorkers} -> {target}");

        workplace.DesiredWorkers = target;
        workplace.UnassignWorkerIfOverstaffed();
    }

    public void DuplicateFrom(EmploymentComponent source)
    {
        if (source == null) return;
        MinWorkerLimit = source.MinWorkerLimit;
        MaxWorkerLimit = source.MaxWorkerLimit;
    }
}