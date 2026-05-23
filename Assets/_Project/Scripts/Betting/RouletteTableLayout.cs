using System;
using System.Collections.Generic;

public static class RouletteTableLayout
{
    private const int MinNumber = 1;
    private const int MaxNumber = 36;
    private const int RowCount = 3;
    private const int ColumnCount = 12;

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

    public static int GetNumberAt(int row, int column)
    {
        if (row < 0 || row >= RowCount)
            throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be between 0 and 2.");

        if (column < 0 || column >= ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be between 0 and 11.");

        return column * RowCount + row + 1;
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

    public static IReadOnlyList<string> GetStreetSlotIds(int startNumber)
    {
        if (!IsTableNumber(startNumber))
            throw new ArgumentOutOfRangeException(nameof(startNumber), startNumber, "Street start number must be between 1 and 36.");

        RouletteTablePosition position = GetPosition(startNumber);

        if (position.Row != 0)
            throw new ArgumentException("Street must start from the bottom row number of a column.", nameof(startNumber));

        return new[]
        {
            GetNumberAt(0, position.Column).ToString(),
            GetNumberAt(1, position.Column).ToString(),
            GetNumberAt(2, position.Column).ToString()
        };
    }

    public static IReadOnlyList<string> GetCornerSlotIds(int bottomLeftNumber)
    {
        if (!IsTableNumber(bottomLeftNumber))
            throw new ArgumentOutOfRangeException(nameof(bottomLeftNumber), bottomLeftNumber, "Corner start number must be between 1 and 36.");

        RouletteTablePosition position = GetPosition(bottomLeftNumber);

        if (position.Row >= RowCount - 1)
            throw new ArgumentException("Corner cannot start from the top row.", nameof(bottomLeftNumber));

        if (position.Column >= ColumnCount - 1)
            throw new ArgumentException("Corner cannot start from the last column.", nameof(bottomLeftNumber));

        return new[]
        {
            GetNumberAt(position.Row, position.Column).ToString(),
            GetNumberAt(position.Row + 1, position.Column).ToString(),
            GetNumberAt(position.Row, position.Column + 1).ToString(),
            GetNumberAt(position.Row + 1, position.Column + 1).ToString()
        };
    }

    public static IReadOnlyList<string> GetSixLineSlotIds(int bottomStartNumber)
    {
        if (!IsTableNumber(bottomStartNumber))
            throw new ArgumentOutOfRangeException(nameof(bottomStartNumber), bottomStartNumber, "Six line start number must be between 1 and 36.");

        RouletteTablePosition position = GetPosition(bottomStartNumber);

        if (position.Row != 0)
            throw new ArgumentException("Six Line must start from the bottom row number of a column.", nameof(bottomStartNumber));

        if (position.Column >= ColumnCount - 1)
            throw new ArgumentException("Six Line cannot start from the last column.", nameof(bottomStartNumber));

        return new[]
        {
            GetNumberAt(0, position.Column).ToString(),
            GetNumberAt(1, position.Column).ToString(),
            GetNumberAt(2, position.Column).ToString(),
            GetNumberAt(0, position.Column + 1).ToString(),
            GetNumberAt(1, position.Column + 1).ToString(),
            GetNumberAt(2, position.Column + 1).ToString()
        };
    }
}