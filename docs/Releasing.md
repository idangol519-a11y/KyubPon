# Releasing a version

How the version number is managed, and the steps to publish a new version.

## Where the version lives

| Place | What it is for |
|---|---|
| `ProjectSettings/ProjectSettings.asset` (`bundleVersion`) | The real source. Unity shows it in **Project Settings > Player > Version**, and the game reads it through `Application.version` for the Home screen corner and the log. |
| `docs/GameState.md` ("Current version") | Project status document. |
| `README.md` ("version `x.y.z`") | First thing people see on GitHub. |
| `CHANGELOG.md` (`## [x.y.z] - date`) | What changed in each version. |
| Git tag `vx.y.z` on `main` | Marks the exact commit of each release. |

`tools/Set-GameVersion.ps1` keeps the four files identical. Game code must never
contain a version number of its own; it always reads `Application.version`.

## Choosing the number (Semantic Versioning)

`MAJOR.MINOR.PATCH`, for example `0.3.0`:

- **PATCH** (0.2.0 to 0.2.1): fixes and small changes, nothing new for the player.
- **MINOR** (0.2.1 to 0.3.0): a new feature or screen.
- **MAJOR** (0.x to 1.0.0): the first full release, or a change that breaks saved games or mods.

Names always match the number:

| Thing | Name |
|---|---|
| Release branch (optional) | `release/0.3.0` |
| Merge commit on `main` | `Release 0.3.0` |
| Git tag | `v0.3.0` |
| Changelog heading | `## [0.3.0] - 2026-10-06` |

## Steps

While working, list every change under `## [Unreleased]` in `CHANGELOG.md`.
When `develop` is ready to release:

```powershell
# 1. Set the new version in every file. Unity picks the change up
#    the next time its window is focused.
git checkout develop
.\tools\Set-GameVersion.ps1 -Version 0.3.0

# 2. Read CHANGELOG.md and tidy the notes, then commit.
git commit -am "Bump version to 0.3.0"

# 3. Merge to main, tag, and push.
git checkout main
git merge --no-ff develop -m "Release 0.3.0"
git tag -a v0.3.0 -m "KyubPon 0.3.0"
git push origin develop main v0.3.0
git checkout develop

# 4. Confirm everything agrees, including the tag.
.\tools\Set-GameVersion.ps1 -Check
```

## Unity Hub

Unity Hub's Projects page has no column for a game's own version. It shows the
project name (the folder name), when it was last modified, and the Unity Editor
version. The game version is visible inside the project instead: on the Home
screen corner in Play mode and in **Project Settings > Player > Version**.
