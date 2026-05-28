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

    [Header("Touch Preview")]
    [SerializeField] private bool keepTouchHighlightBrieflyAfterRelease = true;
    [SerializeField] private float touchHighlightReleaseHoldSeconds = 0.12f;

    private RouletteBetArea hoveredBetArea;
    private bool wasTouchPreviewActive;
    private float touchPreviewClearTime = -1f;

    private void Awake()
    {
        ValidateReferences();
    }

    private void Update()
    {
        UpdatePointerPreview();

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
        if (stackView != null)
        {
            if (!gameFlowController.TryCanAcceptGameplayInput(GameplayInputKind.BetUndo, out string undoReason))
            {
                gameFlowController.RequestFeedback(undoReason);
                return;
            }

            if (chipStackViewController.TryUndoTopChip(stackView))
                return;
        }

        RouletteBetArea betArea = hit.collider.GetComponentInParent<RouletteBetArea>();
        if (betArea == null)
            return;

        TryPlaceBet(betArea, chipSelectionController.SelectedChip, chipSelectionController.SelectedChipValue);
    }

    private void TryPlaceBet(RouletteBetArea betArea, ChipDenomination selectedChip, int stake)
    {
        if (!gameFlowController.TryCanAcceptGameplayInput(GameplayInputKind.BetPlacement, out string reason))
        {
            gameFlowController.RequestFeedback(reason);
            return;
        }

        if (!betArea.IsAvailableForWheelType(gameFlowController.GameState.WheelType))
        {
            ClearHoveredBetArea();
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
                    () => chipStackViewController.ShowOrUpdateStack(
                        betArea,
                        selectedChip,
                        bet.Stake,
                        stackVisualVersion));
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

    private void UpdatePointerPreview()
    {
        if (highlightController == null)
            return;

        if (TryGetActiveTouchPosition(out Vector2 touchPosition, out int touchPointerId))
        {
            wasTouchPreviewActive = true;
            touchPreviewClearTime = -1f;

            UpdateHoverAtScreenPosition(touchPosition, touchPointerId);
            return;
        }

        if (wasTouchPreviewActive)
        {
            if (keepTouchHighlightBrieflyAfterRelease)
            {
                if (touchPreviewClearTime < 0f)
                    touchPreviewClearTime = Time.time + touchHighlightReleaseHoldSeconds;

                if (Time.time < touchPreviewClearTime)
                    return;
            }

            wasTouchPreviewActive = false;
            touchPreviewClearTime = -1f;
            ClearHoveredBetArea();
            return;
        }

        if (Mouse.current != null)
            UpdateHoverAtScreenPosition(Mouse.current.position.ReadValue(), -1);
    }

    private bool TryGetActiveTouchPosition(out Vector2 screenPosition, out int pointerId)
    {
        if (Touchscreen.current != null)
        {
            var primaryTouch = Touchscreen.current.primaryTouch;

            if (primaryTouch.press.isPressed)
            {
                screenPosition = primaryTouch.position.ReadValue();
                pointerId = primaryTouch.touchId.ReadValue();
                return true;
            }
        }

        screenPosition = default;
        pointerId = -1;
        return false;
    }

    private void UpdateHoverAtScreenPosition(Vector2 screenPosition, int pointerId)
    {
        RouletteBetArea betArea = GetBetAreaAtScreenPosition(screenPosition, pointerId);

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

    private RouletteBetArea GetBetAreaAtScreenPosition(Vector2 screenPosition, int pointerId)
    {
        if (IsPointerBlockedByUi(pointerId) || IsPointerBlockedByTableControl(screenPosition))
            return null;

        if (!gameFlowController.TryCanAcceptGameplayInput(GameplayInputKind.BetPlacement, out _))
            return null;

        if (!TryGetTableHit(screenPosition, out RaycastHit hit))
            return null;

        return hit.collider.GetComponentInParent<RouletteBetArea>();
    }

    private void ClearHoveredBetArea()
    {
        if (hoveredBetArea == null)
            return;

        hoveredBetArea = null;
        highlightController.ClearHighlight();
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

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                tableControlLayerMask,
                QueryTriggerInteraction.Collide))
        {
            return false;
        }

        return hit.collider.GetComponentInParent<TableGameControls3DButton>() != null;
    }

    private bool TryGetTableHit(Vector2 screenPosition, out RaycastHit hit)
    {
        Ray ray = rayCamera.ScreenPointToRay(screenPosition);

        return Physics.Raycast(
            ray,
            out hit,
            rayDistance,
            betAreaLayerMask,
            QueryTriggerInteraction.Collide);
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
}