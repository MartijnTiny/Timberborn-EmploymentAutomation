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

public class ProductComponent : TickableComponent, IPersistentEntity, IEmploymentBoundsProvider
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManagerProductComponent");
    private static readonly PropertyKey<bool> OutStockActiveKey = new("Active");
    private static readonly PropertyKey<float> OutStockHighKey = new("High");
    private static readonly PropertyKey<float> OutStockLowKey = new("Low");
    private Manufactory manufactory;
    private Workplace workplace;
    private DistrictBuilding districtBuilding;
    private DistrictResourceCounterService districtResourceCounterService;
    public bool Available { get; private set; }
    public bool Active { get; set; } = false;
    public float High { get; set; } = 0.90f;
    public float Low { get; set; } = 0.50f;
    public float Fillrate { get; private set; } = 0f;
    public Vector2Int EmploymentBounds { get; private set; } = new();

    public void Save(IEntitySaver entitySaver)
    {
        var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
        component.Set(OutStockActiveKey, Active);
        component.Set(OutStockLowKey, Low);
        component.Set(OutStockHighKey, High);
    }

    public void Load(IEntityLoader entityLoader)
    {
        try
        {
            var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
            Active = component.Get(OutStockActiveKey);
            Low = component.Get(OutStockLowKey);
            High = component.Get(OutStockHighKey);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [Inject]
    public void InjectDependencies(EventBus eventBus, DistrictResourceCounterService districtResourceCounterService)
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

        Available = manufactory.CurrentRecipe?.ProducesProducts ?? false;
        var products = manufactory.CurrentRecipe?.Products ?? [];
        Fillrate = products.Aggregate(
            1.0f,
            (current, product) =>
                Mathf.Min(
                    current,
                    districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, product.Id)));
        EmploymentBounds = GetEmploymentBoundsProduct(Active ? Fillrate : 1.0f);
    }

    private Vector2Int GetEmploymentBoundsProduct(float fillrate)
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
            bounds.x -= Convert.ToInt32(fillrate > low); // fillrate above low threshold? remove one minimum worker
            bounds.y += Convert.ToInt32(fillrate < high); // fillrate below high threshold? add one maximum worker
            low += offset;
            high -= offset;
        }

        return bounds;
    }
}