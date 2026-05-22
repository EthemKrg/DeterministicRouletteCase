using System;

public class ResolvedBet
{
    public RouletteBet Bet { get; private set; }
    public bool IsWin { get; private set; }
    public int Profit { get; private set; }
    public int TotalReturn { get; private set; }

    // Net result is used for profit/loss tracking.
    public int NetResult { get; private set; }

    public ResolvedBet(RouletteBet bet, bool isWin)
    {
        if (bet == null)
            throw new ArgumentNullException(nameof(bet));

        Bet = bet;
        IsWin = isWin;

        if (isWin)
        {
            Profit = bet.GetProfit();
            TotalReturn = bet.GetTotalReturn();
            NetResult = Profit;
        }
        else
        {
            Profit = 0;
            TotalReturn = 0;
            NetResult = -bet.Stake;
        }
    }
}