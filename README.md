# Wiggle

A worm game prototype built with Unity 6 and DOTS/ECS.

## Features
- Player-controlled worm that follows the input direction
- Bot worms that seek food
- Food spawning, growth and scoring
- Segments follow the head along a recorded path
- Worm-vs-worm collisions: a head touching another worm kills it and its segments drop food
- Start screen (Play / Quit), pause menu (Esc), game-over screen (Play Again / Quit; Space also works)
- Bots respawn automatically; the player respawns in place from the game-over screen
- Boost: hold Space or left mouse to sprint; a stamina bar limits it
- Scoreboard (UI Toolkit)

## Requirements
- Unity 6 (URP)
- Packages: Entities 1.4.3, Entities Graphics, Input System (see `Packages/manifest.json`)

## Running
Open the project in Unity and play `Assets/Scenes/MainScene`.

## Code layout (`Assets/Scripts`)
- `Authoring/` - bakers that convert scene objects and prefabs to entities
- `Components/` - ECS components and buffers
- `Systems/` - gameplay systems (input, bot AI, movement, growth, food, death, respawn, score)
- `Mono/` - camera follow and scoreboard UI
