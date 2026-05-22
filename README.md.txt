# Deterministic Roulette Case

A Unity case study project for a deterministic single-player roulette prototype.

The main goal is to build a working roulette flow with deterministic result selection, chip-based betting, payout calculation and player statistics.

## Unity Version

Unity 6000.4.4f1

## How to Run

1. Open the project with Unity 6000.4.4f1 or a compatible Unity 6000 version.
2. Open `Assets/_Project/Scenes/Main.unity`.
3. Enter Play Mode.
4. Use the debug UI to place bets and select the next roulette result.

## Current State

Current implementation includes:

- European and American roulette wheel data
- Support for `0` and `00` slot ids
- Basic bet model
- Payout multipliers
- Deterministic round resolving
- Chip balance updates
- Runtime statistics tracking
- Basic game flow states
- Temporary debug UI
- Bet limits
- Basic input validation

## Debug UI

The current UI is mainly for testing the game flow.

It supports:

- Selecting European or American roulette
- Setting the deterministic result
- Setting a straight bet number
- Entering stake amount
- Placing basic outside bets
- Placing dozen and column bets
- Spinning the wheel result instantly
- Clearing active bets

## Notes

Bet placement decreases chips immediately.

If a bet wins, the player receives the original stake plus profit.  
If a bet loses, the stake is not returned.

For player feedback, a round is treated as a win when at least one placed bet wins.  
Net profit/loss is still tracked separately.

Example:

- Red bet wins: `+10`
- Straight bet loses: `-10`
- Net profit: `0`
- Win feedback: `true`

This is intentional for player feedback, while still keeping financial statistics correct.

## Development Constraints

- No third-party gameplay/code plugins
- No DOTween
- Unity-native UI and gameplay code

## Planned Next Steps

- Split bet support
- Street bet support
- Corner bet support
- Six Line bet support
- American five-number bet support
- Separate statistics for European and American roulette
- 3D wheel and ball animation
- Save/load support
- UI polish
- Audio, VFX and haptic feedback

## Known Issues / Missing Features

- Current UI is a debug UI, not the final presentation UI
- Wheel and ball animation are not implemented yet
- Full inside bet support is not finished yet
- Save/load is not implemented yet