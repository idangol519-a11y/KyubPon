# Changelog

All notable changes to KyubPon are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.8.0] - 2026-10-07

### Added
- Sandbox: a rules tooltip. Hold the mouse on a block for one second, in the palette or on the grid, to see its name, HP, and what it does. For blocks on the grid it also says whose block it is and shows current HP out of starting HP.

### Changed
- Sandbox: the grid is about 30% larger. The rules panel on the left and the battle panel on the right are gone, the title and battle numbers are single lines at the top, and the palette shares the bottom row with the Back, Pass, and Reset buttons.
- Sandbox: the battle line now shows the turn number and the blocks destroyed on each side. The "blocks on grid" counts were dropped since they can be seen on the board.

## [0.7.0] - 2026-10-07

### Added
- Block skins drawn by Idan for the four blocks (red, blue, green, yellow), 64 by 64 pixel PNGs in `Assets/StreamingAssets/Content/Blocks/<Name>/<Name>.png`.
- `BlockSkinLoader`: loads block skins from those PNG files while the game runs, so a skin can be changed by replacing its file. A missing file falls back to a plain colored square and logs a warning.

### Changed
- Sandbox blocks, the palette, and the dragged block now show the skins instead of plain colored squares.
- Enemy blocks show the same skin darkened, with the X drawn across the whole block. HP numbers have a dark outline so they stay readable on the skins.

## [0.6.0] - 2026-10-06

### Added
- **Temporary** turn-based battle on the Sandbox page. Each turn you drag one block onto an empty square, then the enemy places one of its own, then every square activates in order starting from a random block.
- Enemy blocks: every block kind has an enemy copy, drawn darker with an X on it. The enemy prefers squares next to your blocks.
- Blocks have HP, shown as a number on each block. A block at 0 HP is destroyed.
- A battle panel that tracks the turn number, blocks destroyed on each side, and blocks on the grid.
- PASS (skip placing) and RESET (clear the battle) buttons.

### Changed
- The Sandbox block rules are now about fighting instead of score: red hits each enemy next to it for 2 HP, blue pushes each enemy next to it one square (off the edge destroys it, a blocked push costs 1 HP), green heals each friend next to it by 2 HP, yellow hits every enemy in its row for 1 HP.
- Placed blocks can no longer be dragged around or removed by hand.

### Removed
- The Sandbox score counter and SCORE button, replaced by the battle.

## [0.5.0] - 2026-10-06

### Added
- **Temporary** scoring test on the Sandbox page. Each cube color has a scoring rule: red adds 10 points, blue adds 5 for each cube next to it, green doubles the score so far, yellow adds 1 for every cube on the grid.
- A rules note on the left of the Sandbox page that lists what each color does.
- A SCORE button. A scoring run starts at a random cube, then visits every square of the grid in order, lighting each one up. The running score and the final score are shown above the grid.

## [0.4.0] - 2026-10-06

### Added
- **Temporary** Sandbox test page, opened from a new SANDBOX button on the Home screen. It has a grid that can be resized with on-screen arrows (minimum 5 by 4, maximum 12 by 8) and four colored cubes that can be dragged into the grid, moved between cells, and removed by dropping them outside it. This page is for testing only and will be removed.

## [0.3.0] - 2026-10-06

### Added
- Title screen (the DropScreen scene) is back in the game. It shows the game name and "PRESS ANY KEY", then opens the Home screen. Works with keyboard, mouse, controller, and touch.

### Changed
- The build now contains two scenes in order: DropScreen (title), then HomeScreen.
- The title screen uses the same pixel font and scaling as the Home screen instead of Unity's basic IMGUI text.
- The repository now has a single long-lived branch, `main`. `develop` and old feature branches were removed.

## [0.2.1] - 2026-10-06

### Added
- `tools/Set-GameVersion.ps1`: sets the version in Player Settings, README, GameState, and CHANGELOG together, and checks that they agree.
- `docs/Releasing.md`: how version numbers, branches, and tags are named, and the release steps.

### Fixed
- README still said version 0.1.0 and pointed to the old DropScreen scene.

## [0.2.0] - 2026-10-06

### Added
- Home screen with New Game, Continue, Settings, and Quit buttons. New Game and Continue are visible but disabled until runs and saved games exist.
- Settings panel with master volume, fullscreen on/off, resolution, and Back. Settings are remembered between sessions.
- Quit closes the game (and stops Play mode inside the Unity Editor).
- Press Start 2P pixel font (SIL Open Font License 1.1), with its license file.
- Unity Input System package (`com.unity.inputsystem` 1.20.0). Menus work with mouse, keyboard, and controller.
- `docs/Credits.md` listing third-party assets.

### Changed
- The game now starts on the HomeScreen scene instead of DropScreen.
- Player Settings > Active Input Handling is now set to the Input System package (it held an invalid value before).

## [0.1.0] - 2026-10-05

### Added
- New Unity 2D project named KyubPon.
- DropScreen scene: a blank landing screen showing the game name and version.
- Version number shown in the bottom-right corner of the screen and written to the log at startup.
