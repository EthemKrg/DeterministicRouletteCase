using System;
using System.Collections.Generic;
using System.Linq;

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
    public IReadOnlyList<RouletteBet> ActiveBets => activeBets;

    public int CurrentChips { get; private set; } = 50000;

    public int MinBet { get; private set; } = 250;
    public int MaxBetPerBet { get; private set; } = 10000;
    public int MaxTotalActiveBet { get; private set; } = 10000;

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

    public bool SetWheelType(RouletteWheelType wheelType)
    {
        if (!CanAcceptBets)
            throw new System.InvalidOperationException("Wheel type can only be changed during betting state.");

        if (WheelType == wheelType)
            return false;

        ClearBets();
        WheelType = wheelType;

        return true;
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

    public bool TryRemoveLastMatchingBet(RouletteBet betTemplate, out int refundedStake)
    {
        refundedStake = 0;

        if (!CanAcceptBets || betTemplate == null)
            return false;

        for (int i = activeBets.Count - 1; i >= 0; i--)
        {
            RouletteBet bet = activeBets[i];

            if (!HasSameTarget(bet, betTemplate))
                continue;

            refundedStake = bet.Stake;
            CurrentChips += refundedStake;
            activeBets.RemoveAt(i);
            return true;
        }

        return false;
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

    private bool HasSameTarget(RouletteBet first, RouletteBet second)
    {
        if (first.Type != second.Type)
            return false;

        if (first.CoveredSlotIds.Count != second.CoveredSlotIds.Count)
            return false;

        for (int i = 0; i < first.CoveredSlotIds.Count; i++)
        {
            if (first.CoveredSlotIds[i] != second.CoveredSlotIds[i])
                return false;
        }

        return true;
    }

    public GameStateSaveData ExportSnapshot()
    {
        BetSaveData[] betData = new BetSaveData[activeBets.Count];

        for (int i = 0; i < activeBets.Count; i++)
        {
            RouletteBet bet = activeBets[i];
            betData[i] = new BetSaveData
            {
                betType = bet.Type.ToString(),
                stake = bet.Stake,
                coveredSlotIds = bet.CoveredSlotIds.ToArray()
            };
        }

        return new GameStateSaveData
        {
            currentChips = CurrentChips,
            wheelType = WheelType.ToString(),
            activeBets = betData
        };
    }

    public void RestoreSnapshot(GameStateSaveData snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        if (snapshot.currentChips < 0)
            throw new ArgumentException($"Invalid current chips: {snapshot.currentChips}");

        if (!Enum.TryParse(snapshot.wheelType, out RouletteWheelType parsedWheelType))
            throw new ArgumentException($"Invalid wheel type: {snapshot.wheelType}");

        activeBets.Clear();
        WheelType = parsedWheelType;

        if (snapshot.activeBets != null && snapshot.activeBets.Length > 0)
        {
            foreach (BetSaveData betData in snapshot.activeBets)
            {
                ValidateAndAddBet(betData);
            }
        }

        if (TotalActiveStake > MaxTotalActiveBet)
        {
            activeBets.Clear();
            throw new InvalidOperationException(
                $"Total active stake ({TotalActiveStake}) exceeds maximum ({MaxTotalActiveBet}).");
        }

        CurrentChips = snapshot.currentChips;
        FlowState = GameFlowState.Betting;
    }

    private void ValidateAndAddBet(BetSaveData betData)
    {
        if (betData == null)
            throw new ArgumentException("Bet data is null.");

        if (betData.stake <= 0)
            throw new ArgumentException($"Invalid bet stake: {betData.stake}");

        if (!Enum.TryParse(betData.betType, out BetType parsedType))
            throw new ArgumentException($"Invalid bet type: {betData.betType}");

        if (betData.coveredSlotIds == null || betData.coveredSlotIds.Length == 0)
            throw new ArgumentException($"Bet of type {betData.betType} has no covered slot IDs.");

        foreach (string slotId in betData.coveredSlotIds)
        {
            RouletteSlot slot = RouletteWheelData.GetSlotById(slotId, WheelType);
            if (slot == null)
                throw new ArgumentException($"Slot '{slotId}' is not valid for {WheelType} roulette.");
        }

        RouletteBet restoredBet = new RouletteBet(parsedType, betData.stake, betData.coveredSlotIds);
        activeBets.Add(restoredBet);
    }
}
