public readonly struct RouletteTablePosition
{
    public int Row { get; }
    public int Column { get; }

    public RouletteTablePosition(int row, int column)
    {
        Row = row;
        Column = column;
    }
}