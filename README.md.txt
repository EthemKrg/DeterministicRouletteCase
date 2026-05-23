# Deterministic Roulette Case

A Unity case study for a deterministic single-player roulette prototype.

The goal is to make a small but complete roulette flow with manual result selection, table betting, chip balance, payouts and basic player stats.

## Unity Version

Unity 6000.4.4f1

## How to Run

1. Open the project with Unity 6000.4.4f1 or a compatible Unity 6000 version.
2. Open `Assets/_Project/Scenes/Main.unity`.
3. Enter Play Mode.
4. Use the table and the debug panel to test the game.

## How to Play

1. Select a chip value.
2. Place bets on the 3D roulette table.
3. Select the next winning number from the UI.
4. Spin the round.
5. The result is resolved and chips/statistics are updated.

If no result is selected, random result selection is planned but not added yet.

## Current State

Implemented so far:

- European roulette data
- American roulette data with `00`
- Roulette type switch
- Chip balance
- Bet limits
- Chip denomination selection
- Deterministic result selection
- Runtime win/loss and profit/loss stats
- Full bet logic for:
  - Straight
  - Split
  - Street
  - Corner
  - Six Line
  - Red / Black
  - Even / Odd
  - Low / High
  - Dozens
  - Columns
- Full 3D table betting layout
- Collider based table input
- New Input System based raycast input
- Hover preview for covered numbers
- 3D marker for the last winning number
- Basic feedback text
- Temporary debug UI for testing

## Table Interaction

Betting is done on the 3D table.

The current flow is:

`3D bet area -> RouletteBetArea -> RouletteTableInputController -> GameFlowController`

The table has generated collider areas for all required bet types. Some line and corner bet areas are still visible for testing. These will need a visual polish pass later so the table looks less like a debug layout.

## Debug UI

The debug UI is still used for testing.

It supports:

- Selecting European or American roulette
- Setting the next winning number
- Spinning the round
- Clearing active bets
- Viewing chips, active bets, statistics and last round result

Final betting should happen on the 3D table, not through debug bet buttons.

## Rules and Payouts

Current payout multipliers:

- Straight: 35:1
- Split: 17:1
- Street: 11:1
- Corner: 8:1
- Six Line: 5:1
- Red / Black: 1:1
- Even / Odd: 1:1
- Low / High: 1:1
- Dozen: 2:1
- Column: 2:1

Placing a bet decreases chips immediately.

If a bet wins, the original stake and profit are returned. If a bet loses, the stake is not returned.

Win feedback is based on whether at least one bet wins. Profit/loss is tracked separately.

## Code Notes

Main scripts:

- `GameFlowController`: controls round flow and sends state/result events.
- `RouletteGameState`: stores chips, active bets, limits, wheel type and flow state.
- `RouletteBetFactory`: creates valid roulette bets.
- `RouletteTableLayout`: handles table positions for inside bets.
- `BetResolver`: resolves active bets against the winning slot.
- `StatisticsTracker`: tracks player stats.
- `RouletteBetArea`: stores data for one table bet area.
- `RouletteTableInputController`: reads pointer input and places table bets.
- `RouletteTableHighlightController`: handles hover preview and winning marker.
- `ChipSelectionController`: stores the selected chip value.

Patterns used:

- Factory for bet creation.
- Events for state and round result updates.
- Separate state object for the current roulette session.
- Small helper class for roulette table layout rules.

## Development Constraints

- No third-party gameplay/code plugins.
- No DOTween.
- No reused scripts from other projects.
- Unity UI is allowed.
- External art/audio assets can be used if the license allows it.

## Planned Work

Next work:

- Add chip visuals on placed bets
- Add round history
- Add 3D wheel spin animation
- Add ball drop / landing animation
- Add random result fallback
- Add American five-number bet
- Add save/load
- Add auto-save and resume
- Polish the table visuals
- Polish the final UI
- Add sound effects
- Add simple win VFX
- Record demo video

## Known Issues

- Wheel and ball animations are not implemented yet.
- Random result fallback is not implemented yet.
- Save/load is not implemented yet.
- Chip stack visuals are not implemented yet.
- Audio and VFX are not implemented yet.
- UI is still partly debug focused.
- Some table bet areas need visual cleanup.
- Demo video link will be added before submission.

## Demo Video

Demo video link will be added before submission.

## Repository

The project is developed with regular commits and feature branches.
