using UnityEngine;

public class TableGameControls3DButton : Selectable3DButton
{
    public enum ControlAction
    {
        None = 0,
        Spin,
        ClearBets,
        ToggleWheelType
    }

    [SerializeField] private ControlAction action;

    [Header("Highlight")]
    [SerializeField] private MeshRenderer buttonRenderer;
    private Material normalMaterial;

    public ControlAction Action => action;

    private bool isInteractable = true;
    public bool IsInteractable => isInteractable;
    public MeshRenderer ButtonRenderer => buttonRenderer;

    protected override void Awake()
    {
        base.Awake();
        CacheNormalMaterial();
    }

    public void SetState(bool interactable)
    {
        isInteractable = interactable;
    }

    public void SetHighlight(Material highlightMaterial)
    {
        if (buttonRenderer != null && highlightMaterial != null)
            buttonRenderer.sharedMaterial = highlightMaterial;
    }

    public void ClearHighlight()
    {
        if (buttonRenderer != null && normalMaterial != null)
            buttonRenderer.sharedMaterial = normalMaterial;
    }

    private void CacheNormalMaterial()
    {
        if (buttonRenderer != null)
            normalMaterial = buttonRenderer.sharedMaterial;
    }
}
