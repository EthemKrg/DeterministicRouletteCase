using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugGameFlowUI : MonoBehaviour
{
    private const string DefaultSlotId = "17";

    [Header("References")]
    [SerializeField] private GameFlowController gameFlowController;

    [Header("Inputs")]
    [SerializeField] private TMP_Dropdown wheelTypeDropdown;
    [SerializeField] private TMP_InputField winningSlotInput;
    [SerializeField] private TMP_InputField stakeInput;
    [SerializeField] private TMP_InputField straightBetSlotInput;

    [Header("Buttons")]
    [SerializeField] private Button addStraightButton;
    [SerializeField] private Button addRedButton;
    [SerializeField] private Button addBlackButton;
    [SerializeField] private Button addEvenButton;
    [SerializeField] private Button addOddButton;
    [SerializeField] private Button addLowButton;
    [SerializeField] private Button addHighButton;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button clearBetsButton;
    [SerializeField] private Button addDozen1Button;
    [SerializeField] private Button addDozen2Button;
    [SerializeField] private Button addDozen3Button;
    [SerializeField] private Button addColumn1Button;
    [SerializeField] private Button addColumn2Button;
    [SerializeField] private Button addColumn3Button;

    [Header("Output")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text lastRoundText;

    [SerializeField] private TMP_Text feedbackText;

    private void Awake()
    {
        addStraightButton.onClick.AddListener(AddStraightBet);

        addRedButton.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceRedBet(GetStake())));
        addBlackButton.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceBlackBet(GetStake())));
        addEvenButton.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceEvenBet(GetStake())));
        addOddButton.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceOddBet(GetStake())));
        addLowButton.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceLowBet(GetStake())));
        addHighButton.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceHighBet(GetStake())));

        addDozen1Button.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceDozenBet(1, GetStake())));
        addDozen2Button.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceDozenBet(2, GetStake())));
        addDozen3Button.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceDozenBet(3, GetStake())));

        addColumn1Button.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceColumnBet(1, GetStake())));
        addColumn2Button.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceColumnBet(2, GetStake())));
        addColumn3Button.onClick.AddListener(() => AddBet(() => gameFlowController.PlaceColumnBet(3, GetStake())));

        winningSlotInput.onEndEdit.AddListener(_ =>
        {
            ValidateInputs(true);
            UpdateActionButtons();
        });

        straightBetSlotInput.onEndEdit.AddListener(_ =>
        {
            ValidateInputs(true);
            UpdateActionButtons();
        });

        spinButton.onClick.AddListener(Spin);
        clearBetsButton.onClick.AddListener(ClearBets);
        wheelTypeDropdown.onValueChanged.AddListener(OnWheelTypeChanged);
    }

    private void Start()
    {
        SetDefaultInputValues();
        Refresh();
    }

    private void OnEnable()
    {
        gameFlowController.OnFeedbackRequested += HandleFeedbackRequested;
        gameFlowController.OnGameStateChanged += Refresh;
        gameFlowController.OnRoundResolved += RefreshLastRound;
        Refresh();
    }

    private void OnDisable()
    {
        gameFlowController.OnFeedbackRequested -= HandleFeedbackRequested;
        gameFlowController.OnGameStateChanged -= Refresh;
        gameFlowController.OnRoundResolved -= RefreshLastRound;
    }

    private void SetDefaultInputValues()
    {
        if (gameFlowController == null || gameFlowController.GameState == null)
            return;

        if (stakeInput != null && string.IsNullOrWhiteSpace(stakeInput.text))
            stakeInput.SetTextWithoutNotify(gameFlowController.GameState.MinBet.ToString());

        if (winningSlotInput != null && string.IsNullOrWhiteSpace(winningSlotInput.text))
            winningSlotInput.SetTextWithoutNotify(DefaultSlotId);

        if (straightBetSlotInput != null && string.IsNullOrWhiteSpace(straightBetSlotInput.text))
            straightBetSlotInput.SetTextWithoutNotify(DefaultSlotId);

        if (wheelTypeDropdown != null)
        {
            int wheelTypeIndex = gameFlowController.GameState.WheelType == RouletteWheelType.American ? 1 : 0;
            wheelTypeDropdown.SetValueWithoutNotify(wheelTypeIndex);
        }
    }

    private void HandleFeedbackRequested(string message)
    {
        ShowFeedback(message);
    }

    private void AddBet(Action placeBetAction)
    {
        try
        {
            placeBetAction?.Invoke();
            UpdateActionButtons();
        }
        catch (Exception exception)
        {
            ShowFeedback(exception.Message, true);
            UpdateActionButtons();
        }
    }

    // Straight bet input is separate from deterministic result input.
    private void AddStraightBet()
    {
        string slotId = straightBetSlotInput.text.Trim();

        if (!IsSlotValid(slotId))
        {
            ShowFeedback($"Straight bet slot '{slotId}' is not valid for {gameFlowController.GameState.WheelType} roulette.", true);
            return;
        }

        AddBet(() => gameFlowController.PlaceStraightBet(slotId, GetStake()));
    }

    private void Spin()
    {
        string slotId = winningSlotInput.text.Trim();

        if (!IsSlotValid(slotId))
        {
            ShowFeedback($"Result slot '{slotId}' is not valid for {gameFlowController.GameState.WheelType} roulette.", true);
            return;
        }

        try
        {
            gameFlowController.Spin(slotId);
        }
        catch (Exception exception)
        {
            ShowFeedback(exception.Message, true);
        }
    }

    private void ClearBets()
    {
        gameFlowController.ClearBets();
    }

    private void OnWheelTypeChanged(int index)
    {
        RouletteWheelType wheelType = index == 0
            ? RouletteWheelType.European
            : RouletteWheelType.American;

        try
        {
            bool hadActiveBets = gameFlowController.GameState.ActiveBets.Count > 0;

            gameFlowController.SetWheelType(wheelType);

            Refresh();
            ValidateInputs(true);
            UpdateActionButtons();

            if (hadActiveBets)
                ShowFeedback($"Wheel type set to {wheelType}. Active bets were refunded.");
            else
                ShowFeedback($"Wheel type set to {wheelType}.");
        }
        catch (Exception exception)
        {
            ShowFeedback(exception.Message, true);

            int currentIndex = gameFlowController.GameState.WheelType == RouletteWheelType.European ? 0 : 1;
            wheelTypeDropdown.SetValueWithoutNotify(currentIndex);

            Refresh();
            ValidateInputs(true);
            UpdateActionButtons();
        }
    }

    private int GetStake()
    {
        if (int.TryParse(stakeInput.text, out int stake))
            return stake;

        int defaultStake = gameFlowController.GameState.MinBet;
        stakeInput.text = defaultStake.ToString();
        ShowFeedback($"Invalid stake. Min stake is {defaultStake}.");

        return defaultStake;
    }

    private void Refresh()
    {
        if (gameFlowController == null || gameFlowController.GameState == null)
            return;

        RouletteGameState state = gameFlowController.GameState;
        PlayerStatistics stats = gameFlowController.StatisticsTracker.Statistics;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Wheel: {state.WheelType}");
        builder.AppendLine($"Chips: {state.CurrentChips}");
        builder.AppendLine($"Active Bets: {state.ActiveBets.Count}");
        builder.AppendLine();
        builder.AppendLine($"Spins: {stats.TotalSpins}");
        builder.AppendLine($"Wins: {stats.TotalWins}");
        builder.AppendLine($"Losses: {stats.TotalLosses}");
        builder.AppendLine($"Win Rate: {stats.WinRate:0.0}%");
        builder.AppendLine($"Profit/Loss: {stats.TotalProfitLoss}");
        builder.AppendLine($"Last Slot: {stats.LastWinningSlotId}");
        builder.AppendLine($"Last Round Net: {stats.LastRoundNetProfit}");

        stateText.text = builder.ToString();

        UpdateActionButtons();
    }

    private void RefreshLastRound(RoundResult result)
    {
        if (result == null)
            return;

        string feedback = result.HasAnyWinningBet ? "Winning bet hit!" : "No winning bets.";
        ShowFeedback(feedback);

        lastRoundText.text =
            $"Result: {result.WinningSlot.Id}\n" +
            $"Winning Bets: {result.GetWinningBets().Count}\n" +
            $"Losing Bets: {result.GetLosingBets().Count}\n" +
            $"Net Profit: {result.NetProfit}\n" +
            $"Player Win Feedback: {result.HasAnyWinningBet}";
    }

    private void ShowFeedback(string message, bool logAsWarning = false)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        if (logAsWarning)
            Debug.LogWarning(message);
    }

    private void UpdateActionButtons()
    {
        bool isWinningSlotValid = IsSlotValid(winningSlotInput.text.Trim());
        bool isStraightBetSlotValid = IsSlotValid(straightBetSlotInput.text.Trim());
        bool hasActiveBets = gameFlowController.GameState.ActiveBets.Count > 0;
        bool isBettingState = gameFlowController.GameState.FlowState == GameFlowState.Betting;

        spinButton.interactable = isWinningSlotValid && hasActiveBets && isBettingState;
        addStraightButton.interactable = isStraightBetSlotValid && isBettingState;
        clearBetsButton.interactable = hasActiveBets && isBettingState;
    }

    private bool ValidateInputs(bool showFeedback)
    {
        string resultSlotId = winningSlotInput.text.Trim();
        string straightSlotId = straightBetSlotInput.text.Trim();

        if (!IsSlotValid(resultSlotId))
        {
            if (showFeedback)
                ShowFeedback($"Result slot '{resultSlotId}' is not valid for {gameFlowController.GameState.WheelType} roulette.", true);

            return false;
        }

        if (!IsSlotValid(straightSlotId))
        {
            if (showFeedback)
                ShowFeedback($"Straight bet slot '{straightSlotId}' is not valid for {gameFlowController.GameState.WheelType} roulette.", true);

            return false;
        }

        if (showFeedback)
            ShowFeedback(string.Empty);

        return true;
    }

    private bool IsSlotValid(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return false;

        return RouletteWheelData.GetSlotById(slotId.Trim(), gameFlowController.GameState.WheelType) != null;
    }
}