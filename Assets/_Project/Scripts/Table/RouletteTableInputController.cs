using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RouletteTableInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera rayCamera;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private ChipSelectionController chipSelectionController;
    [SerializeField] private RouletteTableHighlightController highlightController;
    [SerializeField] private ChipStackViewController chipStackViewController;
    [SerializeField] private BalanceChipDisplayController balanceChipDisplayController;

    [Header("Raycast")]
    [SerializeField] private LayerMask betAreaLayerMask = ~0;
    [SerializeField] private LayerMask tableControlLayerMask = 1 << 7;
    [SerializeField] private float rayDistance = 100f;

    private RouletteBetArea hoveredBetArea;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Update()
    {
        UpdateHover();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHandlePointerPress(Mouse.current.position.ReadValue(), -1);
            return;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            TryHandlePointerPress(
                Touchscreen.current.primaryTouch.position.ReadValue(),
                Touchscreen.current.primaryTouch.touchId.ReadValue());
        }
    }

    private void TryHandlePointerPress(Vector2 screenPosition, int pointerId)
    {
        if (IsPointerBlockedByUi(pointerId) || IsPointerBlockedByTableControl(screenPosition))
            return;

        if (!TryGetTableHit(screenPosition, out RaycastHit hit))
            return;

        ChipStackView stackView = hit.collider.GetComponentInParent<ChipStackView>();
        if (stackView != null && chipStackViewController.TryUndoTopChip(stackView))
            return;

        RouletteBetArea betArea = hit.collider.GetComponentInParent<RouletteBetArea>();
        if (betArea == null)
            return;

        TryPlaceBet(betArea, chipSelectionController.SelectedChip, chipSelectionController.SelectedChipValue);
    }

    private void TryPlaceBet(RouletteBetArea betArea, ChipDenomination selectedChip, int stake)
    {
        if (gameFlowController.GameState.FlowState != GameFlowState.Betting)
            return;

        if (!betArea.IsAvailableForWheelType(gameFlowController.GameState.WheelType))
        {
            highlightController.ClearHighlight();
            return;
        }

        bool reservedStackPosition = false;

        try
        {
            RouletteBet bet = betArea.CreateBet(stake, gameFlowController.GameState.WheelType);

            if (balanceChipDisplayController != null)
            {
                Vector3 stackPosition = chipStackViewController.ReserveNextChipWorldPosition(betArea);
                reservedStackPosition = true;
                int stackVisualVersion = chipStackViewController.GetVisualVersion();
                balanceChipDisplayController.SetNextBetChipTarget(
                    selectedChip,
                    stackPosition,
                    () => chipStackViewController.ShowOrUpdateStack(betArea, selectedChip, bet.Stake, stackVisualVersion));
            }

            gameFlowController.PlacePreparedBet(bet);

            if (balanceChipDisplayController == null)
                chipStackViewController.ShowOrUpdateStack(betArea, selectedChip, bet.Stake);
        }
        catch (System.Exception exception)
        {
            if (balanceChipDisplayController != null)
                balanceChipDisplayController.ClearNextBetChipTarget();

            if (reservedStackPosition)
                chipStackViewController.CancelPendingChipReservation(betArea);

            gameFlowController.RequestFeedback(exception.Message);
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

        if (highlightController == null)
            throw new System.InvalidOperationException($"{nameof(RouletteTableInputController)} needs a RouletteTableHighlightController reference.");

        if (chipStackViewController == null)
            throw new System.InvalidOperationException($"{nameof(RouletteTableInputController)} needs a ChipStackViewController reference.");
    }

    private void UpdateHover()
    {
        if (Mouse.current == null || highlightController == null)
            return;

        RouletteBetArea betArea = GetBetAreaAtScreenPosition(Mouse.current.position.ReadValue());

        if (betArea == hoveredBetArea)
            return;

        hoveredBetArea = betArea;

        if (hoveredBetArea == null)
        {
            highlightController.ClearHighlight();
            return;
        }

        IReadOnlyList<string> previewSlotIds = hoveredBetArea.GetPreviewSlotIds(gameFlowController.GameState.WheelType);
        highlightController.HighlightBetArea(hoveredBetArea, previewSlotIds);
    }

    private RouletteBetArea GetBetAreaAtScreenPosition(Vector2 screenPosition)
    {
        if (IsPointerBlockedByUi(-1) || IsPointerBlockedByTableControl(screenPosition))
            return null;

        if (!TryGetTableHit(screenPosition, out RaycastHit hit))
            return null;

        return hit.collider.GetComponentInParent<RouletteBetArea>();
    }

    private bool IsPointerBlockedByUi(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId < 0)
            return EventSystem.current.IsPointerOverGameObject();

        return EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    private bool IsPointerBlockedByTableControl(Vector2 screenPosition)
    {
        if (rayCamera == null)
            return false;

        Ray ray = rayCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, tableControlLayerMask, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider.GetComponentInParent<TableGameControls3DButton>() != null;
    }

    private bool TryGetTableHit(Vector2 screenPosition, out RaycastHit hit)
    {
        Ray ray = rayCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hit, rayDistance, betAreaLayerMask);
    }
}
