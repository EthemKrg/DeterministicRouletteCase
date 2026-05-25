using System.Collections.Generic;
using UnityEngine;
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
        RouletteBetArea betArea = GetBetAreaAtScreenPosition(screenPosition);

        if (betArea == null)
            return;

        TryPlaceBet(betArea);
    }

    private void TryPlaceBet(RouletteBetArea betArea)
    {
        if (gameFlowController.GameState.FlowState != GameFlowState.Betting)
            return;

        if (!betArea.IsAvailableForWheelType(gameFlowController.GameState.WheelType))
        {
            highlightController.ClearHighlight();
            return;
        }

        try
        {
            int stake = chipSelectionController.SelectedChipValue;
            ChipDenomination selectedChip = chipSelectionController.SelectedChip;
            RouletteBet bet = betArea.CreateBet(stake, gameFlowController.GameState.WheelType);

            if (balanceChipDisplayController != null)
            {
                Vector3 stackPosition = chipStackViewController.GetNextChipWorldPosition(betArea);
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
        highlightController.HighlightSlots(previewSlotIds);
    }

    private RouletteBetArea GetBetAreaAtScreenPosition(Vector2 screenPosition)
    {
        Ray ray = rayCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, betAreaLayerMask))
            return null;

        return hit.collider.GetComponentInParent<RouletteBetArea>();
    }
}
