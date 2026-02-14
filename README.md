# Dice Idler
An idle/incremental clicker game built in **Unity 6** where players roll dice to earn points, purchase upgrades, and unlock new dice types with unique materials and multipliers.

## Design Philosophy
A core goal of this project is to **use as few imported assets as possible**. Nearly everything — dice geometry, pip placement, sound effects, UI elements — is **generated entirely from code at runtime**. Audio is synthesized procedurally from basic waveforms, dice visuals are constructed programmatically, and materials are applied dynamically. This keeps the project lightweight and demonstrates how far you can get with pure code-driven content.

## Gameplay
- **Tap or click** anywhere on the scene to roll your dice.
- When each Die lands it awards points based on its roll value, type multiplier, and level multiplier.
- Spend points in the **dice shop** to unlock new dice types with their own appearance and score multiplier.
- Purchasing additional copies of a dice type adds more dice to your pool; leveling dice up increases their score multiplier exponentially (×10 per level) and their physical size.
- Purchase **auto-clicker upgrades** in the clicker shop to automatically roll dice of each tier at a configurable rate — each tier rolls independently.

## Project Structure
```
Assets/
├── Images/               # Sprites and textures
├── Materials/            # Dice and pip materials
├── Prefabs/
│   ├── Dice.prefab       # Base dice prefab with DiceController
│   ├── PopupText.prefab  # Floating score popup
│   └── ShopItem.prefab   # Shop UI entry
├── Scenes/
│   └── SampleScene.unity # Main game scene
├── Scripts/
│   ├── GameManager.cs            # Core game loop, scoring, input handling
│   ├── SaveManager.cs            # JSON save/load with autosave
│   ├── AudioManager.cs           # Procedural SFX generation & pooled playback
│   ├── AutoClickerManager.cs     # Per-tier auto-roll driver
│   ├── dice/
│   │   ├── DiceManager.cs        # Dice spawning, rolling (global & per-type)
│   │   ├── diceController.cs     # Physics-based roll, settle detection, pips
│   │   └── DicePreviewRenderer.cs
│   ├── Shop/
│   │   ├── ShopItem.cs               # ScriptableObject: dice type + auto-click metadata
│   │   ├── DiceShopManager.cs        # Dice purchase logic, price scaling, multipliers
│   │   ├── AutoClickShopManager.cs   # Auto-click upgrade purchases
│   │   ├── UIDiceShopHandler.cs      # Populates dice shop UI
│   │   ├── UIDiceShopItem.cs         # Individual dice shop entry UI
│   │   ├── UIAutoClickShopHandler.cs # Populates auto-click shop UI
│   │   └── UIAutoClickShopItem.cs    # Individual auto-click shop entry UI
│   └── UI Tools/
│       ├── UIWindow.cs                   # Base class for all sliding window panels
│       ├── DiceShopWindowManager.cs      # Dice shop window (inherits UIWindow)
│       ├── ClickerShopWindow.cs          # Auto-click shop window (inherits UIWindow)
│       ├── SettingsMenuWindow.cs         # Settings window (inherits UIWindow)
│       ├── PopupTextHandler.cs           # Animated floating text
│       └── ImageAligner.cs               # UI alignment utility
├── ShopData/Shop/        # ShopItem ScriptableObject assets
│   ├── Starter, Wood, Stone, Copper, Iron, Gold
│   ├── Emerald, Sapphire, Amethyst, Runic, Void
└── Settings/             # URP and render pipeline settings
```

## Key Systems

### Scoring
Managed by `GameManager`. Score is a `System.Numerics.BigInteger`, allowing unbounded growth. Each die roll produces:

$$\text{points} = \text{roll} \times \text{typeMultiplier} \times 10^{(\text{level} - 1)}$$

The displayed score animates toward the actual score with an interpolated counter and dynamic font scaling.

### Dice
`DiceManager` spawns dice from save data on startup and handles round-robin rolling. 
`DiceController` uses Unity physics (`Rigidbody`) for realistic rolls, detects when the die settles, reads the top face, and fires an `OnDiceSettled` event.
`DiceManager` also supports per-type rolling (`RollNextDiceOfType`) for the auto-clicker system, round-robining independently within each dice tier.

### Shops
The project has two shops — **Dice Shop** and **Auto-Clicker Shop** — both driven by the same `ShopItem` ScriptableObject.

`ShopItem` defines each dice tier's identity, dice-shop pricing, materials, multiplier, and auto-click upgrade pricing/CPS. A static `CalculatePrice()` helper centralises the incremental cost formula ($\text{base} \times \text{rate}^{\text{purchased}}$) so it is never duplicated.

`DiceShopManager` handles dice purchases (deducting score, spawning dice, cascading merges). `AutoClickShopManager` handles auto-click upgrade purchases and notifies `AutoClickerManager` to update the live roll rate.

### Auto-Clicker
`AutoClickerManager` drives automatic dice rolls per tier. On startup it reads save data to build a list of active tiers and their CPS (`autoClicksPerSecond × purchased`). Each frame it accumulates `deltaTime` per tier and fires `DiceManager.RollNextDiceOfType()` when enough time has banked, with a per-frame cap to prevent runaway catch-up. Purchases update the rate live via `RefreshTier()`.

### UI Windows
All sliding UI panels (dice shop, clicker shop, settings) inherit from `UIWindow`, which provides animated open/close tweens and **mutual exclusivity** — opening any window automatically closes all others via a static registry. Subclasses (`DiceShopWindowManager`, `ClickerShopWindow`, `SettingsMenuWindow`) add window-specific behaviour.

### Dice Thumbnail Renderer
`DicePreviewRenderer` generates **live 3D thumbnails** of each dice type for the shop UI — entirely at runtime, with no pre-baked images. 
It creates a hidden staging area far below the playfield, instantiates a copy of each dice type with its correct materials, and renders them with a dedicated off-screen camera and light onto per-item `RenderTexture`s. 
The dice slowly rotate in their thumbnails to give the shop a polished, dynamic feel. 
Rendering is throttled (~20 FPS) and batched to stay lightweight, while transforms rotate every frame for smooth motion. 
Shop UI elements simply bind the resulting texture to a `RawImage`.

### Save System
`SaveManager` persists game state (score, unlocked items, dice levels, purchase counts, auto-click upgrade counts) to a JSON file. Features autosave on a configurable interval (default 30 s) and saves on application quit.

### Audio
`AudioManager` generates SFX **procedurally at runtime** using basic waveforms (square, sawtooth, triangle, Perlin noise) — no external audio files needed, though can be supported. 
It manages a dynamically pooled set of `AudioSource` objects for overlapping playback.

## Platforms
Primarily designed for **Android**. PC (mouse input) is also supported for development and testing. The game runs at 60 FPS (`Application.targetFrameRate = 60`).

## Requirements
- Unity **6000.3.7f1** (Unity 6)
- Universal Render Pipeline (URP)
- TextMesh Pro
- Input System package

## Getting Started
1. Clone the repository.
2. Open the project in Unity 6000.3.7f1 or later.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press **Play**.
