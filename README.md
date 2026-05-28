# Deterministic Roulette Case Study

A Unity 6 single-player roulette prototype built for a game developer case study.

The main idea is to keep the normal roulette flow, but also make the result controllable for testing. The player can select the next winning slot from the UI before spinning. If no result is selected, the game picks a random valid slot for the active wheel type.

The project includes European and American roulette modes, 3D table betting, chip balance, payouts, player statistics, wheel and ball animation, audio feedback, save/load, and a few polish features around the winning result.

## Unity Version

Unity 6000.4.4f1

## How to Run

1. Open the project with Unity 6000.4.4f1 or a compatible Unity 6 version.
2. Open `Assets/_Project/Scenes/Main.unity`.
3. Enter Play Mode.
4. Use the table and UI controls to place bets, select a result if needed, and spin the wheel.

## How to Play

1. Select a chip denomination from the chip bar at the bottom of the table.
2. Click or tap a betting area on the 3D roulette table.
3. Optionally select the next winning number before spinning.
4. Press `SPIN` to start the round.
5. The wheel and ball animation plays, the result is resolved, and chips/statistics are updated.

If no deterministic result is selected, the game uses a random result for the selected roulette type.

Placed chip stacks can be tapped before spinning to undo the top chip from that bet. Active bets can also be cleared from the UI before the round starts.

## Table UI

The main gameplay UI is kept around the roulette table so the player can test the full flow without leaving the scene.

- The chip bar is used to select the active chip value.
- `SPIN` starts the current round.
- `CLEAR` removes all active bets before spinning.
- `TOGGLE` switches between European and American roulette modes.
- `WINNER` is used to select or change the deterministic winning result.
- The balance display shows the current chip amount.
- Feedback text is shown above the table for short gameplay messages.

## Implemented Features

- Deterministic result selection
- Random result fallback
- European roulette support
- American roulette support with `00`
- Runtime roulette type switching
- Chip denomination selection
- Chip balance and bet limits
- 3D roulette table interaction
- Table-centered UI for chips, balance, spin controls, and deterministic result selection
- Collider/raycast based table betting
- Hover/result highlight feedback
- Animated chip stacks
- Undo from placed chip stacks
- Full payout calculation
- Overall and per-wheel player statistics
- Wheel and ball animation
- Camera transition between betting and spin views
- Audio feedback for chips, spin, ball drop, win/lose, and background music
- Winning number display
- Confetti effect on winning rounds
- Save/load with local JSON data
- Auto-save on gameplay state changes

## Roulette Rules and Payouts

Supported bet types:

| Bet Type | Payout |
|---|---:|
| Straight | 35:1 |
| Split | 17:1 |
| Street | 11:1 |
| Corner | 8:1 |
| Six Line | 5:1 |
| Red / Black | 1:1 |
| Even / Odd | 1:1 |
| Low / High | 1:1 |
| Dozen | 2:1 |
| Column | 2:1 |

When a bet is placed, the stake is removed from the chip balance immediately. If the bet wins, the original stake and profit are returned. If the bet loses, the stake is kept by the table.

## Wheel and Ball Animation

The wheel and ball animation is handled by `RouletteWheelSpinAnimator`.

The spin is split into a few readable phases instead of being one long movement:

1. Wheel warmup and acceleration
2. Ball release with opposite-direction orbit
3. Ball coast toward the target pocket
4. Ball drop into the pocket area
5. Small settle movement at the end

The animation uses pocket transforms for the final result position, so deterministic results can land on the selected slot reliably. The camera also transitions from the top-down betting view to a perspective spin view during the round.

## Save and Load

The project includes a small persistence layer.

Main files:

- `SaveGameManager.cs`
- `SaveGameRepository.cs`
- `SaveGameData.cs`

The save system stores:

- Current chip balance
- Active bets
- Selected chip denomination
- Selected roulette type
- Last round result
- Overall statistics
- European roulette statistics
- American roulette statistics

Data is saved as JSON using Unity's `JsonUtility`. File writing is handled through a temp-file-then-replace flow, so the save file is less likely to be left in a broken state if something interrupts the write.

## Statistics

Statistics are tracked through `StatisticsTracker` and `PlayerStatistics`.

The game tracks:

- Spins played
- Wins
- Losses
- Profit/loss
- Total wagered
- Best win
- Last round data

Statistics are kept both overall and separately for European and American roulette modes.

## Code Structure

