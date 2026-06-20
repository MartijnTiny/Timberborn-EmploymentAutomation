using System;
using EmploymentAutomation.Logic;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.Localization;
using Timberborn.TooltipSystem;
using TimberUi.CommonUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UI;

public abstract class BaseAutomationFragment<T> : BaseEntityPanelFragment<T> where T : BaseEmploymentComponent
{
    protected readonly ILoc Loc;
    protected readonly VisualElementInitializer Initializer;
    protected readonly ITooltipRegistrar TooltipRegistrar;

    private Toggle toggle;
    private MinMaxSlider fillRateSlider;

    protected BaseAutomationFragment(ILoc loc, VisualElementInitializer initializer, ITooltipRegistrar tooltipRegistrar)
    {
        Loc = loc;
        Initializer = initializer;
        TooltipRegistrar = tooltipRegistrar;
    }

    /// <summary>
    /// Get the localization key for the automation toggle. Must be overridden in derived classes.
    /// </summary>
    protected abstract string GetToggleLocalizationKey();

    /// <summary>
    /// Get human-readable name for this automation component.
    /// </summary>
    protected abstract string GetComponentName();

    /// <summary>
    /// Optional: Add tooltips or other UI enhancements. Called after ShowFragment.
    /// Override to add custom tooltips.
    /// </summary>
    protected virtual void SetupTooltips(T component)
    {
        // Override in derived classes to add tooltips
    }

    protected override void InitializePanel()
    {
        // Toggle for activation
        toggle = panel.AddToggle(
            text: Loc.T(GetToggleLocalizationKey()),
            onValueChanged: OnToggle);

        // Fill rate slider for this automation's thresholds
        fillRateSlider = panel.AddIntMinMaxSliderWithValueDisplay(
            label: $"{GetComponentName()} Thresholds",
            value: new Vector2Int(0, 100),
            min: 0,
            max: 100,
            onChange: OnFillRateSliderChanged);

        panel.Initialize(Initializer);

        // Apply light text color after initialization to catch all created elements
        ApplyLightTextColor(panel);
    }

    public override void ShowFragment(BaseComponent entity)
    {
        base.ShowFragment(entity);
        if (component == null) return;
        UpdateValues(component);
        ApplyLightTextColor(panel);
        SetupTooltips(component);
    }

    public override void UpdateFragment()
    {
        base.UpdateFragment();
        if (component == null) return;
        UpdateReadonlyValues(component);
    }

    private void UpdateValues(IEmploymentBoundsProvider component)
    {
        panel.ToggleDisplayStyle(component.Available);
        toggle.ToggleDisplayStyle(component.Available);
        fillRateSlider.ToggleDisplayStyle(component.Available);

        toggle.text = ToggleText(component.Fillrate);
        toggle.value = component.Active;
        fillRateSlider.value = new Vector2(component.Low * 100f, component.High * 100f);
    }

    private void UpdateReadonlyValues(IEmploymentBoundsProvider component)
    {
        toggle.text = ToggleText(component.Fillrate);
    }

    private string ToggleText(float fillrate) =>
        Loc.T(GetToggleLocalizationKey()) + " " + (int)Math.Round(fillrate * 100) + "%";

    private void OnFillRateSliderChanged(Vector2Int value)
    {
        if (component == null)
            return;
        component.Low = value.x / 100f;
        component.High = value.y / 100f;
    }

    private void OnToggle(bool toggleState)
    {
        if (component == null)
            return;
        component.Active = toggleState;
    }

    /// <summary>
    /// Apply light text color to an element and all its children recursively.
    /// </summary>
    private static void ApplyLightTextColor(VisualElement element)
    {
        if (element == null) return;

        // Apply light text color to the element itself
        element.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));

        // Recursively apply to all children
        foreach (var child in element.Children())
        {
            ApplyLightTextColor(child);
        }
    }
}