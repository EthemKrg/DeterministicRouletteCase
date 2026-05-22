using System.Collections.Generic;
using System.Linq;

public class RoundResult
{
    private readonly List<ResolvedBet> resolvedBets;

    public RouletteSlot WinningSlot { get; private set; }
    public IReadOnlyList<ResolvedBet> ResolvedBets => resolvedBets;

    public int TotalStake { get; private set; }
    public int TotalReturn { get; private set; }
    public int NetProfit { get; private set; }

    public bool HasPositiveNetProfit => NetProfit > 0; // This indicates the player won more than they bet.
    public bool HasAnyWinningBet => resolvedBets.Any(result => result.IsWin);

    public RoundResult(RouletteSlot winningSlot, IEnumerable<ResolvedBet> resolvedBets)
    {
        if (winningSlot == null)
            throw new System.ArgumentNullException(nameof(winningSlot));

        if (resolvedBets == null)
            throw new System.ArgumentNullException(nameof(resolvedBets));

        WinningSlot = winningSlot;
        this.resolvedBets = resolvedBets.ToList();

        TotalStake = this.resolvedBets.Sum(result => result.Bet.Stake);
        TotalReturn = this.resolvedBets.Sum(result => result.TotalReturn);
        NetProfit = this.resolvedBets.Sum(result => result.NetResult);
    }

    public IReadOnlyList<ResolvedBet> GetWinningBets()
    {
        return resolvedBets.Where(result => result.IsWin).ToList();
    }

    public IReadOnlyList<ResolvedBet> GetLosingBets()
    {
        return resolvedBets.Where(result => !result.IsWin).ToList();
    }
}