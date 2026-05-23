using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public RouletteGameState GameState { get; private set; }
    public StatisticsTracker StatisticsTracker { get; private set; }
    public RoundResult LastRoundResult { get; private set; }

    public event Action OnGameStateChanged;
    public event Action<RoundResult> OnRoundResolved;
    public event Action<string> OnFeedbackRequested;

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
            OnFeedbackRequested?.Invoke("Wheel type changed. Active bets were cleared.");

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
        PlaceBet(RouletteBetFactory.CreateBlack(stake, GameState.WheelType));
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

        RouletteSlot winningSlot = RouletteWheelData.GetSlotById(winningSlotId, GameState.WheelType);

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

    public void ClearBets()
    {
        GameState.ClearBets();
        NotifyStateChanged();
    }

    private void PlaceBet(RouletteBet bet)
    {
        GameState.PlaceBet(bet);
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnGameStateChanged?.Invoke();
    }
}