public class PlayerStatistics
{
    public int TotalSpins { get; private set; }
    public int TotalWins { get; private set; }
    public int TotalLosses { get; private set; }
    public int TotalProfitLoss { get; private set; }

    public string LastWinningSlotId { get; private set; }
    public int LastRoundNetProfit { get; private set; }

    public float WinRate
    {
        get
        {
            if (TotalSpins == 0)
                return 0f;

            return (float)TotalWins / TotalSpins * 100f;
        }
    }

    public void ApplyRoundResult(RoundResult roundResult)
    {
        if (roundResult == null)
            throw new System.ArgumentNullException(nameof(roundResult));

        TotalSpins++;

        // Win count is based on player feedback, while profit/loss uses net result.
        if (roundResult.HasAnyWinningBet)
            TotalWins++;
        else
            TotalLosses++;

        TotalProfitLoss += roundResult.NetProfit;
        LastRoundNetProfit = roundResult.NetProfit;
        LastWinningSlotId = roundResult.WinningSlot.Id;
    }

    public void Reset()
    {
        TotalSpins = 0;
        TotalWins = 0;
        TotalLosses = 0;
        TotalProfitLoss = 0;
        LastWinningSlotId = string.Empty;
        LastRoundNetProfit = 0;
    }
}