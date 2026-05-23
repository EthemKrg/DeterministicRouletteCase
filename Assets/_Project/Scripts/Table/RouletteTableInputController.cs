using UnityEngine;
using UnityEngine.InputSystem;

public class RouletteTableInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera rayCamera;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipSelectionController chipSelectionController;

    [Header("Raycast")]
    [SerializeField] private LayerMask betAreaLayerMask = ~0;
    [SerializeField] private float rayDistance = 100f;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHandlePointerPress(Mouse.current.position.ReadValue());
            return;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            TryHandlePointerPress(Touchscreen.current.primaryTouch.position.ReadValue());
        }
    }

    private void TryHandlePointerPress(Vector2 screenPosition)
    {
        Ray ray = rayCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, betAreaLayerMask))
            return;

        RouletteBetArea betArea = hit.collider.GetComponentInParent<RouletteBetArea>();

        if (betArea == null)
            return;

        TryPlaceBet(betArea);
    }

    private void TryPlaceBet(RouletteBetArea betArea)
    {
        if (gameFlowController.GameState.FlowState != GameFlowState.Betting)
            return;

        try
        {
            int stake = chipSelectionController.SelectedChipValue;
            RouletteBet bet = betArea.CreateBet(stake, gameFlowController.GameState.WheelType);

            gameFlowController.PlacePreparedBet(bet);

            //Debug.Log($"Placed bet: {bet.Type}, stake {bet.Stake}, covers {string.Join(", ", bet.CoveredSlotIds)}");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }

    private void ValidateReferences()
    {
        if (rayCamera == null)
            throw new System.InvalidOperationException($"{nameof(RouletteTableInputController)} needs a ray camera reference.");

        if (gameFlowController == null)
            throw new System.InvalidOperationException($"{nameof(RouletteTableInputController)} needs a GameFlowController reference.");

        if (chipSelectionController == null)
            throw new System.InvalidOperationException($"{nameof(RouletteTableInputController)} needs a ChipSelectionController reference.");
    }
}