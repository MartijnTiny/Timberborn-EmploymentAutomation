using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using EmploymentAutomation.Logic;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.GoodsUI;
using Timberborn.InventorySystem;
using Timberborn.TooltipSystem;
using Timberborn.Workshops;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UI;

public static class StorageTooltipHelper
{
    private class DistrictSnapshot
    {
        public Dictionary<string, List<Inventory>> InventoriesByGood = new();
        public Dictionary<string, List<Inventory>> InputInventoriesByGood = new();
        public Dictionary<string, List<Inventory>> OutputInventoriesByGood = new();
        public int OutputThreshold;
        public readonly Dictionary<string, HashSet<int>> SeenByGood = new();
        public readonly Dictionary<string, HashSet<int>> SeenInputByGood = new();
        public readonly Dictionary<string, HashSet<int>> SeenOutputByGood = new();
    }

    private static readonly Dictionary<int, DistrictSnapshot> SnapshotCache = new();
    private static int _cachedDistrictId = -1;
    private static VisualElement _cachedTooltip;
    private static string _cachedTooltipKey;

    private static DistrictSnapshot GetOrBuildSnapshot(DistrictCenter district)
    {
        var districtId = district.GetHashCode();
        if (_cachedDistrictId == districtId && SnapshotCache.TryGetValue(districtId, out var cached))
            return cached;

        _cachedTooltip = null;
        var snapshot = new DistrictSnapshot();
        var maxRecipeSlots = 0;
        var inventoryRegistry = district.GetComponent<DistrictInventoryRegistry>();

        if (inventoryRegistry != null)
        {
            foreach (var inventory in inventoryRegistry.Inventories)
            {
                if (!inventory.IsUnblocked || !inventory.Enabled)
                    continue;

                var capacityCache = new List<GoodAmount>();
                inventory.GetCapacity(capacityCache);

                var slotCount = capacityCache.Count;
                if (inventory.GetComponent<Manufactory>() != null && slotCount > maxRecipeSlots)
                    maxRecipeSlots = slotCount;

                var invId = RuntimeHelpers.GetHashCode(inventory);
                foreach (var good in capacityCache)
                {
                    if (!snapshot.InventoriesByGood.TryGetValue(good.GoodId, out var list))
                    {
                        list = new List<Inventory>();
                        snapshot.InventoriesByGood[good.GoodId] = list;
                        snapshot.SeenByGood[good.GoodId] = new HashSet<int>();
                    }
                    if (snapshot.SeenByGood[good.GoodId].Add(invId))
                        list.Add(inventory);

                    if (inventory.InputGoods.Contains(good.GoodId))
                    {
                        if (!snapshot.InputInventoriesByGood.TryGetValue(good.GoodId, out var inputList))
                        {
                            inputList = new List<Inventory>();
                            snapshot.InputInventoriesByGood[good.GoodId] = inputList;
                            snapshot.SeenInputByGood[good.GoodId] = new HashSet<int>();
                        }
                        if (snapshot.SeenInputByGood[good.GoodId].Add(invId))
                            inputList.Add(inventory);
                    }

                    if (inventory.OutputGoods.Contains(good.GoodId))
                    {
                        if (!snapshot.OutputInventoriesByGood.TryGetValue(good.GoodId, out var outputList))
                        {
                            outputList = new List<Inventory>();
                            snapshot.OutputInventoriesByGood[good.GoodId] = outputList;
                            snapshot.SeenOutputByGood[good.GoodId] = new HashSet<int>();
                        }
                        if (snapshot.SeenOutputByGood[good.GoodId].Add(invId))
                            outputList.Add(inventory);
                    }
                }
            }
        }

        snapshot.OutputThreshold = maxRecipeSlots * 2;

        _cachedDistrictId = districtId;
        SnapshotCache[districtId] = snapshot;
        return snapshot;
    }

    public static void AddIngredientStorageTooltip(
        VisualElement element,
        DistrictBuilding districtBuilding,
        IEnumerable<string> ingredientIds,
        ITooltipRegistrar tooltipRegistrar,
        GoodDescriber goodDescriber)
    {
        if (element == null || districtBuilding == null || tooltipRegistrar == null || goodDescriber == null)
            return;

        var district = districtBuilding.InstantDistrict as DistrictCenter;
        var idList = ingredientIds.ToList();

        tooltipRegistrar.Register(element, () => BuildStorageTooltip(
            district,
            idList,
            "Input Storage",
            goodDescriber));
    }

    public static void AddProductStorageTooltip(
        VisualElement element,
        DistrictBuilding districtBuilding,
        IEnumerable<string> productIds,
        ITooltipRegistrar tooltipRegistrar,
        GoodDescriber goodDescriber)
    {
        if (element == null || districtBuilding == null || tooltipRegistrar == null || goodDescriber == null)
            return;

        var district = districtBuilding.InstantDistrict as DistrictCenter;
        var idList = productIds.ToList();

        tooltipRegistrar.Register(element, () => BuildStorageTooltip(
            district,
            idList,
            "Output Storage",
            goodDescriber));
    }

