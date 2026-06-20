using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UI;

public static class VisualElementExtensions
{
    public static MinMaxSlider AddIntMinMaxSliderWithValueDisplay(
        this VisualElement element,
        string label,
        Vector2Int value,
        int min,
        int max,
        Action<Vector2Int> onChange)
    {
        var slider = element.AddMinMaxSlider(
            label: label,
            values: new MinMaxSliderValues(new Vector2(value.x, value.y), min, max));

        // Apply light text color to all child labels
        var labels = slider.Query<Label>().ToList();
        foreach (var labelElement in labels)
        {
            labelElement.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));
        }

        var valueLabel = slider.AddGameLabel(min.ToString());
        slider.RegisterValueChangedCallback((e) =>
        {
            var newValue = new Vector2Int((int)Math.Round(e.newValue.x), (int)Math.Round(e.newValue.y));
            valueLabel.text = newValue.x + "% - " + newValue.y + "%";
            onChange(newValue);
        });
        return slider;
    }

    /// <summary>
    /// Creates a MinMaxSlider for worker counts (integers without % sign).
    /// </summary>
    public static MinMaxSlider AddIntMinMaxSliderWithoutPercentage(
        this VisualElement element,
        string label,
        Vector2Int value,
        int min,
        int max,
        Action<Vector2Int> onChange)
    {
        var slider = element.AddMinMaxSlider(
            label: label,
            values: new MinMaxSliderValues(new Vector2(value.x, value.y), min, max));

        // Apply light text color to all child labels
        var labels = slider.Query<Label>().ToList();
        foreach (var labelElement in labels)
        {
            labelElement.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));
        }

        var valueLabel = slider.AddGameLabel(min.ToString());
        
        // Set text color to be visible (light color for dark backgrounds)
        valueLabel.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f)); // Light gray
        
        slider.RegisterValueChangedCallback((e) =>
        {
            var newValue = new Vector2Int((int)Math.Round(e.newValue.x), (int)Math.Round(e.newValue.y));
            valueLabel.text = newValue.x + " - " + newValue.y;
            onChange(newValue);
        });
        return slider;
    }
}
