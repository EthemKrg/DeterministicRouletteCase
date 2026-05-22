using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Inspector data for resolver tests.
[Serializable]
public class DebugBetData
{
    public BetType betType;
    public int stake = 10;
    public List<string> coveredSlotIds = new List<string>();
}

public class BetResolverDebug : MonoBehaviour
{
    [SerializeField] private RouletteWheelType wheelType = RouletteWheelType.European;
    [SerializeField] private string winningSlotId = "17";
    [SerializeField] private List<DebugBetData> testBets = new List<DebugBetData>();

    [ContextMenu("Test Resolve")]
    private void TestResolve()
    {
        RouletteSlot winningSlot = RouletteWheelData.GetSlotById(winningSlotId, wheelType);

        if (winningSlot == null)
        {
            Debug.LogError($"Winning slot not found. Id: {winningSlotId}, Wheel Type: {wheelType}");
            return;
        }

        List<RouletteBet> bets = testBets.Count > 0
            ? CreateBetsFromDebugData()
            : CreateDefaultBets();

        if (bets.Count == 0)
            Debug.LogWarning("No valid debug bets were created.");

        RoundResult result = BetResolver.Resolve(winningSlot, bets);

        Debug.Log($"Winning slot: {result.WinningSlot.Id}");
        Debug.Log($"Total stake: {result.TotalStake}");
        Debug.Log($"Total return: {result.TotalReturn}");
        Debug.Log($"Net profit: {result.NetProfit}");
        Debug.Log($"Has positive net profit: {result.HasPositiveNetProfit}");
        Debug.Log($"Has any winning bet: {result.HasAnyWinningBet}");
        Debug.Log($"Winning bets: {result.GetWinningBets().Count}");
        Debug.Log($"Losing bets: {result.GetLosingBets().Count}");
    }

    private List<RouletteBet> CreateBetsFromDebugData()
    {
        List<RouletteBet> bets = new List<RouletteBet>();

        foreach (DebugBetData data in testBets)
        {
            if (data.coveredSlotIds == null || data.coveredSlotIds.Count == 0)
            {
                Debug.LogWarning($"Skipped debug bet because it has no covered slots. Type: {data.betType}");
                continue;
            }

            bets.Add(new RouletteBet(data.betType, data.stake, data.coveredSlotIds));
        }

        return bets;
    }

    private List<RouletteBet> CreateDefaultBets()
    {
        return new List<RouletteBet>
        {
            new RouletteBet(BetType.Straight, 10, new[] { winningSlotId }),
            new RouletteBet(BetType.Red, 10, RouletteWheelData.GetRedSlots(wheelType).Select(slot => slot.Id)),
            new RouletteBet(BetType.Even, 10, RouletteWheelData.GetEvenSlots(wheelType).Select(slot => slot.Id))
        };
    }
}