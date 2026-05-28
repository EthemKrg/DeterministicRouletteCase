using UnityEngine;
using UnityEngine.InputSystem;

public class ChipSelection3DInputController : MonoBehaviour
{
    [SerializeField] private Camera rayCamera;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipSelection3DView selectionView;
    [SerializeField] private LayerMask chipSelectionLayerMask = ~0;
    [SerializeField] private float rayDistance = 100f;

    private void Awake()
    {
        if (rayCamera == null)
            throw new System.InvalidOperationException($"{nameof(ChipSelection3DInputController)} needs a ray camera reference.");

        if (gameFlowController == null)
            throw new System.InvalidOperationException($"{nameof(ChipSelection3DInputController)} needs a GameFlowController reference.");

        if (selectionView == null)
            throw new System.InvalidOperationException($"{nameof(ChipSelection3DInputController)} needs a ChipSelection3DView reference.");
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelect(Mouse.current.position.ReadValue());
            return;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            TrySelect(Touchscreen.current.primaryTouch.position.ReadValue());
    }

    private void TrySelect(Vector2 screenPosition)
    {
        if (!gameFlowController.TryCanAcceptGameplayInput(GameplayInputKind.ChipSelection, out _))
            return;

        Ray ray = rayCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, chipSelectionLayerMask))
            return;

        ChipSelection3DButton button = hit.collider.GetComponentInParent<ChipSelection3DButton>();

        if (button == null)
            return;

        selectionView.Select(button.Denomination);
    }
}
