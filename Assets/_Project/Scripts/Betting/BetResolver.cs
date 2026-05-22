using System;
using System.Collections.Generic;

public static class BetResolver
{
    public static RoundResult Resolve(RouletteSlot winningSlot, IEnumerable<RouletteBet> activeBets)
    {
        if (winningSlot == null)
            throw new ArgumentNullException(nameof(winningSlot));

        if (activeBets == null)
            throw new ArgumentNullException(nameof(activeBets));

        List<ResolvedBet> resolvedBets = new List<ResolvedBet>();

        foreach (RouletteBet bet in activeBets)
        {
            if (bet == null)
                throw new ArgumentException("Active bets collection contains a null bet.", nameof(activeBets));

            bool isWin = bet.CoversSlot(winningSlot.Id);
            resolvedBets.Add(new ResolvedBet(bet, isWin));
        }

        return new RoundResult(winningSlot, resolvedBets);
    }
}