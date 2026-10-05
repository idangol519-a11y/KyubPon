# CLAUDE.md — Project Guidelines

This file defines how Claude works on this project. Read it at the start of every session and follow it unless I give a different instruction in the chat. When a rule here conflicts with a direct instruction from me, my instruction wins, and you should suggest updating this file.

---

## 1. Project overview

**Working title:** Block Roguelike (placeholder)
**Engine:** Unity, 2D
**Genre:** Strategy roguelike / roguelite, player vs. computer
**Target platforms:** Steam (Windows PC) first. Mobile (Android / iOS) later.

### Core gameplay loop
1. **Battle.** The player places cube-like creatures ("blocks") on the board. The enemy bot sits at the top of the screen, places its own blocks at the start of the match, and may place more during the level.
2. **Resolution.** Blocks act using their stats and abilities. Abilities can trigger other abilities, causing chain reactions.
3. **Win / loss.** The level is won when the enemy has no blocks left. The run is lost when the player has 0 blocks left.
4. **Between levels.** The player visits the **Shop** (buy blocks and items) or an **Event** (accept a condition for a bonus or a penalty).
5. **Repeat.** The run continues through an endless series of levels with rising difficulty, until the player loses.

### Blocks
Every block has:
- **HP** — damage it can take before it is destroyed.
- **Defence** — reduces incoming damage.
- **Abilities** — one or more effects, for example:
  - **Physical:** directional attacks, attack power, pushing blocks off the board edge.
  - **Magic:** freeze, burn, teleport, and other status or abstract effects.

### Screens and flow
- Opening animation (skippable) → Home screen.
- Home screen buttons: **Continue**, **Play**, **Settings**, **Info**, **Quit**. Each opens its own page or performs its action. Continue is hidden or disabled when there is no saved run.
- In-run screens: Battle, Shop, Event, Run Summary (win/loss), Pause menu.

### Design changes
The design is expected to change. I will provide plans, drawings, sketches, or references to other games. When I do:
1. Summarize your understanding of the change in a few bullet points.
2. List the systems, files, and data that would be affected.
3. For large changes, wait for my confirmation before restructuring.
4. Update `docs/GameDesign.md` so the document always matches the game.

Reference games for tone and mechanics: Balatro, Slay the Spire, Monster Train, Luck be a Landlord, Peglin, Backpack Hero, Shogun Showdown.

---

## 2. Guiding principles

1. **Beginner-friendly.** I am learning. Code and folders must be easy for a beginner to read, find, and edit.
2. **Data-driven and moddable.** Game content lives in JSON files and PNG images, not hardcoded in C#.
3. **Replaceable art.** Every visual can be swapped by replacing a PNG file, with no code change.
4. **PC first, mobile-ready.** Build for Steam first, but do not make choices that block a later mobile port.
5. **Legal only.** Use only assets, code, and tools we have the right to use.
6. **Small, working steps.** Every change should leave the project compiling and playable.

---

## 3. Project structure

Keep folders shallow. Do not create empty folders or folders "for later." Add a folder only when it holds real files.

```
Assets/
  Game/
    Scenes/          Boot, MainMenu, Run
    Scripts/
      Core/          Game state, save/load, data loading, logging
      Blocks/        Block model, stats, placement
      Abilities/     Triggers, conditions, effects, chain resolver
      Enemy/         Enemy bot logic
      Run/           Level flow, Shop, Events, rewards
      UI/            Screens and UI components
      Platform/      Steam integration, input, platform differences
    Prefabs/
    Audio/
  StreamingAssets/
    Content/
      Blocks/
        FireBlock/
          FireBlock.json
          FireBlock.png
      Items/
        MagicPotion/
          MagicPotion.json
          MagicPotion.png
      Events/
      Enemies/
Tests/               Edit-mode tests for game logic
docs/
  GameDesign.md      Current design (kept in sync with the game)
  DataFormat.md      JSON fields for every content type, with examples
  GameState.md       What is built, what is in progress, known issues
tools/
  steam/             SteamPipe build scripts and depot configs
CHANGELOG.md
CLAUDE.md
```

### One folder per content asset
Every block, item, event, and enemy gets **its own named folder** containing everything that defines it:
- `<Name>.json` — its data (stats, abilities, price, description).
- `<Name>.png` — its sprite.
- Extra PNGs (e.g. `<Name>_icon.png`, animation frames) if needed.

