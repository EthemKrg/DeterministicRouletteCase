using System;
using System.Collections.Generic;
using UnityEngine;

public class RouletteBetArea : MonoBehaviour
{
    [SerializeField] private BetType betType;
    [SerializeField] private string straightSlotId;
    [SerializeField] private int primaryNumber;
    [SerializeField] private int secondaryNumber;
    [SerializeField] private int index;
    [SerializeField] private bool americanOnly;

    public BetType BetType => betType;
    public bool AmericanOnly => americanOnly;

    public RouletteBet CreateBet(int stake, RouletteWheelType wheelType)
    {
        if (!IsAvailableForWheelType(wheelType))
            throw new InvalidOperationException($"{betType} is only available in American roulette.");

        switch (betType)
        {
            case BetType.Straight:
                return RouletteBetFactory.CreateStraight(GetStraightSlotId(), stake, wheelType);

            case BetType.Split:
                return RouletteBetFactory.CreateSplit(primaryNumber, secondaryNumber, stake);

            case BetType.Street:
                return RouletteBetFactory.CreateStreet(primaryNumber, stake);

            case BetType.Corner:
                return RouletteBetFactory.CreateCorner(primaryNumber, stake);

            case BetType.SixLine:
                return RouletteBetFactory.CreateSixLine(primaryNumber, stake);

            case BetType.Red:
                return RouletteBetFactory.CreateRed(stake, wheelType);

            case BetType.Black:
                return RouletteBetFactory.CreateBlack(stake, wheelType);

            case BetType.Even:
                return RouletteBetFactory.CreateEven(stake, wheelType);

            case BetType.Odd:
                return RouletteBetFactory.CreateOdd(stake, wheelType);

            case BetType.Low:
                return RouletteBetFactory.CreateLow(stake, wheelType);

            case BetType.High:
                return RouletteBetFactory.CreateHigh(stake, wheelType);

            case BetType.Dozen:
                return RouletteBetFactory.CreateDozen(index, stake, wheelType);

            case BetType.Column:
                return RouletteBetFactory.CreateColumn(index, stake, wheelType);

            case BetType.FiveNumber:
                return RouletteBetFactory.CreateFiveNumber(stake, wheelType);

            default:
                throw new ArgumentOutOfRangeException(nameof(betType), betType, "Unsupported bet area type.");
        }
    }

    public bool IsAvailableForWheelType(RouletteWheelType wheelType)
    {
        return !americanOnly || wheelType == RouletteWheelType.American;
    }

    public IReadOnlyList<string> GetPreviewSlotIds(RouletteWheelType wheelType)
    {
        if (!IsAvailableForWheelType(wheelType))
            return Array.Empty<string>();

        RouletteBet previewBet = CreateBet(10, wheelType);
        return previewBet.CoveredSlotIds;
    }

    private string GetStraightSlotId()
    {
        if (!string.IsNullOrWhiteSpace(straightSlotId))
            return straightSlotId.Trim();

        return primaryNumber.ToString();
    }
}