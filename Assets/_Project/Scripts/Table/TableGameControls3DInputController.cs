using UnityEngine;
using UnityEngine.InputSystem;

public class TableGameControls3DInputController : MonoBehaviour
{
    [SerializeField] private Camera rayCamera;
    [SerializeField] private TableGameControls3DView controlsView;
    [SerializeField] private RouletteTableHighlightController highlightController;
    [SerializeField] private LayerMask controlsLayerMask = 1 << 7;
    [SerializeField] private float rayDistance = 100f;

    private TableGameControls3DButton lastHoveredButton;

    private void Awake()
    {
        if (rayCamera == null)
            throw new System.InvalidOperationException($"{nameof(TableGameControls3DInputController)} needs a ray camera reference.");

        if (controlsView == null)
            throw new System.InvalidOperationException($"{nameof(TableGameControls3DInputController)} needs a TableGameControls3DView reference.");
    }

    private void Update()
    {
        UpdateHover();

        if (rayCamera == null || controlsView == null)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPress(Mouse.current.position.ReadValue());
            return;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            TryPress(Touchscreen.current.primaryTouch.position.ReadValue());
    }

    private void UpdateHover()
    {
        if (Mouse.current == null || highlightController == null)
            return;

        TableGameControls3DButton hovered = GetButtonAtScreenPosition(Mouse.current.position.ReadValue());

        if (hovered == lastHoveredButton)
            return;

        // Exit previous
        if (lastHoveredButton != null)
            highlightController.ClearTableButtonHighlight();

        // Enter new
        if (hovered != null && hovered.IsInteractable)
            highlightController.HighlightTableButton(hovered);

        lastHoveredButton = hovered;
    }

    private void TryPress(Vector2 screenPosition)
    {
        TableGameControls3DButton button = GetButtonAtScreenPosition(screenPosition);

        if (button == null)
            return;

        controlsView.HandleButtonPressed(button);
    }

    private TableGameControls3DButton GetButtonAtScreenPosition(Vector2 screenPosition)
    {
        if (rayCamera == null)
            return null;

        Ray ray = rayCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, controlsLayerMask, QueryTriggerInteraction.Ignore))
            return null;

        return hit.collider.GetComponentInParent<TableGameControls3DButton>();
    }
}
