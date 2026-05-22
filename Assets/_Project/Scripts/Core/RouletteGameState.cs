using System.Collections.Generic;

public class RouletteGameState
{
    public GameFlowState FlowState { get; private set; } = GameFlowState.Betting;

    public bool CanAcceptBets => FlowState == GameFlowState.Betting;

    public void SetFlowState(GameFlowState flowState)
    {
        FlowState = flowState;
    }

    private readonly List<RouletteBet> activeBets = new List<RouletteBet>();

    public RouletteWheelType WheelType { get; private set; } = RouletteWheelType.European;
    public int CurrentChips { get; private set; } = 1000;
    public IReadOnlyList<RouletteBet> ActiveBets => activeBets;

    public int MinBet { get; private set; } = 10;
    public int MaxBetPerBet { get; private set; } = 500;
    public int MaxTotalActiveBet { get; private set; } = 1000;

    public int TotalActiveStake
    {
        get
        {
            int total = 0;

            foreach (RouletteBet bet in activeBets)
                total += bet.Stake;

            return total;
        }
    }

    public void SetWheelType(RouletteWheelType wheelType)
    {
        if (activeBets.Count > 0)
            throw new System.InvalidOperationException("Wheel type cannot be changed while active bets exist.");

        WheelType = wheelType;
    }

    public bool CanPlaceBet(int stake, out string reason)
    {
        reason = string.Empty;

        if (stake < MinBet)
        {
            reason = $"Minimum bet is {MinBet}.";
            return false;
        }

        if (stake > MaxBetPerBet)
        {
            reason = $"Maximum bet per bet is {MaxBetPerBet}.";
            return false;
        }

        if (TotalActiveStake + stake > MaxTotalActiveBet)
        {
            reason = $"Maximum total active bet is {MaxTotalActiveBet}.";
            return false;
        }

        if (CurrentChips < stake)
        {
            reason = "Not enough chips.";
            return false;
        }

        return true;
    }

    public void PlaceBet(RouletteBet bet)
    {
        if (!CanAcceptBets)
            throw new System.InvalidOperationException("Bets can only be placed during betting state.");

        if (bet == null)
            throw new System.ArgumentNullException(nameof(bet));

        if (!CanPlaceBet(bet.Stake, out string reason))
            throw new System.InvalidOperationException(reason);

        activeBets.Add(bet);
        CurrentChips -= bet.Stake;
    }

    public void ApplyRoundResult(RoundResult roundResult)
    {
        if (roundResult == null)
            throw new System.ArgumentNullException(nameof(roundResult));

        CurrentChips += roundResult.TotalReturn;
        activeBets.Clear();
    }

    public void ClearBets()
    {
        if (!CanAcceptBets)
            throw new System.InvalidOperationException("Bets can only be cleared during betting state.");

        foreach (RouletteBet bet in activeBets)
        {
            CurrentChips += bet.Stake;
        }

        activeBets.Clear();
    }
}