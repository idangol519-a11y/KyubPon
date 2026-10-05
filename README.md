# KyubPon

A 2D strategy roguelite block game made in Unity.

You place cube-like creatures ("blocks") on a board and fight an enemy bot that
places its own. Blocks act through their stats and abilities, and abilities can
trigger each other in chain reactions. Between levels you visit a shop or an
event, and the run continues through harder and harder levels until you lose.

**Status:** early development, version `0.1.0`. The project currently contains
the Unity setup and a placeholder drop screen. See
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
4. Open `Assets/Game/Scenes/DropScreen.unity` and press **Play**.

## Project structure

```
Assets/Game/
  Scenes/      Unity scenes (DropScreen)
  Scripts/
    Editor/    Editor-only tools (scene builder)
    UI/        Screens and UI components
Packages/      Unity package manifest
ProjectSettings/
docs/          Design and project state documents
CHANGELOG.md   Release history
CLAUDE.md      Project guidelines and coding conventions
```

## Branches and versions

- `main` holds stable, released versions only.
- `develop` is the integration branch for ongoing work.
- `feature/<short-name>`, `release/<version>` and `hotfix/<short-name>` branch
  off as needed.

The project uses [Semantic Versioning](https://semver.org/). Every release is
tagged (for example `v0.1.0`) and recorded in [CHANGELOG.md](CHANGELOG.md).
The version number is set in **Edit > Project Settings > Player > Version**.

## License

No license has been granted. All rights reserved.
