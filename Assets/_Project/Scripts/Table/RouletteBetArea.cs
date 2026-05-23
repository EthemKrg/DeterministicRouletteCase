using System;
using System.Collections.Generic;
using UnityEngine;

public class RouletteBetArea : MonoBehaviour
{
    [SerializeField] private BetType betType;
    [SerializeField] private int primaryNumber;
    [SerializeField] private int secondaryNumber;
    [SerializeField] private int index;

    public BetType BetType => betType;

    public RouletteBet CreateBet(int stake, RouletteWheelType wheelType)
    {
        switch (betType)
        {
            case BetType.Straight:
                return RouletteBetFactory.CreateStraight(primaryNumber.ToString(), stake, wheelType);

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

            default:
                throw new ArgumentOutOfRangeException(nameof(betType), betType, "Unsupported bet area type.");
        }
    }

    public IReadOnlyList<string> GetPreviewSlotIds(RouletteWheelType wheelType)
    {
        RouletteBet previewBet = CreateBet(10, wheelType);
        return previewBet.CoveredSlotIds;
    }
}