# Dice Clicker
An idle/incremental clicker game built in **Unity 6** where players roll dice to earn points, purchase upgrades, and unlock new dice types with unique materials and multipliers.


<img width="397" height="799" alt="image" src="https://github.com/user-attachments/assets/45c76a5c-270b-4659-948f-76e981358df1" />
<img width="399" height="821" alt="image" src="https://github.com/user-attachments/assets/51401178-01c2-4d53-9dbf-52fc0108515b" />



## Gameplay
- **Tap or click** anywhere on the scene to roll your dice.
- When each Die lands it awards points based on its roll value, type multiplier, and level multiplier.
- Spend points in the **dice shop** to unlock new dice types with their own appearance and score multiplier.
- Purchasing additional copies of a dice type adds more dice to your pool; leveling dice up increases their score multiplier exponentially (×10 per level) and their physical size.
- Purchase **auto-clicker upgrades** in the clicker shop to automatically roll dice of each tier at a configurable rate — each tier rolls independently.

## Key Systems
### Scoring
Managed by `GameManager`. Score is a `System.Numerics.BigInteger`, allowing unbounded growth. 
Each die roll produces:

$$\text{points} = \text{roll} \times \text{typeMultiplier} \times 10^{(\text{level} - 1)}$$

### Dice
`DiceManager` spawns dice from save data on startup and handles round-robin rolling. 
`DiceController` uses Unity physics (`Rigidbody`) for realistic rolls, detects when the die settles, reads the top face, and fires an `OnDiceSettled` event.
`DiceManager` also supports per-type rolling (`RollNextDiceOfType`) for the auto-clicker system, round-robining independently within each dice tier.

### Shops
The project has two shops — **Dice Shop** and **Auto-Clicker Shop** — both driven by the same `ShopItem` ScriptableObject.
`ShopItem` defines each dice tier's identity, dice-shop pricing, materials, multiplier, and auto-click upgrade pricing/CPS. 
A static `CalculatePrice()` helper centralises the incremental cost formula ($\text{base} \times \text{rate}^{\text{purchased}}$) so it is never duplicated.
`DiceShopManager` handles dice purchases (deducting score, spawning dice, cascading merges).

### Auto-Clicker
`AutoClickerManager` drives automatic dice rolls per tier. 
`AutoClickShopManager` handles auto-click upgrade purchases and notifies `AutoClickerManager` to update the live roll rate.
Each frame it accumulates `deltaTime` per tier and rolls the next available dice when enough time has banked.

### User Interface
All UI windows are made using UI Toolkit.
All in-world UI elements are made using uGUI.

### Dice Thumbnail Renderer
`DicePreviewRenderer` generates **live 3D thumbnails** of each dice type for the shop UI — entirely at runtime, with no pre-baked images. 
Rendering is throttled (~20 FPS) and batched to stay lightweight, while transforms rotate every frame for smooth motion.
Shop UI elements simply bind the resulting texture to a `RawImage`.

### Save System
`SaveManager` persists game state (score, unlocked items, dice levels, purchase counts, auto-click upgrade counts) to a JSON file. 
Features autosave on an interval (default 30 s) and saves on application quit.

### Audio
`AudioManager` generates SFX **procedurally at runtime** using basic waveforms (square, sawtooth, triangle, Perlin noise) — no external audio files needed, though are supported. 
It manages a dynamically pooled set of `AudioSource` objects for uninterrupted overlapping playback.
The only raw audio files used are for background music.

## Platforms
Primarily designed for **Android**. PC (mouse input) is also supported for development and testing. 

## Requirements
- Unity **6000.3.7f1** (Unity 6)
- Universal Render Pipeline (URP)
- TextMesh Pro

## Getting Started
1. Clone the repository.
2. Open the project in Unity 6000.3.7f1 or later.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press **Play**.
