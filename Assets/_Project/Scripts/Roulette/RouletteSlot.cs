using System;

[Serializable]
public class RouletteSlot
{
    public string Id { get; private set; } // should be string to cover for american roulette too, -> requires 00
    public int Number { get; private set; } // for calculation of -> even, odd, low, high, dozen, column
    public RouletteColor Color { get; private set; }
    public bool IsDoubleZero { get; private set; } // 0 and 00 makes lose other outside bets

    public RouletteSlot(string id, int number, RouletteColor color, bool isDoubleZero = false)
    {
        Id = id;
        Number = number;
        Color = color;
        IsDoubleZero = isDoubleZero;
    }

    public bool IsZero()
    {
        return Number == 0 || IsDoubleZero;
    }

    public bool IsEven()
    {
        return !IsZero() && Number % 2 == 0;
    }

    public bool IsOdd()
    {
        return !IsZero() && Number % 2 != 0;
    }

    public bool IsLow()
    {
        return Number >= 1 && Number <= 18;
    }

    public bool IsHigh()
    {
        return Number >= 19 && Number <= 36;
    }

    public int GetDozenIndex()
    {
        if (Number >= 1 && Number <= 12)
            return 1;

        if (Number >= 13 && Number <= 24)
            return 2;

        if (Number >= 25 && Number <= 36)
            return 3;

        return 0;
    }

    public int GetColumnIndex()
    {
        if (Number < 1 || Number > 36)
            return 0;

        int remainder = Number % 3;

        if (remainder == 1)
            return 1;

        if (remainder == 2)
            return 2;

        return 3;
    }
}