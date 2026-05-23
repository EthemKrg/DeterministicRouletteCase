# Deterministic Roulette Case
A Unity case study for a deterministic single-player roulette prototype.

The main goal is to build a working roulette flow with deterministic result selection, chip-based betting, payout calculation and player statistics.

## Unity Version
Unity 6000.4.4f1

## How to Run
1. Open the project with Unity 6000.4.4f1 or a compatible Unity 6000 version.
2. Open `Assets/_Project/Scenes/Main.unity`.
3. Enter Play Mode.
4. Use the debug UI to place bets and select the next roulette result.

## How to Play
1. Select a chip value.
2. Place bets on the roulette table.
3. Select the next winning number from the UI.
4. Spin the wheel.
5. The round is resolved and the chip balance/statistics are updated.

If no deterministic result is selected, the game is planned to use a random result.


## Current Implementation
Implemented so far:

* European roulette wheel data
* American roulette wheel data with `00`
* Straight bet support
* Split bet support
* Street bet support
* Corner bet support
* Six Line bet support
* Red and Black bets
* Even and Odd bets
* Low and High bets
* Dozen bets
* Column bets
* Chip balance updates
* Bet limits
* Deterministic round resolving
* Runtime player statistics
* European/American roulette type switch
* Collider-based 3D table bet area test
* New Input System based table raycast input
* Temporary debug UI for testing


## Current 3D Table Test
The project currently has test bet areas for:
* Straight 17
* Split 17/20
* Street 16
* Corner 16
* Six Line 16

These test areas are used to verify the table interaction flow before building the full roulette table layout.

The intended final flow is:

`3D table collider -> RouletteBetArea -> RouletteTableInputController -> GameFlowController`


## Debug UI
The debug UI is used for development and testing.

It currently supports:
* Selecting the roulette type
* Setting the deterministic winning result
* Placing some test bets
* Spinning the round instantly
* Viewing chip balance and statistics
* Clearing active bets

The final betting interaction is intended to happen on the 3D roulette table, not through debug UI buttons.

## Rules and Payouts
Supported payout multipliers:

* Straight: 35:1
* Split: 17:1
* Street: 11:1
* Corner: 8:1
* Six Line: 5:1
* Red/Black: 1:1
* Even/Odd: 1:1
* Low/High: 1:1
* Dozen: 2:1
* Column: 2:1

A placed bet immediately decreases the chip balance.

When a bet wins, the original stake and the profit are returned.

Player win feedback is based on whether at least one placed bet wins. Profit/loss is tracked separately.


## Architecture Notes
Main scripts:

* `GameFlowController`: controls the round flow and connects the game state, bets, resolving, and statistics.
* `RouletteGameState`: stores current chips, active bets, wheel type, limits, and flow state.
* `RouletteBetFactory`: creates valid roulette bets.
* `RouletteTableLayout`: validates inside bet positions on the roulette table.
* `BetResolver`: resolves active bets against the winning slot.
* `StatisticsTracker`: tracks player statistics.
* `RouletteBetArea`: stores the data for one 3D table bet area.
* `RouletteTableInputController`: reads pointer input and places bets through raycast.
* `ChipSelectionController`: stores the selected chip denomination.

Patterns used in the project:

* Factory pattern for bet creation.
* Event-based updates for game state and round results.
* Separate state object for the current roulette session.
* Small layout helper for table-specific inside bet validation.


## Development Constraints
* No third-party gameplay or code plugins.
* No DOTween.
* No reused scripts from other projects.
* Unity UI is allowed.
* 3D, audio, and texture assets may be sourced if their licenses allow it.

## Planned Work Before Final Delivery
Remaining planned work:

* Full 3D table betting layout
* Chip visual placement and stacking
* Hover and winning bet highlights
* 3D wheel spin animation
* Ball drop / landing animation
* Random result fallback when no deterministic result is selected
* American five-number bet
* Save/load support
* Auto-save on quit
* Resume previous state
* Polished in-game UI
* Sound effects
* Basic VFX for wins
* Demo video
* Final README update

## Known Issues
Current known limitations:

* The full 3D table layout is not finished yet.
* Wheel and ball animations are not implemented yet.
* Save/load is not implemented yet.
* Audio and VFX are not implemented yet.
* Current UI is still partly debug-focused.
* Demo video link will be added before final submission.

## Demo Video
Demo video link will be added before submission.


## Repository
This project is developed with regular Git commits and feature branches.



