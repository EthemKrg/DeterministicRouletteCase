using System.Collections.Generic;
using UnityEngine;

public class RouletteTableHighlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private Transform numberAreasRoot;

    [Header("Hover")]
    [SerializeField] private Material highlightMaterial;

    [Header("Table Button Hover")]
    [SerializeField] private Material tableButtonHighlightMaterial;

    [Header("Winning Marker")]
    [SerializeField] private GameObject winningMarkerPrefab;
    [SerializeField] private Vector3 markerOffset = Vector3.zero;

    private readonly Dictionary<string, RouletteNumberArea> numberAreasBySlotId = new Dictionary<string, RouletteNumberArea>();
    private readonly List<RouletteNumberArea> highlightedAreas = new List<RouletteNumberArea>();

    private RouletteBetArea highlightedBetArea;
    private TableGameControls3DButton highlightedTableButton;
    private GameObject winningMarkerInstance;

    private void Awake()
    {
        CacheNumberAreas();
        CreateWinningMarker();
    }

    private void OnEnable()
    {
        if (gameFlowController != null)
            gameFlowController.OnRoundResolved += HandleRoundResolved;
    }

    private void OnDisable()
    {
        if (gameFlowController != null)
            gameFlowController.OnRoundResolved -= HandleRoundResolved;
    }

    public void HighlightSlots(IReadOnlyList<string> slotIds)
    {
        ClearHighlight();
        HighlightNumberAreas(slotIds);
    }

    public void HighlightBetArea(RouletteBetArea betArea, IReadOnlyList<string> slotIds)
    {
        ClearHighlight();

        highlightedBetArea = betArea;

        if (highlightedBetArea != null)
            highlightedBetArea.SetHighlight(highlightMaterial);

        HighlightNumberAreas(slotIds);
    }

    public void ClearHighlight()
    {
        foreach (RouletteNumberArea numberArea in highlightedAreas)
            numberArea.ClearHighlight();

        highlightedAreas.Clear();

        if (highlightedBetArea != null)
        {
            highlightedBetArea.ClearHighlight();
            highlightedBetArea = null;
        }

        ClearTableButtonHighlight();
    }

    public void RefreshNumberAreas()
    {
        CacheNumberAreas();
    }

    private void HighlightNumberAreas(IReadOnlyList<string> slotIds)
    {
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

    public void ShowWinningMarker(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return;

        if (!numberAreasBySlotId.TryGetValue(slotId, out RouletteNumberArea numberArea))
            return;

        if (winningMarkerInstance == null)
            CreateWinningMarker();

        Transform target = numberArea.transform;

        var position = target.position + markerOffset;

        winningMarkerInstance.SetActive(false);
        winningMarkerInstance.transform.position = position;
        winningMarkerInstance.transform.rotation = target.rotation;
        winningMarkerInstance.SetActive(true);
    }

    private void HandleRoundResolved(RoundResult result)
    {
        if (result == null || result.WinningSlot == null)
            return;

        ShowWinningMarker(result.WinningSlot.Id);
    }

    private void CacheNumberAreas()
    {
        numberAreasBySlotId.Clear();

        Transform root = numberAreasRoot != null ? numberAreasRoot : transform;
        RouletteNumberArea[] numberAreas = root.GetComponentsInChildren<RouletteNumberArea>(true);

        foreach (RouletteNumberArea numberArea in numberAreas)
        {
            if (string.IsNullOrWhiteSpace(numberArea.SlotId))
                continue;

            numberAreasBySlotId[numberArea.SlotId] = numberArea;
        }
    }

    private void CreateWinningMarker()
    {
        if (winningMarkerInstance != null)
            return;

        if (winningMarkerPrefab == null)
            throw new System.InvalidOperationException("Winning marker prefab is not assigned.");

        winningMarkerInstance = Instantiate(winningMarkerPrefab);
        winningMarkerInstance.SetActive(false);
    }

    public void HighlightTableButton(TableGameControls3DButton button)
    {
        ClearTableButtonHighlight();

        highlightedTableButton = button;

        if (highlightedTableButton != null)
            highlightedTableButton.SetHighlight(tableButtonHighlightMaterial);
    }

    public void ClearTableButtonHighlight()
    {
        if (highlightedTableButton != null)
        {
            highlightedTableButton.ClearHighlight();
            highlightedTableButton = null;
        }
    }
}
