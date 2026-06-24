using System;
using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using Timberborn.DuplicationSystem;
using Timberborn.MechanicalSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation.Logic;

public class PowerComponent : BaseEmploymentComponent, IDuplicable<PowerComponent>
{
    private static readonly ComponentKey EmploymentManagerComponentKey =
        new("EmploymentManagerPowerComponent");

    private static readonly PropertyKey<bool> PowerActiveKey = new("Active");
    private static readonly PropertyKey<float> PowerHighKey = new("High");
    private static readonly PropertyKey<float> PowerLowKey = new("Low");
    private bool permanentlyDisabled = false;
    private bool available;

    public override bool Available
    {
        get => available && !permanentlyDisabled;
        protected set => available = value;
    }

    public override float High { get; set; } = 0.75f;
    public override float Low { get; set; } = 0.25f;
    protected override bool ShouldPause => Fillrate < Low;

    private Manufactory manufactory;
    private MechanicalNode mechanicalNode;
    private EmploymentComponent employmentComponent;

    protected override ComponentKey GetComponentKey() => EmploymentManagerComponentKey;

    protected override (PropertyKey<bool> activeKey, PropertyKey<float> lowKey, PropertyKey<float> highKey) GetPropertyKeys()
    {
        return (PowerActiveKey, PowerLowKey, PowerHighKey);
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
        employmentComponent = GetComponent<EmploymentComponent>();
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
        ApplyPauseLogic();
    }

    private Vector2Int GetEmploymentBoundsPower(float powerMeter)
    {
        if (workplace == null || employmentComponent == null)
        {
            return Vector2Int.zero;
        }

        var bounds = new Vector2Int(
            powerMeter < High ? 0 : workplace.MaxWorkers, // min
            powerMeter < Low ? 0 : workplace.MaxWorkers); // max
        
        return ApplyWorkerLimits(bounds, employmentComponent.MinWorkerLimit, employmentComponent.MaxWorkerLimit);
    }

    public void DuplicateFrom(PowerComponent source)
    {
        CopySettingsFrom(source);
    }

}