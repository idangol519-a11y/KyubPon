# Changelog

All notable changes to KyubPon are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