When you create a new asset, always create its full folder, even if the art is a placeholder. Use **PascalCase** folder names that describe the asset (`FireBlock`, `MagicPotion`, `FrozenLakeEvent`).

### Placeholder art
- When real art is missing, generate a simple placeholder with Unity (a colored square or basic shape) and **save it as a PNG** in the asset's folder, at the final intended size.
- State the expected PNG size and format in `docs/DataFormat.md` (e.g. 128×128, transparent background), so I can replace the file directly.
- If a PNG is missing at runtime, show a generated fallback (colored square with the asset name) and log a warning. Never crash.

---

## 4. Data and modding

- All content (blocks, abilities, items, events, enemies, shop prices, difficulty curves) is defined in JSON under `StreamingAssets/Content/`.
- C# defines the **behaviors** ("verbs"). JSON selects and configures them by string id.
- Model abilities as **Trigger → Condition → Effect**:
  - Trigger: `OnPlaced`, `OnTurnStart`, `OnHit`, `OnDestroyed`, `OnAbilityTriggered`, …
  - Condition: `IfAdjacentTo`, `IfHpBelow`, `IfEnemyInDirection`, …
  - Effect: `DealDamage`, `Push`, `Freeze`, `Burn`, `Teleport`, `AddDefence`, …
- Every JSON object has a unique `id` and a `version` field.
- Use Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`), not `JsonUtility`.
- One `ContentLoader` class loads all content at startup. If a file is invalid, log the file name and the problem, skip that file, and continue.
- Keep the loader behind an interface so mod folders (persistent data path, Steam Workshop) can be added later without rewriting it.
- On Android, `StreamingAssets` must be read with `UnityWebRequest`. Keep file access inside the loader so this is a one-place change.
- When adding a new content type or field, add an example JSON file and update `docs/DataFormat.md` in the same change.

Example:
```json
{
  "id": "fire_block",
  "version": 1,
  "name": "Fire Block",
  "sprite": "FireBlock.png",
  "hp": 10,
  "defence": 2,
  "price": 5,
  "abilities": [
    { "trigger": "OnTurnStart", "condition": "IfEnemyInDirection", "direction": "Up",
      "effect": "Burn", "amount": 3, "duration": 2 }
  ]
}
```

---

## 5. Coding guidelines

### Naming and readability
- Use clear, full names: `BlockHealth`, `ApplyBurnEffect`, `enemySpawnDelay`. Avoid abbreviations like `bh`, `tmp`, `mgr`.
- C# conventions: `PascalCase` for classes, methods, and public properties; `camelCase` for local variables and parameters; `_camelCase` for private fields.
- One class per file. The file name matches the class name.
- Keep methods short and focused on one job. Split methods longer than about 40 lines.
- Add a short `/// <summary>` comment to every public class and method, written in plain English. Comment **why** something is done, not what each line does.
- No magic numbers. Put tunable values in JSON or a clearly named constant.

### Structure
- Keep game rules in plain C# classes, separate from Unity. Use `MonoBehaviour`s only to connect logic to scenes, visuals, and input. This makes the rules testable and easy to follow.
- Prefer simple, direct code over clever patterns. Do not add frameworks, dependency-injection libraries, or abstractions until a real need appears.
- Avoid global singletons except for a small number of clearly named services (e.g. `GameServices`), documented in `docs/GameState.md`.
- Use `[SerializeField] private` instead of public fields for Inspector values.
- Do not hand-edit `.unity` scene or `.prefab` YAML. Create objects through editor scripts or tell me exactly what to set in the Editor.

### Chain reactions and scoring
- Resolve ability triggers with a **queue** (breadth-first), not recursion, so long chains cannot overflow the stack.
- Give each chain a **chain id** and enforce a **maximum trigger count** per chain to stop infinite loops. Log when the cap is hit.
- Scores and damage may grow very large. Use a number type that cannot overflow (e.g. `double`, or a big-number type if needed) and format large values for display (1.2K, 3.4M, 5.6e12).
- Keep resolution deterministic: use a seeded random number generator per run, so a run can be reproduced from its seed.

### Performance
- Avoid allocations in code that runs every frame or inside chain resolution (no `new` lists, LINQ, or string building in hot loops).
- Cache component lookups. Do not call `GetComponent` or `Find` every frame.
- Keep a stress-test scene with a full board and long chains, and check it after changes to abilities or resolution.

