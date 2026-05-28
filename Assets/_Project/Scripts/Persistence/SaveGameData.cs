using System;

[Serializable]
public class SaveGameData
{
    public int version = 1;
    public GameStateSaveData gameState;
    public StatisticsSaveData overallStats;
    public StatisticsSaveData europeanStats;
    public StatisticsSaveData americanStats;
    public LastRoundSaveData lastRound;
    public int selectedChipValue;
}

[Serializable]
public class GameStateSaveData
{
    public int currentChips;
    public string wheelType;
    public BetSaveData[] activeBets;
}

[Serializable]
public class BetSaveData
{
    public string betType;
    public int stake;
    public string[] coveredSlotIds;
}

[Serializable]
public class StatisticsSaveData
{
    public int totalSpins;
    public int totalWins;
    public int totalLosses;
    public int totalProfitLoss;
    public int totalWagered;
    public int bestRoundNetProfit;
    public string lastWinningSlotId;
    public int lastRoundNetProfit;
}

[Serializable]
public class LastRoundSaveData
{
    public string winningSlotId;
    public string wheelType;
    public int totalStake;
    public int totalReturn;
    public int netProfit;
    public int winningBetCount;
    public int losingBetCount;
}