    private static VisualElement BuildStorageTooltip(
        DistrictCenter district,
        List<string> resourceIds,
        string label,
        GoodDescriber goodDescriber)
    {
        if (district == null || goodDescriber == null)
            return new VisualElement();

        var key = $"{district.GetHashCode()}|{string.Join(",", resourceIds)}|{label}";
        if (_cachedTooltipKey == key && _cachedTooltip != null)
            return _cachedTooltip;

        var container = new VisualElement();
        container.style.paddingLeft = 5;
        container.style.paddingRight = 5;
        container.style.paddingTop = 5;
        container.style.paddingBottom = 5;

        var titleLabel = new Label(label + ":");
        titleLabel.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        container.Add(titleLabel);

        if (!resourceIds.Any())
        {
            var emptyLabel = new Label("(No items)");
            emptyLabel.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));
            container.Add(emptyLabel);
            return container;
        }

        var snapshot = GetOrBuildSnapshot(district);
        var resourceSet = resourceIds.ToHashSet();

        var districtCur = new Dictionary<string, int>();
        var districtMax = new Dictionary<string, int>();
        var inputCur = new Dictionary<string, int>();
        var inputMax = new Dictionary<string, int>();
        var outputCur = new Dictionary<string, int>();
        var outputMax = new Dictionary<string, int>();
        var producerInputCur = new Dictionary<string, int>();
        var producerInputMax = new Dictionary<string, int>();
        var producerOutputCur = new Dictionary<string, int>();
        var producerOutputMax = new Dictionary<string, int>();

        foreach (var id in resourceIds)
        {
            districtCur[id] = 0;
            districtMax[id] = 0;
            inputCur[id] = 0;
            inputMax[id] = 0;
            outputCur[id] = 0;
            outputMax[id] = 0;
            producerInputCur[id] = 0;
            producerInputMax[id] = 0;
            producerOutputCur[id] = 0;
            producerOutputMax[id] = 0;
        }

        foreach (var inventory in snapshot.InventoriesByGood
                     .Where(kvp => resourceSet.Contains(kvp.Key))
                     .SelectMany(kvp => kvp.Value))
        {
            var invLabel = inventory.ToString().Split('\n')[0].Trim();
            var capacityCache = new List<GoodAmount>();
            inventory.GetCapacity(capacityCache);
            var totalSlots = capacityCache.Select(c => c.GoodId).Distinct().Count();

            var hasInput = false;
            foreach (var rid in resourceIds)
                if (inventory.InputGoods.Contains(rid)) { hasInput = true; break; }
            if (totalSlots > snapshot.OutputThreshold && !hasInput)
            {
                Debug.Log($"[EmploymentAutomation] skipped district: {invLabel} capSlots={totalSlots}");
                continue;
            }

            var seenGoods = new HashSet<string>();
            foreach (var good in capacityCache)
            {
                if (!resourceSet.Contains(good.GoodId))
                    continue;
                if (!seenGoods.Add(good.GoodId))
                    continue;

                var stock = inventory.Stock.FirstOrDefault(s => s.GoodId == good.GoodId);
                var stockAmount = stock.GoodId == good.GoodId ? stock.Amount : 0;

                districtCur[good.GoodId] += stockAmount;
                districtMax[good.GoodId] += good.Amount;

                var isInput = inventory.InputGoods.Contains(good.GoodId);
                var isOutput = inventory.OutputGoods.Contains(good.GoodId);
                var isStorage = isInput && isOutput;

                if (isInput)
                {
                    inputCur[good.GoodId] += stockAmount;
                    inputMax[good.GoodId] += good.Amount;
                    if (!isStorage)
                    {
                        producerInputCur[good.GoodId] += stockAmount;
                        producerInputMax[good.GoodId] += good.Amount;
                    }
                    Debug.Log($"[EmploymentAutomation] input: {invLabel} +{stockAmount}/{good.Amount} {good.GoodId}");
                }

                if (isOutput && !(totalSlots > snapshot.OutputThreshold && !inventory.InputGoods.Contains(good.GoodId)))
                {
                    outputCur[good.GoodId] += stockAmount;
                    outputMax[good.GoodId] += good.Amount;
                    if (!isStorage)
                    {
                        producerOutputCur[good.GoodId] += stockAmount;
                        producerOutputMax[good.GoodId] += good.Amount;
                    }
                    Debug.Log($"[EmploymentAutomation] output: {invLabel} +{stockAmount}/{good.Amount} {good.GoodId}");
                }
            }
        }

        foreach (var resourceId in resourceIds)
        {
            try
            {
                var describedGood = goodDescriber.GetDescribedGood(resourceId);
                var displayName = describedGood.DisplayName;

                var percent = districtMax[resourceId] > 0 ? districtCur[resourceId] * 100 / districtMax[resourceId] : 0;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 2;
                row.style.marginBottom = 2;

                var icon = new Image();
                icon.sprite = describedGood.Icon;
                icon.style.width = 20;
                icon.style.height = 20;
                icon.style.marginRight = 5;
                icon.style.flexShrink = 0;
                row.Add(icon);

                var text = $"{displayName}: {districtCur[resourceId]}/{districtMax[resourceId]} ({percent}% [{producerInputCur[resourceId]}/{producerInputMax[resourceId]} | {producerOutputCur[resourceId]}/{producerOutputMax[resourceId]}])";

                var itemLabel = new Label(text);
                itemLabel.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));
                itemLabel.style.whiteSpace = WhiteSpace.Normal;
                row.Add(itemLabel);

                container.Add(row);
            }
            catch
            {
                var errorLabel = new Label($"{resourceId}: (unavailable)");
                errorLabel.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 1f));
                errorLabel.style.marginLeft = 10;
                container.Add(errorLabel);
            }
        }

        _cachedTooltip = container;
        _cachedTooltipKey = key;
        return container;
    }
}
