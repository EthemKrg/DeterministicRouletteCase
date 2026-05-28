using System;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public RouletteGameState GameState { get; private set; }
    public StatisticsTracker StatisticsTracker { get; private set; }
    public RoundResult LastRoundResult { get; private set; }

    public event Action OnGameStateChanged;
    public event Action OnBetPlaced;
    public event Action<RoundResult> OnRoundResolved;
    public event Action<string> OnFeedbackRequested;
    public event Action OnBetsCleared;

    [Header("Wheel Animation")]
    [SerializeField] private RouletteWheelSpinAnimator wheelAnimator;
    [SerializeField] private CameraAnimationController cameraAnimationController;

    private SpinSession pendingSpinSession;

    public bool IsSpinSessionActive => pendingSpinSession != null;
    public bool CanAcceptGameplayInput => TryCanAcceptGameplayInput(GameplayInputKind.TableControl, out _);

    private void Awake()
    {
        GameState = new RouletteGameState();
        StatisticsTracker = new StatisticsTracker();

        NotifyStateChanged();
    }

    private void OnEnable()
    {
        if (cameraAnimationController != null)
            cameraAnimationController.OnBettingInputReadinessChanged += HandleBettingInputReadinessChanged;
    }

    private void OnDisable()
    {
        if (cameraAnimationController != null)
            cameraAnimationController.OnBettingInputReadinessChanged -= HandleBettingInputReadinessChanged;
    }

    private void Update()
    {
        if (pendingSpinSession == null)
        {
            RecoverVisualStateIfNeeded();
            return;
        }

        if (Time.unscaledTime < pendingSpinSession.CompleteAt)
            return;

        CompleteSpinSession();
    }

    private void OnDestroy()
    {
        pendingSpinSession = null;
    }

    public void SetWheelType(RouletteWheelType wheelType)
    {
        if (!TryCanAcceptGameplayInput(GameplayInputKind.TableControl, out string reason))
            throw new InvalidOperationException(reason);

        bool changed = GameState.SetWheelType(wheelType);

        if (changed)
        {
            if (wheelAnimator != null)
                wheelAnimator.SetWheelType(wheelType);

            OnBetsCleared?.Invoke();
            OnFeedbackRequested?.Invoke("Wheel type changed. Active bets were cleared.");
        }

        NotifyStateChanged();
    }

    public void PlaceStraightBet(string slotId, int stake)
    {
        PlaceBet(RouletteBetFactory.CreateStraight(slotId, stake, GameState.WheelType));
    }

    public void PlaceRedBet(int stake)
    {
        PlaceBet(RouletteBetFactory.CreateRed(stake, GameState.WheelType));
    }

    public void PlaceSplitBet(int firstNumber, int secondNumber, int stake)
    {
        PlaceBet(RouletteBetFactory.CreateSplit(firstNumber, secondNumber, stake));
    }

    public void PlaceStreetBet(int startNumber, int stake)
    {
        PlaceBet(RouletteBetFactory.CreateStreet(startNumber, stake));
    }

    public void PlaceCornerBet(int bottomLeftNumber, int stake)
    {
        PlaceBet(RouletteBetFactory.CreateCorner(bottomLeftNumber, stake));
    }

    public void PlaceSixLineBet(int bottomStartNumber, int stake)
    {
        PlaceBet(RouletteBetFactory.CreateSixLine(bottomStartNumber, stake));
    }

    public void PlaceBlackBet(int stake)
    {
        PlaceBet(RouletteBetFactory.CreateBlack(stake, GameState.WheelType));
    }

    public void PlaceEvenBet(int stake)
    {
        PlaceBet(RouletteBetFactory.CreateEven(stake, GameState.WheelType));
    }

    public void PlaceOddBet(int stake)
    {
        PlaceBet(RouletteBetFactory.CreateOdd(stake, GameState.WheelType));
    }

    public void PlaceLowBet(int stake)
    {
        PlaceBet(RouletteBetFactory.CreateLow(stake, GameState.WheelType));
    }

    public void PlaceHighBet(int stake)
    {
        PlaceBet(RouletteBetFactory.CreateHigh(stake, GameState.WheelType));
    }

    public void PlaceDozenBet(int dozenIndex, int stake)
    {
        PlaceBet(RouletteBetFactory.CreateDozen(dozenIndex, stake, GameState.WheelType));
    }

    public void PlaceColumnBet(int columnIndex, int stake)
    {
        PlaceBet(RouletteBetFactory.CreateColumn(columnIndex, stake, GameState.WheelType));
    }

    public void Spin(string winningSlotId)
    {
        if (!TryCanAcceptGameplayInput(GameplayInputKind.TableControl, out string inputReason))
            throw new InvalidOperationException(inputReason);

        if (GameState.ActiveBets.Count == 0)
            throw new InvalidOperationException("Place at least one bet before spinning.");

        RouletteSlot winningSlot = GetSpinResult(winningSlotId);

        if (winningSlot == null)
            throw new ArgumentException($"Winning slot not found: {winningSlotId}", nameof(winningSlotId));

        RoundResult pendingResult = BetResolver.Resolve(winningSlot, GameState.ActiveBets);
        float visualDuration = StartSpinVisual(winningSlot);

        pendingSpinSession = new SpinSession(pendingResult, Time.unscaledTime + visualDuration);

        GameState.SetFlowState(GameFlowState.Spinning);
        NotifyStateChanged();
    }

    public bool TryCanAcceptGameplayInput(GameplayInputKind kind, out string reason)
    {
        reason = string.Empty;

        if (GameState == null)
        {
            reason = "Game state is not ready.";
            return false;
        }

        if (GameState.FlowState == GameFlowState.Spinning && pendingSpinSession == null)
        {
            RecoverInvalidSpinState("Spinning state had no active spin session.");
            reason = "Game recovered from an invalid spin state. Try again.";
            return false;
        }

        if (pendingSpinSession != null)
        {
            reason = "Spin is in progress.";
            return false;
        }

        if (GameState.FlowState != GameFlowState.Betting)
        {
            reason = "Gameplay input is only accepted during betting state.";
            return false;
        }

        if (wheelAnimator != null && wheelAnimator.IsSpinAnimationPlaying)
        {
            RecoverInvalidSpinState("Wheel animation was still playing while game state was betting.");
            reason = "Wheel animation state was reset. Try again.";
            return false;
        }

        if (cameraAnimationController != null && !cameraAnimationController.IsReadyForBettingInput)
        {
            reason = "Betting view is not ready yet.";
            return false;
        }

        return true;
    }

    private float StartSpinVisual(RouletteSlot winningSlot)
    {
        if (wheelAnimator == null)
            return 0f;

        return wheelAnimator.PlaySpin(winningSlot);
    }

    private void CompleteSpinSession()
    {
        if (pendingSpinSession == null)
            return;

        RoundResult result = pendingSpinSession.Result;
        pendingSpinSession = null;

        if (wheelAnimator != null)
            wheelAnimator.StopSpinAnimation();

        GameState.SetFlowState(GameFlowState.Resolving);
        NotifyStateChanged();

        try
        {
            LastRoundResult = result;

            GameState.ApplyRoundResult(LastRoundResult);
            StatisticsTracker.TrackRound(LastRoundResult, GameState.WheelType);

            GameState.SetFlowState(GameFlowState.Betting);

            OnRoundResolved?.Invoke(LastRoundResult);
            NotifyStateChanged();
        }
        catch (Exception exception)
        {
            Debug.LogError($"GameFlowController: Spin resolution failed with exception: {exception.Message}");
            RecoverInvalidSpinState(exception.Message);
        }
    }

    private void RecoverInvalidSpinState(string reason)
    {
        pendingSpinSession = null;

        if (wheelAnimator != null)
            wheelAnimator.StopSpinAnimation();

        GameState.SetFlowState(GameFlowState.Betting);
        NotifyStateChanged();

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Debug.LogWarning($"GameFlowController recovered state: {reason}");
            OnFeedbackRequested?.Invoke("Game state recovered. Try again.");
        }
    }

    private void RecoverVisualStateIfNeeded()
    {
        if (GameState == null)
            return;

        if (GameState.FlowState == GameFlowState.Spinning)
        {
            RecoverInvalidSpinState("Spinning state had no active spin session.");
            return;
        }

        if (GameState.FlowState == GameFlowState.Betting &&
            wheelAnimator != null &&
            wheelAnimator.IsSpinAnimationPlaying)
        {
            RecoverInvalidSpinState("Wheel animation was still playing while game state was betting.");
        }
    }

    private RouletteSlot GetSpinResult(string winningSlotId)
    {
        if (!string.IsNullOrWhiteSpace(winningSlotId))
            return RouletteWheelData.GetSlotById(winningSlotId.Trim(), GameState.WheelType);

        var slots = RouletteWheelData.GetSlots(GameState.WheelType);
        int randomIndex = UnityEngine.Random.Range(0, slots.Count);
        return slots[randomIndex];
    }

    public void ClearBets()
    {
        if (!TryCanAcceptGameplayInput(GameplayInputKind.TableControl, out string reason))
            throw new InvalidOperationException(reason);

        bool hadActiveBets = GameState.ActiveBets.Count > 0;

        GameState.ClearBets();
        OnBetsCleared?.Invoke();

        if (hadActiveBets)
            OnFeedbackRequested?.Invoke("Bets cleared.");

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnGameStateChanged?.Invoke();
    }

    private void HandleBettingInputReadinessChanged()
    {
        if (GameState == null ||
            GameState.FlowState != GameFlowState.Betting ||
            cameraAnimationController == null ||
            !cameraAnimationController.IsReadyForBettingInput)
        {
            return;
        }

        NotifyStateChanged();
    }

    public void PlacePreparedBet(RouletteBet bet)
    {
        PlaceBet(bet);
    }

    public bool TryRemoveLastBet(RouletteBet betTemplate, out int refundedStake, bool notifyStateChanged = true)
    {
        refundedStake = 0;

        if (!TryCanAcceptGameplayInput(GameplayInputKind.BetUndo, out _))
            return false;

        bool removed = GameState.TryRemoveLastMatchingBet(betTemplate, out refundedStake);

        if (removed && notifyStateChanged)
            NotifyStateChanged();

        return removed;
    }

    public void RefreshStateViews()
    {
        NotifyStateChanged();
    }

    public void RequestFeedback(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        OnFeedbackRequested?.Invoke(message);
    }

    private void PlaceBet(RouletteBet bet)
    {
        if (!TryCanAcceptGameplayInput(GameplayInputKind.BetPlacement, out string reason))
            throw new InvalidOperationException(reason);

        GameState.PlaceBet(bet);

        OnBetPlaced?.Invoke();
        OnFeedbackRequested?.Invoke(CreateBetPlacedMessage(bet));

        NotifyStateChanged();
    }

    private string CreateBetPlacedMessage(RouletteBet bet)
    {
        return $"Placed {bet.Stake} chips on {GetBetTargetText(bet)}.";
    }

    private string GetBetTargetText(RouletteBet bet)
    {
        switch (bet.Type)
        {
            case BetType.Straight:
                return $"Straight {bet.CoveredSlotIds[0]}";

            case BetType.Split:
                return $"Split {bet.CoveredSlotIds[0]}/{bet.CoveredSlotIds[1]}";

            case BetType.Street:
                return $"Street ({string.Join(", ", bet.CoveredSlotIds)})";

            case BetType.Corner:
                return $"Corner ({string.Join(", ", bet.CoveredSlotIds)})";

            case BetType.SixLine:
                return $"Six Line ({string.Join(", ", bet.CoveredSlotIds)})";

            case BetType.Red:
            case BetType.Black:
            case BetType.Even:
            case BetType.Odd:
            case BetType.Low:
            case BetType.High:
                return bet.Type.ToString();

            case BetType.Dozen:
                return $"Dozen ({GetRangeText(bet)})";

            case BetType.Column:
                return $"Column ({string.Join(", ", bet.CoveredSlotIds)})";

            default:
                return bet.Type.ToString();
        }
    }

    private string GetRangeText(RouletteBet bet)
    {
        int min = int.MaxValue;
        int max = int.MinValue;

        foreach (string slotId in bet.CoveredSlotIds)
        {
            if (!int.TryParse(slotId, out int number))
                continue;

            if (number < min)
                min = number;

            if (number > max)
                max = number;
        }

        if (min == int.MaxValue || max == int.MinValue)
            return string.Join(", ", bet.CoveredSlotIds);

        return $"{min}-{max}";
    }

    public SaveGameData ExportSaveData()
    {
        SaveGameData data = new SaveGameData();
        data.version = 1;

        data.gameState = GameState.ExportSnapshot();
        data.overallStats = StatisticsTracker.ExportOverallSnapshot();
        data.europeanStats = StatisticsTracker.ExportEuropeanSnapshot();
        data.americanStats = StatisticsTracker.ExportAmericanSnapshot();

        if (LastRoundResult != null)
        {
            data.lastRound = new LastRoundSaveData
            {
                winningSlotId = LastRoundResult.WinningSlot?.Id ?? string.Empty,
                wheelType = GameState.WheelType.ToString(),
                totalStake = LastRoundResult.TotalStake,
                totalReturn = LastRoundResult.TotalReturn,
                netProfit = LastRoundResult.NetProfit,
                winningBetCount = LastRoundResult.GetWinningBets().Count,
                losingBetCount = LastRoundResult.GetLosingBets().Count
            };
        }

        return data;
    }

    public void RestoreFromSave(SaveGameData saveData)
    {
        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));

        if (saveData.gameState == null)
            throw new ArgumentException("Save data has no game state.");

        GameState.RestoreSnapshot(saveData.gameState);

        StatisticsTracker.RestoreFromSnapshots(
            saveData.overallStats,
            saveData.europeanStats,
            saveData.americanStats);

        LastRoundResult = null;
        pendingSpinSession = null;

        if (wheelAnimator != null)
            wheelAnimator.StopSpinAnimation();
    }

    public void ClearSaveData()
    {
        GameState = new RouletteGameState();
        StatisticsTracker = new StatisticsTracker();
        LastRoundResult = null;
        pendingSpinSession = null;

        if (wheelAnimator != null)
            wheelAnimator.StopSpinAnimation();
    }


    public System.Collections.Generic.IReadOnlyList<RouletteBet> GetActiveBetsForSnapshot()
    {
        return GameState.ActiveBets;
    }

    private class SpinSession
    {
        public SpinSession(RoundResult result, float completeAt)
        {
            Result = result;
            CompleteAt = completeAt;
        }

        public RoundResult Result { get; }
        public float CompleteAt { get; }
    }
}
