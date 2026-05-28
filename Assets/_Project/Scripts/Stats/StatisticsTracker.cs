public class StatisticsTracker
{
    public PlayerStatistics Statistics { get; private set; }
    public PlayerStatistics EuropeanStatistics { get; private set; }
    public PlayerStatistics AmericanStatistics { get; private set; }

    public StatisticsTracker()
    {
        Statistics = new PlayerStatistics();
        EuropeanStatistics = new PlayerStatistics();
        AmericanStatistics = new PlayerStatistics();
    }

    public void TrackRound(RoundResult roundResult)
    {
        Statistics.ApplyRoundResult(roundResult);
    }

    public void TrackRound(RoundResult roundResult, RouletteWheelType wheelType)
    {
        TrackRound(roundResult);
        GetStatistics(wheelType).ApplyRoundResult(roundResult);
    }

    public PlayerStatistics GetStatistics(RouletteWheelType wheelType)
    {
        return wheelType == RouletteWheelType.American
            ? AmericanStatistics
            : EuropeanStatistics;
    }

    public void Reset()
    {
        Statistics.Reset();
        EuropeanStatistics.Reset();
        AmericanStatistics.Reset();
    }

    public StatisticsSaveData ExportOverallSnapshot()
    {
        return Statistics.ExportSnapshot();
    }

    public StatisticsSaveData ExportEuropeanSnapshot()
    {
        return EuropeanStatistics.ExportSnapshot();
    }

    public StatisticsSaveData ExportAmericanSnapshot()
    {
        return AmericanStatistics.ExportSnapshot();
    }

    public void RestoreFromSnapshots(StatisticsSaveData overall, StatisticsSaveData european, StatisticsSaveData american)
    {
        Statistics.RestoreFromSnapshot(overall);
        EuropeanStatistics.RestoreFromSnapshot(european);
        AmericanStatistics.RestoreFromSnapshot(american);
    }
}
