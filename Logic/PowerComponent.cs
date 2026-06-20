using System;
using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using Timberborn.MechanicalSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation.Logic;

public class PowerComponent : TickableComponent, IPersistentEntity, IEmploymentBoundsProvider
{
    private static readonly ComponentKey EmploymentManagerComponentKey =
        new("EmploymentManagerPowerComponent");

    private static readonly PropertyKey<bool> PowerActiveKey = new("Active");
    private static readonly PropertyKey<float> PowerHighKey = new("High");
    private static readonly PropertyKey<float> PowerLowKey = new("Low");
    private bool permanentlyDisabled = false;

    public bool Available
    {
        get => available && !permanentlyDisabled;
        private set => available = value;
    }

    public bool Active { get; set; } = false;
    public float High { get; set; } = 0.75f;
    public float Low { get; set; } = 0.25f;

    public float Fillrate { get; private set; } = 0;
    public Vector2Int EmploymentBounds { get; private set; }
    private Manufactory manufactory;
    private MechanicalNode mechanicalNode;
    private Workplace workplace;
    private bool available;

    public void Save(IEntitySaver entitySaver)
    {
        var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
        component.Set(PowerActiveKey, Active);
        component.Set(PowerLowKey, Low);
        component.Set(PowerHighKey, High);
    }

    public void Load(IEntityLoader entityLoader)
    {
        try
        {
            var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
            Active = component.Get(PowerActiveKey);
            Low = component.Get(PowerLowKey);
            High = component.Get(PowerHighKey);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [Inject]
    public void InjectDependencies(
        DistrictResourceCounterService districtResourceCounterService, EventBus eventBus)
    {
        eventBus.Register(this);
    }

    private void UpdateComponents()
    {
        workplace = GetComponent<Workplace>();
        manufactory = GetComponent<Manufactory>();
        mechanicalNode = GetComponent<MechanicalNode>();
        // Don't perform power management when another mod adds power automation
        permanentlyDisabled = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Any(x => x.Namespace is "IgorZ.SmartPower.Core");
    }

    public override void StartTickable() => UpdateComponents();

    public override void Tick()
    {
        Available = (mechanicalNode?.IsConsumer ?? false) && (manufactory?.HasCurrentRecipe ?? false);
        if (!Available)
        {
            if (EmploymentBounds == default)
            {
                EmploymentBounds = workplace != null ? new Vector2Int(workplace.MaxWorkers, 0) : Vector2Int.zero;
            }
            return;
        }

        var batteries = mechanicalNode?.Graph?.Batteries.Where(battery =>
            battery.ActiveAndPowered).ToImmutableArray() ?? [];
        var capacities = batteries.Select(battery =>
            new Vector2(battery.NominalBatteryCharge, battery.NominalBatteryCapacity));
        var networkCapacity = capacities.Aggregate(Vector2.zero, (x, y) => x + y);
        if (networkCapacity.y == 0)
            Fillrate = mechanicalNode?.Graph?.PowerEfficiency ?? 0f;
        else
            Fillrate = networkCapacity.x / networkCapacity.y;
        EmploymentBounds = GetEmploymentBoundsPower(Fillrate);
    }

    private Vector2Int GetEmploymentBoundsPower(float powerMeter)
    {
        if (workplace == null)
        {
            return Vector2Int.zero;
        }

        return new Vector2Int(
            powerMeter < High ? 0 : workplace.MaxWorkers, // min
            powerMeter < Low ? 0 : workplace.MaxWorkers); // max
    }
}