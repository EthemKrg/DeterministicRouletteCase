using System;
using System.Collections.Generic;
using UnityEngine;

public class InsideBetDebug : MonoBehaviour
{
    [ContextMenu("Test Valid Bets")]
    private void TestValidBets()
    {
        TestSplit();
        TestStreet();
        TestCorner();
        TestSixLine();
    }

    [ContextMenu("Test Invalid Bets")]
    private void TestInvalidBets()
    {
        TestInvalidSplit(17, 19);
        TestInvalidStreet(17);
        TestInvalidCorner(18);
        TestInvalidCorner(34);
        TestInvalidSixLine(17);
        TestInvalidSixLine(34);
        TestInvalidSplit(0, 1);
        TestInvalidStreet(37);
    }

    #region valit bet tests
    private void TestSplit()
    {
        RouletteBet split = RouletteBetFactory.CreateSplit(17, 20, 10);
        Debug.Log($"Split 17/20 covers: {string.Join(",", split.CoveredSlotIds)}");

        RouletteBet horizontalSplit = RouletteBetFactory.CreateSplit(17, 18, 10);
        Debug.Log($"Split 17/18 covers: {string.Join(",", horizontalSplit.CoveredSlotIds)}");

        RouletteBet verticalSplit = RouletteBetFactory.CreateSplit(17, 20, 10);
        Debug.Log($"Split 17/20 covers: {string.Join(",", verticalSplit.CoveredSlotIds)}");

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

        RouletteBet cornerFromMiddleRow = RouletteBetFactory.CreateCorner(17, 10);
        Debug.Log($"Corner 17 covers: {string.Join(",", cornerFromMiddleRow.CoveredSlotIds)}");
    }

    private void TestSixLine()
    {
        RouletteBet sixLine = RouletteBetFactory.CreateSixLine(16, 10);
        Debug.Log($"Six Line 16 covers: {string.Join(",", sixLine.CoveredSlotIds)}");
    }
    #endregion

    #region Invalid Bet Tests
    private void TestInvalidSplit(int firstNumber, int secondNumber)
    {
        try
        {
            RouletteBetFactory.CreateSplit(firstNumber, secondNumber, 10);
            Debug.LogError($"Split {firstNumber}/{secondNumber} should be invalid.");
        }
        catch (Exception exception)
        {
            Debug.Log($"Split {firstNumber}/{secondNumber} correctly rejected: {exception.Message}");
        }
    }

    private void TestInvalidStreet(int startNumber)
    {
        try
        {
            RouletteBetFactory.CreateStreet(startNumber, 10);
            Debug.LogError($"Street {startNumber} should be invalid.");
        }
        catch (Exception exception)
        {
            Debug.Log($"Street {startNumber} correctly rejected: {exception.Message}");
        }
    }

    private void TestInvalidCorner(int bottomLeftNumber)
    {
        try
        {
            RouletteBetFactory.CreateCorner(bottomLeftNumber, 10);
            Debug.LogError($"Corner {bottomLeftNumber} should be invalid.");
        }
        catch (Exception exception)
        {
            Debug.Log($"Corner {bottomLeftNumber} correctly rejected: {exception.Message}");
        }
    }

    private void TestInvalidSixLine(int bottomStartNumber)
    {
        try
        {
            RouletteBetFactory.CreateSixLine(bottomStartNumber, 10);
            Debug.LogError($"Six Line {bottomStartNumber} should be invalid.");
        }
        catch (Exception exception)
        {
            Debug.Log($"Six Line {bottomStartNumber} correctly rejected:  {exception.Message}");
        }
    }
    #endregion

    [ContextMenu("Test Try Methods")]
    private void TestTryMethods()
    {
        if (RouletteTableLayout.TryGetSplitSlotIds(17, 19, out _))
            Debug.LogError("Try split 17/19 failed. It should be invalid.");
        else
            Debug.Log("Try split 17/19 correctly returned false.");

        if (RouletteTableLayout.TryGetCornerSlotIds(16, out IReadOnlyList<string> cornerSlots))
            Debug.Log($"Try corner 16 valid: {string.Join(",", cornerSlots)}");
        else
            Debug.LogError("Try corner 16 failed. It should be valid.");

        if (RouletteTableLayout.TryGetSixLineSlotIds(34, out _))
            Debug.LogError("Try six line 34 failed. It should be invalid.");
        else
            Debug.Log("Try six line 34 correctly returned false.");
    }
}