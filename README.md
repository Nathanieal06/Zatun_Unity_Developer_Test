# Zatun_Unity_Developer_Test
# Zombie Survival Technical Demo

A high-fidelity Third-Person Zombie Survival Shooter built in Unity 6, showcasing advanced animation rigging, optimized AI systems, and a modular architecture.

## 🚀 Key Features

- **Advanced Combat System**: Responsive gunplay with a hybrid hitscan-projectile system for 100% reliability and visual immersion.
- **Procedural Animation**: Integrated Animation Rigging for procedural aiming, head tracking, and consistent weapon handling across movement states.
- **Dynamic Camera**: Cinemachine-driven camera system with state-driven FOV adjustments for ADS (Aim Down Sights) and hip-fire.
- **Intelligent AI**: Optimized Zombie AI with NavMesh navigation, spatialized 3D audio, and throttled decision-making for high performance.
- **Modular Architecture**: Data-driven weapon system using Scriptable Objects and the New Unity Input System for easy extensibility.
- **Visual Effects**: Custom SRP Batcher-compatible dissolve shaders for enemies and URP-optimized particles.

## 🎮 Controls

| Action | Input |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` / Arrow Keys |
| **Sprint** | `Left Shift` |
| **Jump** | `Space` |
| **Aim (ADS)** | `Right Mouse Button` |
| **Fire** | `Left Mouse Button` |
| **Reload** | `R` |
| **Interact / Pickup** | `F` |
| **Drop Weapon** | `G` |
| **Switch Weapons** | `1`, `2`, `3`, `4` |
| **Pause** | `Esc` |

## 🛠️ Technical Optimizations

1. **Hybrid Hitscan-Projectile System**: Separates visual projectile travel from logical hit registration to ensure high-speed bullets never "tunnel" through targets while maintaining low CPU cost.
2. **SRP Batcher Compatible Shaders**: Custom HLSL shaders (e.g., `URP_Dissolve_Texture.shader`) utilize `CBUFFER` blocks to allow Unity to batch draw calls for multiple dissolving enemies simultaneously.
3. **Throttled Spawning & AI**: Spawning logic uses coroutines to distribute heavy NavMesh calculations over multiple frames. AI updates are globally throttled during cutscenes and menus.
4. **Physics Layer Optimization**: Global collision matrix tuning to ignore redundant bullet-player checks at the engine level.

## 📁 Project Structure

- **`Assets/Scripts`**: Core gameplay logic, AI, and systems.
- **`Assets/Prefabs`**: Optimized game objects and modular environment pieces.
- **`Assets/Shaders`**: Custom URP shaders.
- **`Assets/Settings`**: URP assets and Input System configurations.
- **`Tools`**: Custom Python automation scripts for batch processing assets.

## 🤖 Automation Tools

The project includes several Python utility scripts in the root directory to streamline the development workflow:
- `fix_all_prefabs.py`: Automates batch processing of weapon prefabs and asset metadata.
- `fix_muzzleflash.py`: Corrects alignment and scaling of muzzle flash VFX across all weapon variants.
- `fix_prefabs.py`: General utility for prefab data consistency.

## 📜 Credits

This project utilizes high-quality assets from various creators:
- **Models**: DJMaesen (Pistol), Pieter Ferreira (AK47), Alpen Wolf (Characters), VIVID Arts (Environment).
- **Audio**: Pixabay, Dragon-studio.
- **Animations**: Mixamo, MoCap Online, ActorCore.
- **VFX**: Jean Moreno (War FX), BitGem (Water).
- **Inspiration**: Developed with references from Code Monkey and Brackeys.

---
*Developed as part of a Technical Test for Zatun.*
