using TMPro;
using UnityEngine;

public class RouletteTableModeVisibilityController : MonoBehaviour
{
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private Transform betAreasRoot;
    [SerializeField] private TableLineAutoGenerator[] tableLineGenerators;

    private RouletteBetArea[] betAreas;
    private RouletteWheelType lastWheelType;
    private bool hasLastWheelType;

    private void Awake()
    {
        CacheBetAreas();
    }

    private void OnEnable()
    {
        if (gameFlowController != null)
            gameFlowController.OnGameStateChanged += RefreshVisibility;
    }

    private void OnDisable()
    {
        if (gameFlowController != null)
            gameFlowController.OnGameStateChanged -= RefreshVisibility;
    }

    private void Start()
    {
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (gameFlowController == null || gameFlowController.GameState == null)
        {
            throw new System.Exception("GameFlowController or GameState is not assigned. Please assign it in the inspector.");
        }

        if (betAreas == null || betAreas.Length == 0)
            CacheBetAreas();

        RouletteWheelType wheelType = gameFlowController.GameState.WheelType;
        bool isAmerican = wheelType == RouletteWheelType.American;
        bool wheelTypeChanged = !hasLastWheelType || lastWheelType != wheelType;

        foreach (RouletteBetArea betArea in betAreas)
        {
            if (betArea == null || !betArea.AmericanOnly)
                continue;

            SetAreaVisible(betArea.gameObject, isAmerican);
        }

        if (wheelTypeChanged)
            RebuildTableLines();

        lastWheelType = wheelType;
        hasLastWheelType = true;
    }

    private void CacheBetAreas()
    {
        Transform root = betAreasRoot != null ? betAreasRoot : transform;
        betAreas = root.GetComponentsInChildren<RouletteBetArea>(true);
    }

    private static void SetAreaVisible(GameObject area, bool visible)
    {
        // get component calls might be expensive, can be optimized by caching references if needed..
        Renderer[] renderers = area.GetComponentsInChildren<Renderer>(true);
        Collider[] colliders = area.GetComponentsInChildren<Collider>(true);
        TMP_Text[] labels = area.GetComponentsInChildren<TMP_Text>(true);

        foreach (Renderer renderer in renderers)
            renderer.enabled = visible;

        foreach (Collider collider in colliders)
            collider.enabled = visible;

        foreach (TMP_Text label in labels)
            label.enabled = visible;
    }

    private void RebuildTableLines()
    {
        if (tableLineGenerators == null)
            return;

        foreach (TableLineAutoGenerator tableLineGenerator in tableLineGenerators)
        {
            if (tableLineGenerator == null)
                continue;

            tableLineGenerator.GenerateLinesFromSource();
        }
    }
}