### Errors and logging
- Use one `GameLog` helper with levels: `Debug`, `Info`, `Warn`, `Error`.
- Log the game version, platform, and seed at startup.
- Never fail silently. Never let bad data or a missing file crash the game.

### Testing
- Write edit-mode tests for game rules: damage, defence, abilities, chain resolution, win/loss conditions, shop prices.
- Run tests before saying a task is done. If tests cannot be run, say so.

---

## 6. Settings, input, and accessibility

- **Audio:** Master, Music, and SFX sliders through the Audio Mixer, using a logarithmic volume scale.
- **Display:** Resolution, fullscreen/windowed, VSync, and a frame-rate cap. Risky display changes show a "Keep these settings?" prompt that reverts automatically after 10 seconds.
- **Graphics:** Quality presets (Low / Medium / High) rather than raw technical toggles.
- **Language:** Use Unity's Localization package. No hardcoded player-facing text.
- **Accessibility:** Options to reduce screen shake and flashing effects; readable minimum text size.
- **Input:** Use Unity's Input System. Support mouse, keyboard, and controller. Drag-and-drop must also work as "press to pick up, press to place" for controllers and touch.
- **Admin mode:** A toggle in Settings that gives infinite money and unlocks all content.
  - Implement it through one `AdminMode` class, so its effects are easy to find.
  - Show it in development builds. In release builds, hide it unless a clearly documented flag enables it.
  - Runs played with admin mode on are marked and excluded from achievements and leaderboards.

---

## 7. Platforms and Steam

- Build the **Windows Steam version first**. Mobile comes later.
- Keep platform-specific code inside `Scripts/Platform/`, behind simple interfaces (e.g. `IPlatformServices`), so mobile can be added without touching game logic.
- UI must scale across resolutions and aspect ratios (16:9 primary; also test 16:10 for Steam Deck and ultrawide).
- Use **Steamworks.NET** for Steam features (achievements, cloud saves, later Workshop).
- Steam build scripts and depot configs live in `tools/steam/`. Never commit Steamworks login credentials or other secrets.
- Aim for Steam Deck compatibility: controller support with correct button icons, readable text, and a stable default frame rate.

---

## 8. Legal and assets

- Use only assets we own, created ourselves, or licensed for commercial use (e.g. CC0 or clearly licensed packs).
- Record every third-party asset in `docs/Credits.md` with its source, author, and license.
- Do not copy art, names, music, or code from other games or copyrighted works.
- Do not add packages with unclear or non-commercial licenses.
- Track any AI-generated content so it can be declared in Steam's AI content disclosure.

---

## 9. Version control (Git and GitHub)

- Repository hosted on GitHub.
- Use a Unity `.gitignore` (ignore `Library/`, `Temp/`, `Obj/`, `Logs/`, `Builds/`, `UserSettings/`).
- Use **Git LFS** for PNG, audio, and other large binary files.
- Unity settings: Visible Meta Files and Force Text serialization. Always commit `.meta` files with their assets.

### Branches
- `main` — stable, released versions only.
- `develop` — integration branch for ongoing work.
- `feature/<short-name>` — one feature or system (e.g. `feature/shop-screen`).
- `release/<version>` — preparing a release.
- `hotfix/<short-name>` — urgent fixes to a released version.

### Commits and versions
- Small, focused commits with clear messages in the present tense: `Add burn effect to FireBlock`.
- Use **Semantic Versioning**: `MAJOR.MINOR.PATCH`, with pre-release tags such as `0.3.0-alpha`.
- Tag every release (`v0.3.0`) and update `CHANGELOG.md` using the Keep a Changelog format.
- Show the version number in the game (Home screen corner and log files).

---

## 10. How Claude should work

- Before a large task, give a short plan and list the files you will create or change.
- Work in small steps that compile and run. Do not leave the project broken between steps.
- Ask before adding packages, renaming or moving folders, or changing the architecture.
- After each task, tell me:
  1. What changed.
  2. Anything I need to do in the Unity Editor.
  3. How to test it.
- Keep `docs/GameState.md` up to date: built systems, work in progress, and known issues.
- Explain new concepts in simple terms when they first appear, since I am learning.
- If a request conflicts with these guidelines, point out the conflict and suggest an option before continuing.
