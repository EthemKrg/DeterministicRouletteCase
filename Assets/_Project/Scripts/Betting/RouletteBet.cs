using System;
using System.Collections.Generic;
using System.Linq;

public class RouletteBet
{
    private readonly List<string> coveredSlotIds;

    public BetType Type { get; private set; }
    public int Stake { get; private set; }
    public int PayoutMultiplier { get; private set; }
    public IReadOnlyList<string> CoveredSlotIds => coveredSlotIds;

    public RouletteBet(BetType type, int stake, int payoutMultiplier, IEnumerable<string> coveredSlotIds)
    {
        if (stake <= 0)
            throw new ArgumentException("Stake must be greater than zero.", nameof(stake));

        if (payoutMultiplier <= 0)
            throw new ArgumentException("Payout multiplier must be greater than zero.", nameof(payoutMultiplier));

        if (coveredSlotIds == null)
            throw new ArgumentNullException(nameof(coveredSlotIds));

        this.coveredSlotIds = coveredSlotIds.ToList();

        if (this.coveredSlotIds.Count == 0)
            throw new ArgumentException("Bet must cover at least one slot.", nameof(coveredSlotIds));

        Type = type;
        Stake = stake;
        PayoutMultiplier = payoutMultiplier;
    }

    public bool CoversSlot(string slotId)
    {
        return coveredSlotIds.Contains(slotId);
    }

    public int GetProfit()
    {
        return Stake * PayoutMultiplier;
    }

    // Total return includes the original stake plus profit.
    public int GetTotalReturn()
    {
        return Stake + GetProfit();
    }
}