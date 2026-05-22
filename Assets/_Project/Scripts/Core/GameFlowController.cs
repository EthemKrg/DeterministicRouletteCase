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
        PlaceBet(BetType.Straight, stake, new[] { slotId });
    }

    public void PlaceRedBet(int stake)
    {
        PlaceBet(BetType.Red, stake, RouletteWheelData.GetRedSlots(GameState.WheelType).Select(slot => slot.Id));
    }

    public void PlaceBlackBet(int stake)
    {
        PlaceBet(BetType.Black, stake, RouletteWheelData.GetBlackSlots(GameState.WheelType).Select(slot => slot.Id));
    }

    public void PlaceEvenBet(int stake)
    {
        PlaceBet(BetType.Even, stake, RouletteWheelData.GetEvenSlots(GameState.WheelType).Select(slot => slot.Id));
    }

    public void PlaceOddBet(int stake)
    {
        PlaceBet(BetType.Odd, stake, RouletteWheelData.GetOddSlots(GameState.WheelType).Select(slot => slot.Id));
    }

    public void PlaceLowBet(int stake)
    {
        PlaceBet(BetType.Low, stake, RouletteWheelData.GetLowSlots(GameState.WheelType).Select(slot => slot.Id));
    }

    public void PlaceHighBet(int stake)
    {
        PlaceBet(BetType.High, stake, RouletteWheelData.GetHighSlots(GameState.WheelType).Select(slot => slot.Id));
    }

    public void PlaceDozenBet(int dozenIndex, int stake)
    {
        PlaceBet(BetType.Dozen, stake, RouletteWheelData.GetDozenSlots(dozenIndex, GameState.WheelType).Select(slot => slot.Id));
    }

    public void PlaceColumnBet(int columnIndex, int stake)
    {
        PlaceBet(BetType.Column, stake, RouletteWheelData.GetColumnSlots(columnIndex, GameState.WheelType).Select(slot => slot.Id));
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

    private void PlaceBet(BetType betType, int stake, IEnumerable<string> coveredSlotIds)
    {
        RouletteBet bet = new RouletteBet(betType, stake, coveredSlotIds);
        GameState.PlaceBet(bet);

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnGameStateChanged?.Invoke();
    }
}