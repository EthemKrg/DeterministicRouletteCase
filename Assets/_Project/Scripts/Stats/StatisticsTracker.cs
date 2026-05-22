public class StatisticsTracker
{
    public PlayerStatistics Statistics { get; private set; }

    public StatisticsTracker()
    {
        Statistics = new PlayerStatistics();
    }

    public void TrackRound(RoundResult roundResult)
    {
        Statistics.ApplyRoundResult(roundResult);
    }

    public void Reset()
    {
        Statistics.Reset();
    }
}