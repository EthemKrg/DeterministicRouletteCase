using System.Linq;
using UnityEngine;

public class PayoutRulesDebug : MonoBehaviour
{
    private void Start()
    {
        TestPayoutData();
    }

    private void TestPayoutData()
    {
        var straightBet = new RouletteBet(BetType.Straight, 10, new[] { "17" });
        Debug.Log(straightBet.PayoutMultiplier); // 35
        Debug.Log(straightBet.GetProfit()); // 350
        Debug.Log(straightBet.GetTotalReturn()); // 360

        var redBet = new RouletteBet(BetType.Red, 10, RouletteWheelData.GetRedSlots().Select(slot => slot.Id));
        Debug.Log(redBet.PayoutMultiplier); // 1
        Debug.Log(redBet.GetProfit()); // 10
        Debug.Log(redBet.GetTotalReturn()); // 20

        var dozenBet = new RouletteBet(BetType.Dozen, 10, RouletteWheelData.GetDozenSlots(1).Select(slot => slot.Id));
        Debug.Log(dozenBet.PayoutMultiplier); // 2
        Debug.Log(dozenBet.GetProfit()); // 20
        Debug.Log(dozenBet.GetTotalReturn()); // 30
    }
}
