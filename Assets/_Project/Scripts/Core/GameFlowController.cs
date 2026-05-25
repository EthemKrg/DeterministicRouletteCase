using System;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public RouletteGameState GameState { get; private set; }
    public StatisticsTracker StatisticsTracker { get; private set; }
    public RoundResult LastRoundResult { get; private set; }

    public event Action OnGameStateChanged;
    public event Action<RoundResult> OnRoundResolved;
    public event Action<string> OnFeedbackRequested;
    public event Action OnBetsCleared;

    private void Awake()
    {
        GameState = new RouletteGameState();
        StatisticsTracker = new StatisticsTracker();

        NotifyStateChanged();
    }

    public void SetWheelType(RouletteWheelType wheelType)
    {
        bool changed = GameState.SetWheelType(wheelType);

        if (changed)
        {
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
        if (GameState.FlowState != GameFlowState.Betting)
            throw new InvalidOperationException("Spin can only start during betting state.");

        if (GameState.ActiveBets.Count == 0)
            throw new InvalidOperationException("Place at least one bet before spinning.");

        RouletteSlot winningSlot = GetSpinResult(winningSlotId);

        if (winningSlot == null)
            throw new ArgumentException($"Winning slot not found: {winningSlotId}", nameof(winningSlotId));

        GameState.SetFlowState(GameFlowState.Spinning);
        NotifyStateChanged();

        // Later this will wait for wheel animation.
        GameState.SetFlowState(GameFlowState.Resolving);

        LastRoundResult = BetResolver.Resolve(winningSlot, GameState.ActiveBets);

        GameState.ApplyRoundResult(LastRoundResult);
        StatisticsTracker.TrackRound(LastRoundResult);

        GameState.SetFlowState(GameFlowState.Betting);

        OnRoundResolved?.Invoke(LastRoundResult);
        NotifyStateChanged();
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
        GameState.ClearBets();
        OnBetsCleared?.Invoke();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnGameStateChanged?.Invoke();
    }

    public void PlacePreparedBet(RouletteBet bet)
    {
        PlaceBet(bet);
    }

    public bool TryRemoveLastBet(RouletteBet betTemplate, out int refundedStake, bool notifyStateChanged = true)
    {
        refundedStake = 0;

        if (GameState.FlowState != GameFlowState.Betting)
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
        GameState.PlaceBet(bet);

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
}
