using System.Collections.Generic;
using UnityEngine;

public class RouletteTableHighlightController : MonoBehaviour
{
    [SerializeField] private Transform numberAreasRoot;
    [SerializeField] private Material highlightMaterial;

    private readonly Dictionary<string, RouletteNumberArea> numberAreasBySlotId = new Dictionary<string, RouletteNumberArea>();
    private readonly List<RouletteNumberArea> highlightedAreas = new List<RouletteNumberArea>();

    private void Awake()
    {
        CacheNumberAreas();
    }

    public void HighlightSlots(IReadOnlyList<string> slotIds)
    {
        ClearHighlight();

        if (slotIds == null)
            return;

        foreach (string slotId in slotIds)
        {
            if (!numberAreasBySlotId.TryGetValue(slotId, out RouletteNumberArea numberArea))
                continue;

            numberArea.SetHighlight(highlightMaterial);
            highlightedAreas.Add(numberArea);
        }
    }

    public void ClearHighlight()
    {
        foreach (RouletteNumberArea numberArea in highlightedAreas)
            numberArea.ClearHighlight();

        highlightedAreas.Clear();
    }

    private void CacheNumberAreas()
    {
        numberAreasBySlotId.Clear();

        if (numberAreasRoot == null)
            numberAreasRoot = transform;

        RouletteNumberArea[] numberAreas = numberAreasRoot.GetComponentsInChildren<RouletteNumberArea>(true);

        foreach (RouletteNumberArea numberArea in numberAreas)
        {
            if (string.IsNullOrWhiteSpace(numberArea.SlotId))
                continue;

            numberAreasBySlotId[numberArea.SlotId] = numberArea;
        }
    }
}