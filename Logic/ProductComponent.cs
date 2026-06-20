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

public class ProductComponent : BaseEmploymentComponent
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManagerProductComponent");
    private static readonly PropertyKey<bool> OutStockActiveKey = new("Active");
    private static readonly PropertyKey<float> OutStockHighKey = new("High");
    private static readonly PropertyKey<float> OutStockLowKey = new("Low");
    
    private Manufactory manufactory;
    private DistrictBuilding districtBuilding;
    private DistrictResourceCounterService districtResourceCounterService;
    private EmploymentComponent employmentComponent;

    public override bool Active { get; set; } = false;
    public override float High { get; set; } = 0.90f;
    public override float Low { get; set; } = 0.50f;

    protected override ComponentKey GetComponentKey() => EmploymentManagerComponentKey;

    protected override (PropertyKey<bool> activeKey, PropertyKey<float> lowKey, PropertyKey<float> highKey) GetPropertyKeys()
    {
        return (OutStockActiveKey, OutStockLowKey, OutStockHighKey);
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

        Available = manufactory.CurrentRecipe?.ProducesProducts ?? false;
        var products = manufactory.CurrentRecipe?.Products ?? [];
        var primaryProduct = products.FirstOrDefault();
        Fillrate = primaryProduct.Id != null
            ? districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, primaryProduct.Id)
            : 1.0f;
        EmploymentBounds = GetEmploymentBoundsProduct(Active ? Fillrate : 1.0f);
    }

    private Vector2Int GetEmploymentBoundsProduct(float fillrate)
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
            bounds.x -= Convert.ToInt32(fillrate > low); // fillrate above low threshold? remove one minimum worker
            bounds.y += Convert.ToInt32(fillrate < high); // fillrate below high threshold? add one maximum worker
            low += offset;
            high -= offset;
        }

        return ApplyWorkerLimits(bounds, employmentComponent.MinWorkerLimit, employmentComponent.MaxWorkerLimit);
    }
}