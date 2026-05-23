using System;
using System.Linq;

public static class RouletteBetFactory
{
    public static RouletteBet CreateStraight(string slotId, int stake, RouletteWheelType wheelType)
    {
        RouletteSlot slot = RouletteWheelData.GetSlotById(slotId, wheelType);

        if (slot == null)
            throw new ArgumentException($"Slot '{slotId}' is not valid for {wheelType} roulette.", nameof(slotId));

        return new RouletteBet(BetType.Straight, stake, new[] { slot.Id });
    }

    public static RouletteBet CreateSplit(int firstNumber, int secondNumber, int stake)
    {
        return new RouletteBet(BetType.Split, stake, RouletteTableLayout.GetSplitSlotIds(firstNumber, secondNumber));
    }

    public static RouletteBet CreateStreet(int startNumber, int stake)
    {
        return new RouletteBet(BetType.Street, stake, RouletteTableLayout.GetStreetSlotIds(startNumber));
    }

    public static RouletteBet CreateCorner(int bottomLeftNumber, int stake)
    {
        return new RouletteBet(BetType.Corner, stake, RouletteTableLayout.GetCornerSlotIds(bottomLeftNumber));
    }

    public static RouletteBet CreateSixLine(int bottomStartNumber, int stake)
    {
        return new RouletteBet(BetType.SixLine, stake, RouletteTableLayout.GetSixLineSlotIds(bottomStartNumber));
    }

    public static RouletteBet CreateRed(int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.Red, stake, RouletteWheelData.GetRedSlots(wheelType).Select(slot => slot.Id));
    }

    public static RouletteBet CreateBlack(int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.Black, stake, RouletteWheelData.GetBlackSlots(wheelType).Select(slot => slot.Id));
    }

    public static RouletteBet CreateEven(int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.Even, stake, RouletteWheelData.GetEvenSlots(wheelType).Select(slot => slot.Id));
    }

    public static RouletteBet CreateOdd(int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.Odd, stake, RouletteWheelData.GetOddSlots(wheelType).Select(slot => slot.Id));
    }

    public static RouletteBet CreateLow(int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.Low, stake, RouletteWheelData.GetLowSlots(wheelType).Select(slot => slot.Id));
    }

    public static RouletteBet CreateHigh(int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.High, stake, RouletteWheelData.GetHighSlots(wheelType).Select(slot => slot.Id));
    }

    public static RouletteBet CreateDozen(int dozenIndex, int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.Dozen, stake, RouletteWheelData.GetDozenSlots(dozenIndex, wheelType).Select(slot => slot.Id));
    }

    public static RouletteBet CreateColumn(int columnIndex, int stake, RouletteWheelType wheelType)
    {
        return new RouletteBet(BetType.Column, stake, RouletteWheelData.GetColumnSlots(columnIndex, wheelType).Select(slot => slot.Id));
    }
}