The main gameplay flow is:

`Table Input -> Bet Area -> Game Flow -> Spin Animation -> Bet Resolver -> UI / Stats / Save`

Important scripts:

- `GameFlowController.cs`  
  Controls the main round flow, bet placement, spin routine, result resolving, events, and save/load API.

- `RouletteGameState.cs`  
  Stores current session data such as chips, active bets, roulette type, and flow state.

- `RouletteBetFactory.cs`  
  Creates valid roulette bets from table input.

- `BetResolver.cs`  
  Resolves all active bets against the winning slot.

- `PayoutCalculator.cs`  
  Holds payout multipliers for each bet type.

- `RouletteTableInputController.cs`  
  Reads pointer input and sends selected table areas to the game flow.

- `RouletteBetArea.cs`  
  Stores bet area data and preview/covered slot information.

- `RouletteTableHighlightController.cs`  
  Handles hover and result highlights on the table.

- `ChipStackViewController.cs`  
  Creates, updates, clears, restores, and animates chip stacks.

- `ChipVisualPool.cs`  
  Reuses chip visuals instead of creating and destroying them repeatedly.

- `RouletteWheelSpinAnimator.cs`  
  Handles the wheel and ball spin sequence.

- `CameraAnimationController.cs`  
  Switches between betting and spin camera views.

- `RouletteSoundManager.cs`  
  Handles chip, spin, ball drop, win/lose, and BGM audio.

- `WinningNumberDisplayController.cs`  
  Shows the winning result and triggers the win effect.

- `SaveGameManager.cs`  
  Coordinates save/load and restore flow.

- `SaveGameRepository.cs`  
  Handles JSON file I/O.

## Design Patterns

I kept the architecture simple and only used patterns where they made the code easier to follow.

### Factory

`RouletteBetFactory` handles bet creation. This keeps table input separate from the detailed rules of each bet type.

### Observer / Event-Driven Updates

`GameFlowController` exposes events such as game state changes, round resolved, bet placed, bets cleared, and feedback requested. UI, audio, chip visuals, statistics, and save/load systems react to these events instead of being tightly connected to each other.

### State

`RouletteGameState` stores the active game session, while `GameFlowState` keeps the current flow clear: betting, spinning, and resolving.

### Object Pool

`ChipVisualPool` is used for chip visuals. Chip objects are reused instead of being destroyed and recreated every time a bet changes.

### Repository

`SaveGameRepository` separates file writing/reading from gameplay logic. The game flow does not need to know the details of JSON file handling.

### Snapshot

Game state and statistics can be exported and restored through save data objects. This made save/load simpler because the runtime state can be rebuilt from a clear snapshot.

## Optimization Notes

A few small optimizations were done during development:

- Table number labels use TMP mesh baking, so they do not need to stay as many live text objects at runtime.
- Some scene objects use one-faced meshes where the back side is never visible.
- Materials were tested with separate instances while tuning the look, then unnecessary unique material instances were reduced where possible.
- Chip visuals are pooled instead of constantly instantiated and destroyed.
- Chip stack visuals update when bet data changes instead of rebuilding every frame.
- UI refreshes are mostly event-driven through `GameFlowController` events.
- Table bet areas keep their own bet metadata, which keeps the input controller lightweight.
- Procedural table line meshes are used for some table visuals instead of maintaining every line by hand.
- The project avoids third-party runtime/code plugins.
- DOTween was not used. Animation is handled with Unity coroutines, curves, transforms, audio sources, and particle systems.

## Assets and Constraints

The gameplay code was written for this case study.

All 2D and 3D visual assets used in the project were generated by me with AI tools, then selected and adjusted to fit the same casino table style. This includes the visual direction for the table, chips, UI-related visuals, and scene presentation assets.

Sound effects and music were taken from my licensed Ovani audio asset package.

No third-party gameplay/code plugins were used. Unity built-in systems were preferred for animation, UI, audio playback, saving, and effects. DOTween was not used.

## Known Limitations and Future Improvements

The current version covers the main gameplay, deterministic result flow, statistics, save/load, audio, and visual feedback. A few areas could still be improved with more time:

- The wheel and ball animation could be made more natural with a physics-based ball system.
- Ball-pocket interaction could be improved with more realistic collision and bounce behavior.
- Camera timing during the spin could be tuned further.
- More sound variations could be added for repeated chip placement and round results.
- The table materials and surrounding environment could use another visual polish pass.
