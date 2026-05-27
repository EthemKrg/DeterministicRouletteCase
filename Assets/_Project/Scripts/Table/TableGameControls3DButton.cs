using TMPro;
using UnityEngine;

public class TableGameControls3DButton : Selectable3DButton
{
    public enum ControlAction
    {
        Spin,
        ClearBets,
        ToggleWheelType
    }

    [SerializeField] private ControlAction action;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private float disabledLabelAlpha = 0.45f;

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
        RefreshLabelState();
    }

    public void SetLabel(string label)
    {
        if (labelText != null)
            labelText.text = label;
    }

    public void SetState(bool interactable)
    {
        isInteractable = interactable;
        RefreshLabelState();
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

    private void RefreshLabelState()
    {
        if (labelText == null)
            return;

        Color color = labelText.color;
        color.a = isInteractable ? 1f : disabledLabelAlpha;
        labelText.color = color;
    }

    private void CacheNormalMaterial()
    {
        if (buttonRenderer != null)
            normalMaterial = buttonRenderer.sharedMaterial;
    }
}
