using UnityEngine;

public class RouletteWheelDataDebug : MonoBehaviour
{
    private void Start()
    {
        TestWheelData();
    }

    private void TestWheelData()
    {
        var europeanSlots = RouletteWheelData.GetSlots(RouletteWheelType.European);
        var americanSlots = RouletteWheelData.GetSlots(RouletteWheelType.American);

        Debug.Log($"European slot count: {europeanSlots.Count}");
        Debug.Log($"American slot count: {americanSlots.Count}");

        LogSlot("European 0", RouletteWheelData.GetSlotById("0", RouletteWheelType.European));
        LogSlot("European 00", RouletteWheelData.GetSlotById("00", RouletteWheelType.European));

        LogSlot("American 0", RouletteWheelData.GetSlotById("0", RouletteWheelType.American));
        LogSlot("American 00", RouletteWheelData.GetSlotById("00", RouletteWheelType.American));

        Debug.Log($"Red count: {RouletteWheelData.GetRedSlots().Count}");
        Debug.Log($"Black count: {RouletteWheelData.GetBlackSlots().Count}");
        Debug.Log($"Even count: {RouletteWheelData.GetEvenSlots().Count}");
        Debug.Log($"Odd count: {RouletteWheelData.GetOddSlots().Count}");
        Debug.Log($"Low count: {RouletteWheelData.GetLowSlots().Count}");
        Debug.Log($"High count: {RouletteWheelData.GetHighSlots().Count}");

        Debug.Log($"Dozen 1 count: {RouletteWheelData.GetDozenSlots(1).Count}");
        Debug.Log($"Dozen 2 count: {RouletteWheelData.GetDozenSlots(2).Count}");
        Debug.Log($"Dozen 3 count: {RouletteWheelData.GetDozenSlots(3).Count}");

        Debug.Log($"Column 1 count: {RouletteWheelData.GetColumnSlots(1).Count}");
        Debug.Log($"Column 2 count: {RouletteWheelData.GetColumnSlots(2).Count}");
        Debug.Log($"Column 3 count: {RouletteWheelData.GetColumnSlots(3).Count}");
    }

    private void LogSlot(string label, RouletteSlot slot)
    {
        if (slot == null)
        {
            Debug.Log($"{label}: null");
            return;
        }

        Debug.Log($"{label}: Id={slot.Id}, Number={slot.Number}, Color={slot.Color}, IsDoubleZero={slot.IsDoubleZero}");
    }
}