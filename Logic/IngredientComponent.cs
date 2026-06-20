using System;
using System.Linq;
using Bindito.Core;
using Timberborn.GameDistricts;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation.Logic;

public class IngredientComponent : TickableComponent, IPersistentEntity, IEmploymentBoundsProvider
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManagerIngredientComponent");
    private static readonly PropertyKey<bool> ActiveKey = new("Active");
    private static readonly PropertyKey<float> HighKey = new("High");
    private static readonly PropertyKey<float> LowKey = new("Low");
    private DistrictBuilding districtBuilding;
    private DistrictResourceCounterService districtResourceCounterService;
    private Manufactory manufactory;
    private Workplace workplace;
    public bool Available { get; private set; }
    public bool Active { get; set; } = true;
    public float High { get; set; } = 0.50f;
    public float Low { get; set; } = 0.10f;
    public float Fillrate { get; private set; } = 0f;
    public Vector2Int EmploymentBounds { get; private set; } = new();

    public void Save(IEntitySaver entitySaver)
    {
        var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
        component.Set(ActiveKey, Active);
        component.Set(LowKey, Low);
        component.Set(HighKey, High);
    }

    public void Load(IEntityLoader entityLoader)
    {
        try
        {
            var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
            Active = component.Get(ActiveKey);
            Low = component.Get(LowKey);
            High = component.Get(HighKey);
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
        this.districtResourceCounterService = districtResourceCounterService;
    }

    private void UpdateComponents()
    {
        workplace = GetComponent<Workplace>();
        manufactory = GetComponent<Manufactory>();
        districtBuilding = GetComponent<DistrictBuilding>();
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
        Fillrate = ingredients.Aggregate(
            1.0f,
            (current, ingredient) => Mathf.Min(
                current,
                districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, ingredient.Id)));
        EmploymentBounds = GetEmploymentBoundsIngredient(Active ? Fillrate : 1.0f);
    }

	private Vector2Int GetEmploymentBoundsIngredient(float fillrate)
    {
        if (workplace == null) 
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

        return bounds;
    }
}