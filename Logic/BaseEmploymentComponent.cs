using System;
using Timberborn.Buildings;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using Timberborn.WorkSystem;
using UnityEngine;
using Timberborn.WorldPersistence;

namespace EmploymentAutomation.Logic;

public abstract class BaseEmploymentComponent : TickableComponent, IPersistentEntity, IEmploymentBoundsProvider
{
    protected Workplace workplace;
    
    public virtual bool Available { get; protected set; }
    public virtual bool Active { get; set; } = false;
    public virtual float High { get; set; } = 0.5f;
    public virtual float Low { get; set; } = 0.25f;
    public virtual float Fillrate { get; protected set; } = 0f;
    public Vector2Int EmploymentBounds { get; protected set; }

    /// <summary>
    /// Get the component key for persistence. Must be overridden in derived classes.
    /// </summary>
    protected abstract ComponentKey GetComponentKey();

    /// <summary>
    /// Get the property keys for Active, Low, and High. Must be overridden in derived classes.
    /// </summary>
    protected abstract (PropertyKey<bool> activeKey, PropertyKey<float> lowKey, PropertyKey<float> highKey) GetPropertyKeys();

    public virtual void Save(IEntitySaver entitySaver)
    {
        var componentKey = GetComponentKey();
        var (activeKey, lowKey, highKey) = GetPropertyKeys();
        
        var component = entitySaver.GetComponent(componentKey);
        component.Set(activeKey, Active);
        component.Set(lowKey, Low);
        component.Set(highKey, High);
    }

    public virtual void Load(IEntityLoader entityLoader)
    {
        try
        {
            var componentKey = GetComponentKey();
            var (activeKey, lowKey, highKey) = GetPropertyKeys();
            
            var component = entityLoader.GetComponent(componentKey);
            Active = component.Get(activeKey);
            Low = component.Get(lowKey);
            High = component.Get(highKey);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// Calculate effective min and max worker limits with clamping.
    /// </summary>
    protected (int effectiveMin, int effectiveMax) GetEffectiveWorkerLimits(int minLimit, int maxLimit)
    {
        if (workplace == null)
            return (0, 0);

        var effectiveMin = Mathf.Clamp(minLimit, 0, workplace.MaxWorkers);
        var effectiveMax = Mathf.Clamp(maxLimit, effectiveMin, workplace.MaxWorkers);
        
        return (effectiveMin, effectiveMax);
    }

    protected virtual bool ShouldPause => false;

    protected void ApplyPauseLogic()
    {
        if (!Available || !Active) return;

        var pausableBuilding = GetComponent<PausableBuilding>();
        if (pausableBuilding == null) return;

        var workplaceComp = GetComponent<Workplace>();
        if (workplaceComp != null && workplaceComp.MaxWorkers > 0) return;

        Debug.Log($"[EmploymentAutomation] PauseCheck: {GameObject.name} Fillrate={Fillrate:F2} Low={Low:F2} High={High:F2} ShouldPause={ShouldPause} Paused={pausableBuilding.Paused}");

        if (ShouldPause && !pausableBuilding.Paused)
        {
            Debug.Log($"[EmploymentAutomation] -> Pausing {GameObject.name}");
            pausableBuilding.Pause();
        }
        else if (!ShouldPause && pausableBuilding.Paused)
        {
            Debug.Log($"[EmploymentAutomation] -> Resuming {GameObject.name}");
            pausableBuilding.Resume();
        }
    }

    protected void CopySettingsFrom(BaseEmploymentComponent source)
    {
        if (source == null) return;
        Active = source.Active;
        High = source.High;
        Low = source.Low;
    }

    /// <summary>
    /// Apply worker limits to employment bounds. This ensures the bounds respect MinWorkerLimit and MaxWorkerLimit.
    /// </summary>
    protected Vector2Int ApplyWorkerLimits(Vector2Int bounds, int minLimit, int maxLimit)
    {
        var (effectiveMin, effectiveMax) = GetEffectiveWorkerLimits(minLimit, maxLimit);
        
        // Clamp both bounds to the effective range
        bounds.x = Mathf.Clamp(bounds.x, effectiveMin, effectiveMax);
        bounds.y = Mathf.Clamp(bounds.y, effectiveMin, effectiveMax);
        
        // Ensure min <= max
        if (bounds.x > bounds.y)
            bounds.x = bounds.y;
        
        return bounds;
    }
}
