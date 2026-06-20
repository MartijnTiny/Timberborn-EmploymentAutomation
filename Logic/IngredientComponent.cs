using System;
using System.Linq;
using Bindito.Core;
using Timberborn.GameDistricts;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation.Logic;

public class IngredientComponent : BaseEmploymentComponent
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManagerIngredientComponent");
    private static readonly PropertyKey<bool> ActiveKey = new("Active");
    private static readonly PropertyKey<float> HighKey = new("High");
    private static readonly PropertyKey<float> LowKey = new("Low");
    
    private DistrictBuilding districtBuilding;
    private DistrictResourceCounterService districtResourceCounterService;
    private Manufactory manufactory;
    private EmploymentComponent employmentComponent;

    public override bool Active { get; set; } = false;
    public override float High { get; set; } = 0.50f;
    public override float Low { get; set; } = 0.10f;

    protected override ComponentKey GetComponentKey() => EmploymentManagerComponentKey;

    protected override (PropertyKey<bool> activeKey, PropertyKey<float> lowKey, PropertyKey<float> highKey) GetPropertyKeys()
    {
        return (ActiveKey, LowKey, HighKey);
    }

    [Inject]
    public void InjectDependencies(
        DistrictResourceCounterService districtResourceCounterService, EventBus eventBus)
    {
        eventBus.Register(this);
        this.districtResourceCounterService = districtResourceCounterService;
    }

    private void UpdateComponents()
    {
        workplace = GetComponent<Workplace>();
        manufactory = GetComponent<Manufactory>();
        districtBuilding = GetComponent<DistrictBuilding>();
        employmentComponent = GetComponent<EmploymentComponent>();
    }

    public override void StartTickable() => UpdateComponents();

    public override void Tick()
    {
        if (manufactory == null || districtBuilding == null || workplace == null)
        {
            Available = false;
            Fillrate = 1.0f;
            EmploymentBounds = workplace != null ? new Vector2Int(workplace.MaxWorkers, 0) : Vector2Int.zero;
            return;
        }

        Available = manufactory.CurrentRecipe?.ConsumesIngredients ?? false;
        var ingredients = manufactory.CurrentRecipe?.Ingredients ?? [];
        var primaryIngredient = ingredients.FirstOrDefault();
        Fillrate = primaryIngredient.Id != null
            ? districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, primaryIngredient.Id)
            : 1.0f;
        EmploymentBounds = GetEmploymentBoundsIngredient(Active ? Fillrate : 1.0f);
    }

	private Vector2Int GetEmploymentBoundsIngredient(float fillrate)
    {
        if (workplace == null || employmentComponent == null) 
        {
            return Vector2Int.zero;
        }

        var bounds = new Vector2Int(workplace.MaxWorkers, 0);
        var offset = (High - Low) / (workplace.MaxWorkers * 2 - 1);
        var low = Low;
        var high = High;
        for (var i = 0; i < workplace.MaxWorkers; i++)
        {
            bounds.y += Convert.ToInt32(fillrate > low); // fillrate above low threshold? add one maximum worker
            bounds.x -= Convert.ToInt32(fillrate <
                                        high); // fillrate below high threshold? remove one minimum worker
            low += offset;
            high -= offset;
        }

        return ApplyWorkerLimits(bounds, employmentComponent.MinWorkerLimit, employmentComponent.MaxWorkerLimit);
    }
}