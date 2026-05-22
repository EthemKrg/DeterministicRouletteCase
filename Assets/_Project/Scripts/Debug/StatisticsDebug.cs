using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatisticsDebug : MonoBehaviour
{
    [ContextMenu("Test Statistics")]
    private void TestStatistics()
    {
        StatisticsTracker tracker = new StatisticsTracker();

        RoundResult winningRound = CreateRound("17", new List<RouletteBet>
        {
            new RouletteBet(BetType.Straight, 10, new[] { "17" }),
            new RouletteBet(BetType.Red, 10, RouletteWheelData.GetRedSlots().Select(slot => slot.Id)),
            new RouletteBet(BetType.Even, 10, RouletteWheelData.GetEvenSlots().Select(slot => slot.Id))
        });

        tracker.TrackRound(winningRound);

        RoundResult losingRound = CreateRound("0", new List<RouletteBet>
        {
            new RouletteBet(BetType.Red, 10, RouletteWheelData.GetRedSlots().Select(slot => slot.Id)),
            new RouletteBet(BetType.Black, 10, RouletteWheelData.GetBlackSlots().Select(slot => slot.Id))
        });

        tracker.TrackRound(losingRound);

        PlayerStatistics stats = tracker.Statistics;

        Debug.Log($"Total spins: {stats.TotalSpins}");
        Debug.Log($"Total wins: {stats.TotalWins}");
        Debug.Log($"Total losses: {stats.TotalLosses}");
        Debug.Log($"Total profit/loss: {stats.TotalProfitLoss}");
        Debug.Log($"Last winning slot: {stats.LastWinningSlotId}");
        Debug.Log($"Last round net profit: {stats.LastRoundNetProfit}");
        Debug.Log($"Win rate: {stats.WinRate}%");
    }

    private RoundResult CreateRound(string winningSlotId, List<RouletteBet> bets)
    {
        RouletteSlot winningSlot = RouletteWheelData.GetSlotById(winningSlotId, RouletteWheelType.European);
        return BetResolver.Resolve(winningSlot, bets);
    }
}