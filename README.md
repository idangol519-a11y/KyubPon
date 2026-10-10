# KyubPon

A 2D strategy roguelite block game made in Unity.

You place cube-like creatures ("blocks") on a board and fight an enemy bot that
places its own. Blocks act through their stats and abilities, and abilities can
trigger each other in chain reactions. Between levels you visit a shop or an
event, and the run continues through harder and harder levels until you lose.

**Status:** early development, version `0.17.0`. The project currently contains
the Unity setup, a title screen, and the Home screen with a settings panel. See
[docs/GameState.md](docs/GameState.md) for what is built and what is in progress.

## Requirements

- [Unity Hub](https://unity.com/download)
- Unity **6000.6.2f1** (the exact version is recorded in
  `ProjectSettings/ProjectVersion.txt`)
- [Git](https://git-scm.com/) with [Git LFS](https://git-lfs.com/)

## Getting started

1. Clone the repository:
   ```bash
   git clone https://github.com/idangol519-a11y/KyubPon.git
   ```
2. In Unity Hub, choose **Add > Add project from disk** and pick the `KyubPon` folder.
3. Open the project. The first open takes a few minutes while Unity builds its
   `Library` folder.
4. Open `Assets/Game/Scenes/DropScreen.unity` (the title screen) and press **Play**.

## Project structure

```
Assets/Game/
  Fonts/       Fonts, each with its license file
  Scenes/      Unity scenes (DropScreen title screen, HomeScreen, Battle)
  Scripts/
    Core/      Settings and other shared game code
    Editor/    Editor-only tools (scene builders)
    Battle/    The battle: board, blocks, enemy, screen, and save file
    UI/        Screens and UI components
Packages/      Unity package manifest
ProjectSettings/
docs/          Design, project state, credits, and release steps
tools/         Helper scripts (Set-GameVersion.ps1)
CHANGELOG.md   Release history
CLAUDE.md      Project guidelines and coding conventions
```

## Branches and versions

- `main` is the only long-lived branch. It always holds the latest released version.
- Work happens on a short-lived `feature/<short-name>` branch, which is merged
  into `main` and then deleted.

The project uses [Semantic Versioning](https://semver.org/). Every release is
tagged (for example `v0.3.0`) and recorded in [CHANGELOG.md](CHANGELOG.md).

The version number is written in four files that must always agree. Do not edit
them by hand; use the script, which changes all of them together:

```powershell
.\tools\Set-GameVersion.ps1 -Check            # show the version in every place
.\tools\Set-GameVersion.ps1 -Version 0.3.0    # set a new version everywhere
```

The full release steps are in [docs/Releasing.md](docs/Releasing.md).

## License

No license has been granted. All rights reserved.
