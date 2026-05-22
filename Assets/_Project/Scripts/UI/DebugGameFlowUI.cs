using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugGameFlowUI : MonoBehaviour
{
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

        spinButton.onClick.AddListener(Spin);
        clearBetsButton.onClick.AddListener(ClearBets);
        wheelTypeDropdown.onValueChanged.AddListener(OnWheelTypeChanged);
    }

    private void OnEnable()
    {
        gameFlowController.OnGameStateChanged += Refresh;
        gameFlowController.OnRoundResolved += RefreshLastRound;
        Refresh();
    }

    private void OnDisable()
    {
        gameFlowController.OnGameStateChanged -= Refresh;
        gameFlowController.OnRoundResolved -= RefreshLastRound;
    }

    private void AddBet(System.Action placeBetAction)
    {
        try
        {
            placeBetAction?.Invoke();
            ShowFeedback("Bet placed.");
        }
        catch (System.Exception exception)
        {
            ShowFeedback(exception.Message);
        }
    }

    private void AddBet(System.Action placeBetAction)
    {
        try
        {
            placeBetAction?.Invoke();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }

    private void Spin()
    {
        try
        {
            gameFlowController.Spin(winningSlotInput.text);
        }
        catch (System.Exception exception)
        {
            ShowFeedback(exception.Message);
        }
    }

    private void ClearBets()
    {
        gameFlowController.ClearBets();
    }

    private void OnWheelTypeChanged(int index)
    {
        RouletteWheelType wheelType = index == 1
            ? RouletteWheelType.American
            : RouletteWheelType.European;

        try
        {
            gameFlowController.SetWheelType(wheelType);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(exception.Message);
            wheelTypeDropdown.SetValueWithoutNotify(gameFlowController.GameState.WheelType == RouletteWheelType.American ? 1 : 0);
        }
    }

    private int GetStake()
    {
        if (int.TryParse(stakeInput.text, out int stake))
            return stake;

        return 10;
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

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        Debug.LogWarning(message);
    }
}