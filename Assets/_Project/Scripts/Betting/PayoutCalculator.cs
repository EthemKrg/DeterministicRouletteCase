using System;
public static class PayoutCalculator
{
    /*
    Roulette payout multipliers based on bet type:
    Straight  -> 35
    Split     -> 17
    Street    -> 11
    Corner    -> 8
    SixLine   -> 5
    Red       -> 1
    Black     -> 1
    Even      -> 1
    Odd       -> 1
    Low       -> 1
    High      -> 1
    Dozen     -> 2
    Column    -> 2

    FiveNumber -> 6
    */

    public static int GetMultiplier(BetType betType)
    {
        switch (betType)
        {
            case BetType.Straight:
                return 35;

            case BetType.Split:
                return 17;

            case BetType.Street:
                return 11;

            case BetType.Corner:
                return 8;

            case BetType.SixLine:
                return 5;

            case BetType.Red:
            case BetType.Black:
            case BetType.Even:
            case BetType.Odd:
            case BetType.Low:
            case BetType.High:
                return 1;

            case BetType.Dozen:
            case BetType.Column:
                return 2;

            case BetType.FiveNumber:
                return 6;

            default:
                throw new ArgumentOutOfRangeException(nameof(betType), betType, "Unsupported bet type.");
        }
    }
}