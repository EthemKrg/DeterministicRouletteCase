using UnityEngine;

public class InsideBetDebug : MonoBehaviour
{
    [ContextMenu("Test Inside Bets")]
    private void TestInsideBets()
    {
        TestSplit();
        TestStreet();
        TestCorner();
        TestSixLine();
    }

    private void TestSplit()
    {
        RouletteBet split = RouletteBetFactory.CreateSplit(17, 20, 10);
        Debug.Log($"Split 17/20 covers: {string.Join(",", split.CoveredSlotIds)}");

        RoundResult result = BetResolver.Resolve(
            RouletteWheelData.GetSlotById("17", RouletteWheelType.European),
            new[] { split });

        Debug.Log($"Split 17/20 result 17 win: {result.HasAnyWinningBet}, Net: {result.NetProfit}");
    }

    private void TestStreet()
    {
        RouletteBet street = RouletteBetFactory.CreateStreet(16, 10);
        Debug.Log($"Street 16 covers: {string.Join(",", street.CoveredSlotIds)}");
    }

    private void TestCorner()
    {
        RouletteBet corner = RouletteBetFactory.CreateCorner(16, 10);
        Debug.Log($"Corner 16 covers: {string.Join(",", corner.CoveredSlotIds)}");
    }

    private void TestSixLine()
    {
        RouletteBet sixLine = RouletteBetFactory.CreateSixLine(16, 10);
        Debug.Log($"Six Line 16 covers: {string.Join(",", sixLine.CoveredSlotIds)}");
    }
}