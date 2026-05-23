using System;
using System.Collections.Generic;

public static class RouletteTableLayout
{
    private const int MinNumber = 1;
    private const int MaxNumber = 36;

    public const int RowCount = 3;
    public const int ColumnCount = 12;

    public static bool IsTableNumber(int number)
    {
        return number >= MinNumber && number <= MaxNumber;
    }

    public static RouletteTablePosition GetPosition(int number)
    {
        if (!IsTableNumber(number))
            throw new ArgumentOutOfRangeException(nameof(number), number, "Number must be between 1 and 36.");

        int row = (number - 1) % RowCount;
        int column = (number - 1) / RowCount;

        return new RouletteTablePosition(row, column);
    }

    public static bool TryGetPosition(int number, out RouletteTablePosition position)
    {
        if (!IsTableNumber(number))
        {
            position = default;
            return false;
        }

        position = GetPosition(number);
        return true;
    }

    public static int GetNumberAt(int row, int column)
    {
        if (row < 0 || row >= RowCount)
            throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be between 0 and 2.");

        if (column < 0 || column >= ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be between 0 and 11.");

        return column * RowCount + row + 1;
    }

    public static bool TryGetNumberAt(int row, int column, out int number)
    {
        number = 0;

        if (row < 0 || row >= RowCount)
            return false;

        if (column < 0 || column >= ColumnCount)
            return false;

        number = column * RowCount + row + 1;
        return true;
    }

    public static bool AreAdjacentForSplit(int firstNumber, int secondNumber)
    {
        if (!IsTableNumber(firstNumber) || !IsTableNumber(secondNumber))
            return false;

        RouletteTablePosition first = GetPosition(firstNumber);
        RouletteTablePosition second = GetPosition(secondNumber);

        int rowDifference = Math.Abs(first.Row - second.Row);
        int columnDifference = Math.Abs(first.Column - second.Column);

        return rowDifference + columnDifference == 1;
    }

    public static IReadOnlyList<string> GetSplitSlotIds(int firstNumber, int secondNumber)
    {
        if (!TryGetSplitSlotIds(firstNumber, secondNumber, out IReadOnlyList<string> slotIds))
            throw new ArgumentException($"Split bet requires adjacent numbers. Given: {firstNumber}, {secondNumber}");

        return slotIds;
    }

    public static bool TryGetSplitSlotIds(int firstNumber, int secondNumber, out IReadOnlyList<string> slotIds)
    {
        slotIds = null;

        if (!AreAdjacentForSplit(firstNumber, secondNumber))
            return false;

        slotIds = new[]
        {
        firstNumber.ToString(),
        secondNumber.ToString()
    };

        return true;
    }

    public static IReadOnlyList<string> GetStreetSlotIds(int startNumber)
    {
        if (!TryGetStreetSlotIds(startNumber, out IReadOnlyList<string> slotIds))
            throw new ArgumentException($"Invalid street start number: {startNumber}", nameof(startNumber));

        return slotIds;
    }

    public static bool TryGetStreetSlotIds(int startNumber, out IReadOnlyList<string> slotIds)
    {
        slotIds = null;

        if (!TryGetPosition(startNumber, out RouletteTablePosition position))
            return false;

        if (position.Row != 0)
            return false;

        if (!TryGetNumberAt(0, position.Column, out int first))
            return false;

        if (!TryGetNumberAt(1, position.Column, out int second))
            return false;

        if (!TryGetNumberAt(2, position.Column, out int third))
            return false;

        slotIds = new[]
        {
        first.ToString(),
        second.ToString(),
        third.ToString()
    };

        return true;
    }

    public static IReadOnlyList<string> GetCornerSlotIds(int bottomLeftNumber)
    {
        if (!TryGetCornerSlotIds(bottomLeftNumber, out IReadOnlyList<string> slotIds))
            throw new ArgumentException($"Invalid corner bottom-left number: {bottomLeftNumber}", nameof(bottomLeftNumber));

        return slotIds;
    }

    public static bool TryGetCornerSlotIds(int bottomLeftNumber, out IReadOnlyList<string> slotIds)
    {
        slotIds = null;

        if (!TryGetPosition(bottomLeftNumber, out RouletteTablePosition position))
            return false;

        if (position.Row >= RowCount - 1)
            return false;

        if (position.Column >= ColumnCount - 1)
            return false;

        if (!TryGetNumberAt(position.Row, position.Column, out int bottomLeft))
            return false;

        if (!TryGetNumberAt(position.Row + 1, position.Column, out int topLeft))
            return false;

        if (!TryGetNumberAt(position.Row, position.Column + 1, out int bottomRight))
            return false;

        if (!TryGetNumberAt(position.Row + 1, position.Column + 1, out int topRight))
            return false;

        slotIds = new[]
        {
        bottomLeft.ToString(),
        topLeft.ToString(),
        bottomRight.ToString(),
        topRight.ToString()
    };

        return true;
    }

    public static IReadOnlyList<string> GetSixLineSlotIds(int bottomStartNumber)
    {
        if (!TryGetSixLineSlotIds(bottomStartNumber, out IReadOnlyList<string> slotIds))
            throw new ArgumentException($"Invalid six line bottom-start number: {bottomStartNumber}", nameof(bottomStartNumber));

        return slotIds;
    }

    public static bool TryGetSixLineSlotIds(int bottomStartNumber, out IReadOnlyList<string> slotIds)
    {
        slotIds = null;

        if (!TryGetPosition(bottomStartNumber, out RouletteTablePosition position))
            return false;

        if (position.Row != 0)
            return false;

        if (position.Column >= ColumnCount - 1)
            return false;

        if (!TryGetNumberAt(0, position.Column, out int first))
            return false;

        if (!TryGetNumberAt(1, position.Column, out int second))
            return false;

        if (!TryGetNumberAt(2, position.Column, out int third))
            return false;

        if (!TryGetNumberAt(0, position.Column + 1, out int fourth))
            return false;

        if (!TryGetNumberAt(1, position.Column + 1, out int fifth))
            return false;

        if (!TryGetNumberAt(2, position.Column + 1, out int sixth))
            return false;

        slotIds = new[]
        {
        first.ToString(),
        second.ToString(),
        third.ToString(),
        fourth.ToString(),
        fifth.ToString(),
        sixth.ToString()
    };

        return true;
    }
}