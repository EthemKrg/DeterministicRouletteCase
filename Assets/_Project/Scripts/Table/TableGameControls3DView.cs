using TMPro;
using UnityEngine;

public class TableGameControls3DView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private TMP_InputField winningSlotInput;
    [SerializeField] private TableGameControls3DButton[] buttons;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        gameFlowController.OnGameStateChanged += RefreshControls;
        winningSlotInput.onValueChanged.AddListener(HandleWinningSlotChanged);
        winningSlotInput.onEndEdit.AddListener(HandleWinningSlotEndEdit);
    }

    private void OnDisable()
    {
        gameFlowController.OnGameStateChanged -= RefreshControls;
        winningSlotInput.onValueChanged.RemoveListener(HandleWinningSlotChanged);
        winningSlotInput.onEndEdit.RemoveListener(HandleWinningSlotEndEdit);
    }

    private void Start()
    {
        RefreshControls();
    }

    public void HandleButtonPressed(TableGameControls3DButton button)
    {
        if (button == null || !button.IsInteractable)
            return;

        button.PlayPressAnimation();

        switch (button.Action)
        {
            case TableGameControls3DButton.ControlAction.Spin:
                Spin();
                break;

            case TableGameControls3DButton.ControlAction.ClearBets:
                ClearBets();
                break;

            case TableGameControls3DButton.ControlAction.ToggleWheelType:
                ToggleWheelType();
                break;
        }

        RefreshControls();
    }

    private void Spin()
    {
        string slotId = GetWinningSlotId();

        if (!IsOptionalResultSlotValid(slotId))
        {
            RequestFeedback($"Result slot '{slotId}' is not valid for {gameFlowController.GameState.WheelType} roulette.");
            return;
        }

        try
        {
            gameFlowController.Spin(slotId);
        }
        catch (System.Exception exception)
        {
            RequestFeedback(exception.Message);
        }
    }

    private void ClearBets()
    {
        try
        {
            gameFlowController.ClearBets();
        }
        catch (System.Exception exception)
        {
            RequestFeedback(exception.Message);
        }
    }

    private void ToggleWheelType()
    {
        RouletteWheelType targetWheelType = gameFlowController.GameState.WheelType == RouletteWheelType.European
            ? RouletteWheelType.American
            : RouletteWheelType.European;

        try
        {
            gameFlowController.SetWheelType(targetWheelType);
        }
        catch (System.Exception exception)
        {
            RequestFeedback(exception.Message);
        }
    }

    private void RefreshControls()
    {
        RouletteGameState state = gameFlowController.GameState;
        bool isBettingState = state.FlowState == GameFlowState.Betting;
        bool hasActiveBets = state.ActiveBets.Count > 0;
        bool isWinningSlotValid = IsOptionalResultSlotValid(GetWinningSlotId());
        bool isAmerican = state.WheelType == RouletteWheelType.American;

        winningSlotInput.interactable = isBettingState;

        foreach (TableGameControls3DButton button in buttons)
        {
            if (button == null)
                continue;

            switch (button.Action)
            {
                case TableGameControls3DButton.ControlAction.Spin:
                    button.SetLabel("SPIN");
                    button.SetState(isBettingState && hasActiveBets && isWinningSlotValid);
                    break;

                case TableGameControls3DButton.ControlAction.ClearBets:
                    button.SetLabel("CLEAR");
                    button.SetState(isBettingState && hasActiveBets);
                    break;

                case TableGameControls3DButton.ControlAction.ToggleWheelType:
                    button.SetLabel(isAmerican ? "US" : "EU");
                    button.SetState(isBettingState);
                    break;
            }
        }
    }

    private void HandleWinningSlotChanged(string _)
    {
        RefreshControls();
    }

    private void HandleWinningSlotEndEdit(string _)
    {
        string slotId = GetWinningSlotId();

        if (!IsOptionalResultSlotValid(slotId))
            RequestFeedback($"Result slot '{slotId}' is not valid for {gameFlowController.GameState.WheelType} roulette.");

        RefreshControls();
    }

    private string GetWinningSlotId()
    {
        return winningSlotInput.text.Trim();
    }

    private bool IsOptionalResultSlotValid(string slotId)
    {
        return string.IsNullOrWhiteSpace(slotId)
            || RouletteWheelData.GetSlotById(slotId.Trim(), gameFlowController.GameState.WheelType) != null;
    }

    private void RequestFeedback(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        gameFlowController.RequestFeedback(message);
    }

    private void ValidateReferences()
    {
        if (gameFlowController == null)
            throw new System.InvalidOperationException($"{nameof(TableGameControls3DView)} needs a GameFlowController reference.");

        if (winningSlotInput == null)
            throw new System.InvalidOperationException($"{nameof(TableGameControls3DView)} needs a winning slot input reference.");

        if (buttons == null || buttons.Length == 0)
            throw new System.InvalidOperationException($"{nameof(TableGameControls3DView)} needs table control button references.");
    }
}
