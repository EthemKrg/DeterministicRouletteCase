using System.Collections.Generic;
using System.Linq;

public static class RouletteWheelData
{
    // Used hash set for determining color because it's more efficient than a list for lookups.
    private static readonly HashSet<int> RedNumbers = new HashSet<int>
    {
        1, 3, 5, 7, 9, 12,
        14, 16, 18, 19, 21, 23,
        25, 27, 30, 32, 34, 36
    };

    private static readonly int[] EuropeanWheelOrder =
    {
        0, 32, 15, 19, 4, 21, 2, 25, 17,
        34, 6, 27, 13, 36, 11, 30, 8, 23,
        10, 5, 24, 16, 33, 1, 20, 14, 31,
        9, 22, 18, 29, 7, 28, 12, 35, 3, 26
    };

    // -1 is used as an internal marker for American roulette's "00".
    private static readonly int[] AmericanWheelOrder =
    {
        0, 28, 9, 26, 30, 11, 7, 20, 32,
        17, 5, 22, 34, 15, 3, 24, 36, 13,
        1, -1, 27, 10, 25, 29, 12, 8, 19,
        31, 18, 6, 21, 33, 16, 4, 23, 35,
        14, 2
    };

    // Creates list every time, but it's not a big deal since the number of slots is small, can be optimized later by caching
    public static IReadOnlyList<RouletteSlot> GetEuropeanSlots()
    {
        List<RouletteSlot> slots = new List<RouletteSlot>();

        foreach (int number in EuropeanWheelOrder)
        {
            slots.Add(CreateSlot(number));
        }

        return slots;
    }

    // Also this creates a new list everytime, same caching optimization can be applied if needed
    public static IReadOnlyList<RouletteSlot> GetAmericanSlots()
    {
        List<RouletteSlot> slots = new List<RouletteSlot>();

        foreach (int number in AmericanWheelOrder)
        {
            if (number == -1)
            {
                slots.Add(new RouletteSlot("00", -1, RouletteColor.Green, true));
                continue;
            }

            slots.Add(CreateSlot(number));
        }

        return slots;
    }

    public static RouletteSlot GetSlotById(string id, RouletteWheelType wheelType)
    {
        return GetSlots(wheelType).FirstOrDefault(slot => slot.Id == id);
    }

    public static RouletteSlot GetEuropeanSlotById(string id)
    {
        return GetSlotById(id, RouletteWheelType.European);
    }

    public static RouletteSlot GetAmericanSlotById(string id)
    {
        return GetSlotById(id, RouletteWheelType.American);
    }

    public static IReadOnlyList<RouletteSlot> GetRedSlots(RouletteWheelType wheelType = RouletteWheelType.European)
    {
        return GetSlots(wheelType)
            .Where(slot => slot.Color == RouletteColor.Red)
            .ToList();
    }

    public static IReadOnlyList<RouletteSlot> GetBlackSlots(RouletteWheelType wheelType = RouletteWheelType.European)
    {
        return GetSlots(wheelType)
            .Where(slot => slot.Color == RouletteColor.Black)
            .ToList();
    }

    public static IReadOnlyList<RouletteSlot> GetEvenSlots(RouletteWheelType wheelType = RouletteWheelType.European) // 2, 4, 6, ..., 36
    {
        return GetSlots(wheelType)
            .Where(slot => slot.IsEven())
            .ToList();
    }

    public static IReadOnlyList<RouletteSlot> GetOddSlots(RouletteWheelType wheelType = RouletteWheelType.European) // 1, 3, 5, ..., 35
    {
        return GetSlots(wheelType)
            .Where(slot => slot.IsOdd())
            .ToList();
    }

    public static IReadOnlyList<RouletteSlot> GetLowSlots(RouletteWheelType wheelType = RouletteWheelType.European) // 1-18
    {
        return GetSlots(wheelType)
            .Where(slot => slot.IsLow())
            .ToList();
    }

    public static IReadOnlyList<RouletteSlot> GetHighSlots(RouletteWheelType wheelType = RouletteWheelType.European) // 19-36
    {
        return GetSlots(wheelType)
            .Where(slot => slot.IsHigh())
            .ToList();
    }

    // dozenIndex: 1 for 1-12, 2 for 13-24, 3 for 25-36
    public static IReadOnlyList<RouletteSlot> GetDozenSlots(int dozenIndex, RouletteWheelType wheelType = RouletteWheelType.European)
    {
        return GetSlots(wheelType)
            .Where(slot => slot.GetDozenIndex() == dozenIndex)
            .ToList();
    }

    // columnIndex: 1 for 1,4,7,...,34; 2 for 2,5,8,...,35; 3 for 3,6,9,...,36
    public static IReadOnlyList<RouletteSlot> GetColumnSlots(int dozenIndex, RouletteWheelType wheelType = RouletteWheelType.European)
    {
        return GetSlots(wheelType)
            .Where(slot => slot.GetColumnIndex() == dozenIndex)
            .ToList();
    }

    public static IReadOnlyList<RouletteSlot> GetSlots(RouletteWheelType wheelType)
    {
        return wheelType == RouletteWheelType.American
            ? GetAmericanSlots()
            : GetEuropeanSlots();
    }

    private static RouletteSlot CreateSlot(int number)
    {
        if (number == 0)
            return new RouletteSlot("0", 0, RouletteColor.Green);

        RouletteColor color = RedNumbers.Contains(number)
            ? RouletteColor.Red
            : RouletteColor.Black;

        return new RouletteSlot(number.ToString(), number, color);
    }
}