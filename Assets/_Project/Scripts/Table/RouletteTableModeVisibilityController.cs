using TMPro;
using UnityEngine;

public class RouletteTableModeVisibilityController : MonoBehaviour
{
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private Transform betAreasRoot;

    private RouletteBetArea[] betAreas;

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

        bool isAmerican = gameFlowController.GameState.WheelType == RouletteWheelType.American;

        foreach (RouletteBetArea betArea in betAreas)
        {
            if (betArea == null || !betArea.AmericanOnly)
                continue;

            SetAreaVisible(betArea.gameObject, isAmerican);
        }
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
}