# Dice Idler
An idle/incremental clicker game built in **Unity 6** where players roll dice to earn points, purchase upgrades, and unlock new dice types with unique materials and multipliers.

## Design Philosophy
A core goal of this project is to **use as few imported assets as possible**. Nearly everything — dice geometry, pip placement, sound effects, UI elements — is **generated entirely from code at runtime**. Audio is synthesized procedurally from basic waveforms, dice visuals are constructed programmatically, and materials are applied dynamically. This keeps the project lightweight and demonstrates how far you can get with pure code-driven content.

## Gameplay
- **Tap or click** anywhere on the scene to roll your dice.
- When each Die lands it awards points based on its roll value, type multiplier, and level multiplier.
- Spend points in the **shop** to unlock new dice types (e.g. Wood, Stone, Iron, Gold, Emerald, Sapphire, Amethyst, Runic, Void) — each with its own appearance and score multiplier.
- Purchasing additional copies of a dice type adds more dice to your pool; leveling dice up increases their score multiplier exponentially (×10 per level) and their physical size.

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
│   ├── GameManager.cs        # Core game loop, scoring, input handling
│   ├── SaveManager.cs        # JSON save/load with autosave
│   ├── AudioManager.cs       # Procedural SFX generation & pooled playback
│   ├── dice/
│   │   ├── DiceManager.cs        # Dice spawning, rolling, and lifecycle
│   │   ├── diceController.cs     # Physics-based roll, settle detection, pips
│   │   └── DicePreviewRenderer.cs
│   ├── Shop/
│   │   ├── ShopItem.cs       # ScriptableObject defining a dice type
│   │   ├── ShopManager.cs    # Purchase logic, price scaling, multipliers
│   │   ├── UIShopHandler.cs  # Populates shop UI from ShopItem data
│   │   └── UIShopItem.cs     # Individual shop entry UI behaviour
│   └── UI Tools/
│       ├── PopupTextHandler.cs       # Animated floating text
│       ├── ImageAligner.cs           # UI alignment utility
│       └── DiceShopWindowManager.cs  # Shop panel toggle
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

### Shop
`ShopItem` is a `ScriptableObject` (`[CreateAssetMenu]`) defining each dice type's metadata, inicluding multipliers, and custom materials. 
`ShopManager` handles purchases, deducting score and spawning new dice. Prices scale with a configurable multiplicative growth rate per purchase.

### Dice Thumbnail Renderer
`DicePreviewRenderer` generates **live 3D thumbnails** of each dice type for the shop UI — entirely at runtime, with no pre-baked images. 
It creates a hidden staging area far below the playfield, instantiates a copy of each dice type with its correct materials, and renders them with a dedicated off-screen camera and light onto per-item `RenderTexture`s. 
The dice slowly rotate in their thumbnails to give the shop a polished, dynamic feel. 
Rendering is throttled (~20 FPS) and batched to stay lightweight, while transforms rotate every frame for smooth motion. 
Shop UI elements simply bind the resulting texture to a `RawImage`.

### Save System
`SaveManager` persists game state (score, unlocked items, dice levels, purchase counts) to a JSON file. Features autosave on a configurable interval (default 30 s) and saves on application quit.

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
