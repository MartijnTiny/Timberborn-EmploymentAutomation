using EmploymentAutomation.Logic;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.WorkSystem;
using TimberUi.CommonUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UI;

public class EmploymentAutomationFragment(
    VisualElementInitializer initializer) : BaseEntityPanelFragment<EmploymentComponent>
{
    private MinMaxSlider workerLimitSlider;
    private Workplace workplace;

    protected override void InitializePanel()
    {
        // Worker limit slider for this building (displays as integers, not percentages)
        workerLimitSlider = panel.AddIntMinMaxSliderWithoutPercentage(
            label: "Worker Limits",
            value: new Vector2Int(0, 10),
            min: 0,
            max: 100,
            onChange: OnWorkerLimitSliderChanged);

        panel.Initialize(initializer);
    }

    public override void ShowFragment(BaseComponent entity)
    {
        base.ShowFragment(entity);
        if (component == null) return;

        // Apply light text color to all text elements
        ApplyLightTextColor(panel);

        // Get the workplace to determine max workers
        workplace = component.GetComponent<Workplace>();
        UpdateWorkerLimitSliderRange();
        UpdateValues(component);
    }

    public override void UpdateFragment()
    {
        base.UpdateFragment();
        if (component == null) return;
        UpdateReadonlyValues(component);
    }

    private void UpdateValues(EmploymentComponent component)
    {
        workerLimitSlider.value = new Vector2(component.MinWorkerLimit, component.MaxWorkerLimit);
    }

    private void UpdateReadonlyValues(EmploymentComponent component)
    {
        // Called during updates, no text changes needed
    }

    private void UpdateWorkerLimitSliderRange()
    {
        var currentValue = workerLimitSlider.value;
        var maxWorkers = workplace != null ? workplace.MaxWorkers : 1;
        workerLimitSlider.lowLimit = 0;
        workerLimitSlider.highLimit = maxWorkers;

        workerLimitSlider.value = new Vector2(
            Mathf.Clamp(currentValue.x, 0, maxWorkers),
            Mathf.Clamp(currentValue.y, 0, maxWorkers));
    }

    private void OnWorkerLimitSliderChanged(Vector2Int value)
    {
        if (component == null)
            return;

        var minLimit = value.x;
        var maxLimit = value.y;

        // Ensure min <= max
        if (minLimit > maxLimit)
        {
            minLimit = maxLimit;
        }

        component.MinWorkerLimit = minLimit;
        component.MaxWorkerLimit = maxLimit;
    }

    private static void ApplyLightTextColor(VisualElement element)
    {
        if (element == null) return;

        element.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));

        foreach (var child in element.Children())
        {
            ApplyLightTextColor(child);
        }
    }
}
