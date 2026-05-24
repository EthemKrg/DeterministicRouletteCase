using UnityEngine;

public class ChipSelection3DButton : MonoBehaviour
{
    [SerializeField] private ChipDenomination denomination;
    [SerializeField] private Transform visualRoot;

    [Header("Selected State")]
    [SerializeField] private float selectedScaleMultiplier = 1.12f;

    private Vector3 defaultLocalPosition;
    private Vector3 defaultLocalScale;

    public ChipDenomination Denomination => denomination;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        defaultLocalPosition = visualRoot.localPosition;
        defaultLocalScale = visualRoot.localScale;
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            ApplySelectedVisual();
        else
            ResetVisual();
    }

    public void ResetVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = defaultLocalPosition;
        visualRoot.localScale = defaultLocalScale;
    }

    private void ApplySelectedVisual()
    {
        if (visualRoot == null)
            return;

        visualRoot.localScale = defaultLocalScale * selectedScaleMultiplier;
    }
}