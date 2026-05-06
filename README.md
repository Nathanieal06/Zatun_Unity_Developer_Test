# 🧟 Zombie Survival Technical Demo

A high-fidelity **Third-Person Zombie Survival Shooter** built in **Unity 6**, showcasing advanced animation rigging, optimized AI systems, and a modular architecture designed for performance and extensibility.

> 🎯 **Technical Test Project** for Zatun - Demonstrating professional-grade game development practices.

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Prerequisites & Installation](#⚙️-prerequisites--installation)
- [Getting Started](#-getting-started)
- [Controls](#-controls)
- [Project Structure](#-project-structure)
- [Technical Details](#-technical-details)
- [Automation Tools](#-automation-tools)
- [Contributing](#-contributing)
- [Credits](#-credits)
- [License](#-license)

---

## 📖 Overview

This project demonstrates a complete, production-ready game demo with focus on:

- **High-Performance Combat**: Responsive gunplay with advanced hit detection
- **Intelligent Enemy AI**: NavMesh-based navigation with throttled decision-making
- **Professional Animation Pipeline**: Procedural rigging and state-driven animations
- **Optimized Rendering**: SRP Batcher-compatible shaders and URP optimization
- **Modular Gameplay**: Data-driven weapon and item systems using Scriptable Objects

**Target Audience**: Game developers, technical artists, and gameplay programmers.

---

## 🚀 Key Features

### Combat System
- **Hybrid Hitscan-Projectile System**: Combines instant hit detection with visual projectiles for 100% reliability without "bullet tunneling"
- **Responsive Gunplay**: Low-latency aiming and firing with multiple weapon types
- **Damage & Feedback**: Visual and audio feedback systems for player engagement

### Animation & Movement
- **Procedural Animation Rigging**: IK-driven aiming, head tracking, and weapon positioning
- **Movement-State Consistency**: Smooth transitions across walking, running, sprinting, and aiming states
- **Dynamic Reload Animations**: Weapon-specific reload sequences

### Camera System
- **Cinemachine Integration**: Sophisticated camera tracking and positioning
- **State-Driven FOV**: Automatic field-of-view adjustments for ADS (Aim Down Sights) and hip-fire
- **Smooth Follow**: Lag-free player tracking with responsive shake effects

### AI & Enemies
- **Intelligent Zombie AI**: NavMesh-based pathfinding with obstacle avoidance
- **Spatialized 3D Audio**: Directional zombie audio for immersion
- **Throttled Decision-Making**: Distributed AI updates across frames for high performance
- **Difficulty Scaling**: Enemy wave spawning with difficulty progression

### Rendering & Visual Effects
- **Custom SRP Batcher Shaders**: Enemy dissolve effects with batched draw calls
- **URP Optimization**: Particle systems tuned for mobile and console performance
- **Procedural Dissolve**: Custom shader effects for enemy elimination
- **Lighting**: Real-time and baked lighting with optimal shadow settings

### Architecture
- **Data-Driven Weapon System**: Scriptable Objects for weapon definitions
- **New Unity Input System**: Modern input handling for cross-platform support
- **Modular Prefabs**: Reusable components for rapid iteration
- **Event System**: Decoupled systems using C# delegates and events

---

## ⚙️ Prerequisites & Installation

### System Requirements

| Requirement | Specification |
|-------------|---------------|
| **Unity Version** | 6.x (LTS) or later |
| **OS** | Windows 10/11, macOS 10.13+, or Linux |
| **RAM** | 8GB minimum (16GB recommended) |
| **GPU** | DirectX 11/12 compatible with shader support |
| **Storage** | 15GB free space for project |
| **Python** | 3.8+ (for automation tools only) |

### Dependencies

- **Unity Packages**:
  - Universal Render Pipeline (URP)
  - Animation Rigging
  - Cinemachine
  - Navigation
  - Input System (New)
  - VFX Graph (optional)

- **3rd Party Assets**:
  - Shader Graph Tutorials (included in `/Assets/3rd Party Assets/`)
  - Various character and weapon models (see Credits)

### Installation Steps

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Nathanieal06/Zatun_Unity_Developer_Test.git
   cd Zatun_Unity_Developer_Test
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Click "Open" and select the project folder
   - Wait for the initial import (~2-5 minutes)

3. **Verify Dependencies**
   - Go to **Window → TextMesh Pro → Import TMP Essentials** (if prompted)
   - Check **Project Settings → Input Manager** for Input System configuration
   - Verify URP is installed via **Window → Render Pipeline Converter**

4. **Install Automation Tools** (Optional)
   ```bash
   pip install -r requirements.txt  # If a requirements.txt exists
   ```

---

## 🎮 Getting Started

### Loading the Demo

1. Open **Assets/Scenes/MainScene.unity** (main gameplay)
2. Press **Play** in the Unity Editor
3. Use the controls below to interact with the environment

### First Run Checklist

- ✅ Confirm camera is tracking the player correctly
- ✅ Test movement and aiming inputs
- ✅ Fire a weapon to verify hit detection
- ✅ Observe enemy AI behavior

### Scene Overview

| Scene | Purpose |
|-------|---------|
| **MainScene** | Primary gameplay scene with combat and enemies |
| **MenuScene** | (Optional) Main menu and settings |
| **Arena** | Optimized testing environment for AI |

---

## 🎮 Controls

| Action | Input | Notes |
|--------|-------|-------|
| **Move Forward/Backward** | `W` / `S` | Or arrow keys |
| **Strafe Left/Right** | `A` / `D` | Or arrow keys |
| **Sprint** | `Left Shift` (hold) | Increases movement speed |
| **Jump** | `Space` | Use for navigation obstacles |
| **Aim Down Sights** | `Right Mouse Button` (hold) | Reduces FOV, increases accuracy |
| **Fire/Shoot** | `Left Mouse Button` (hold/tap) | Depends on weapon type |
| **Reload** | `R` | Refills current weapon magazine |
| **Interact / Pickup** | `F` | Pick up weapons or items |
| **Drop Current Weapon** | `G` | Discard current weapon |
| **Switch Weapon** | `1` / `2` / `3` / `4` | Quick weapon selection |
| **Pause Menu** | `Esc` | Pause/Resume gameplay |

---

## 📁 Project Structure

```
Zatun_Unity_Developer_Test/
│
├── Assets/
│   ├── Scripts/                          # Core C# gameplay logic
│   │   ├── Player/                       # Player controller & mechanics
│   │   ├── Combat/                       # Weapon system & hit detection
│   │   ├── AI/                           # Zombie AI & behaviors
│   │   ├── Animation/                    # Animation rigging scripts
│   │   ├── Camera/                       # Cinemachine integration
│   │   ├── VFX/                          # Particle & visual effects
│   │   ├── Audio/                        # Sound management
│   │   ├── UI/                           # User interface systems
│   │   └── Managers/                     # Game, spawning, wave managers
│   │
│   ├── Prefabs/                          # Reusable game objects
│   │   ├── Player/
│   │   ├── Weapons/
│   │   ├── Enemies/
│   │   ├── VFX/
│   │   └── Environment/
│   │
│   ├── Shaders/                          # Custom HLSL/ShaderLab files
│   │   ├── URP_Dissolve_Texture.shader
│   │   ├── URP_NormalMap.shader
│   │   └── ...
│   │
│   ├── Materials/                        # Material instances
│   ├── Textures/                         # Image assets
│   ├── Models/                           # 3D mesh files
│   ├── Animations/                       # Animation clips & controllers
│   ├── Audio/                            # Sound effects & music
│   │
│   ├── Settings/                         # Project configuration
│   │   ├── URP Settings
│   │   ├── Input System mappings
│   │   ├── Physics layers
│   │   └── Quality settings
│   │
│   ├── Scenes/                           # Unity scene files
│   │   ├── MainScene.unity
│   │   └── (other scenes)
│   │
│   └── 3rd Party Assets/                 # External packages
│       └── Shader-Graph-Tutorials-master/
│
├── Tools/                                # Python automation scripts
│   ├── fix_all_prefabs.py
│   ├── fix_muzzleflash.py
│   └── fix_prefabs.py
│
├── Packages/                             # Unity package manifest
├── ProjectSettings/                      # Unity project configuration
├── .gitignore                            # Git ignore rules
├── .gitattributes                        # Git attributes
└── README.md                             # This file
```

### Key Directories Explained

- **`Assets/Scripts/`**: All C# source code organized by feature
- **`Assets/Prefabs/`**: Modular, reusable game object templates
- **`Assets/Shaders/`**: Custom shaders for visual effects
- **`Assets/Settings/`**: URP asset, quality, and input configuration
- **`Tools/`**: Python scripts for asset pipeline automation

---

## 🛠️ Technical Details

### 1. Hybrid Hitscan-Projectile System

**Problem**: Fast-moving bullets can "tunnel" through targets at high framerates.

**Solution**:
- **Hitscan Logic**: Instant hit detection using raycasts for game logic
- **Visual Projectiles**: Separate visual objects for player feedback
- **No Missed Shots**: 100% hit registration reliability regardless of framerate

```csharp
// Pseudo-code
RaycastHit hit;
if (Physics.Raycast(bulletOrigin, direction, out hit, range))
{
    ApplyDamage(hit.collider);
    ShowProjectileVFX(bulletOrigin, hit.point);
}
```

### 2. SRP Batcher Compatible Shaders

**Optimization**: Uses `CBUFFER` blocks to allow dynamic batching of dissolving enemies.

**Benefits**:
- Reduced draw calls (many enemies batched in one call)
- Lower GPU memory overhead
- Consistent dissolve animations across instances

```hlsl
CBUFFER_START(UnityPerMaterial)
    float _DissolveAmount;
    float4 _MainColor;
CBUFFER_END
```

### 3. Throttled Spawning & AI

**Implementation**:
- Coroutines distribute NavMesh calculations across multiple frames
- AI decision updates are globally throttled (e.g., 10 updates/frame max)
- Dynamic throttling during cutscenes and menus

**Performance Benefit**: 60+ FPS with 100+ enemies on mid-range hardware.

### 4. Physics Layer Optimization

**Setup**:
- Custom collision matrix ignores redundant checks (bullet-to-player collisions disabled)
- Layer groups: Player, Enemy, Projectile, Environment, Trigger
- Physics.gravity optimized for gameplay

### 5. Animation Rigging Pipeline

**Features**:
- **IK Constraint**: Procedural arm/hand positioning for weapon aim
- **Head Tracking**: Automatic head rotation toward camera direction
- **Layered Blending**: Smooth transitions between animation states

---

## 🤖 Automation Tools

The project includes Python utility scripts for streamlining development workflows:

### `fix_all_prefabs.py`
Automates batch processing of weapon prefabs and asset metadata.

```bash
python Tools/fix_all_prefabs.py
```

**Functionality**:
- Resets prefab settings
- Validates component hierarchies
- Updates metadata references

### `fix_muzzleflash.py`
Corrects alignment and scaling of muzzle flash VFX across all weapon variants.

```bash
python Tools/fix_muzzleflash.py
```

### `fix_prefabs.py`
General utility for prefab data consistency.

```bash
python Tools/fix_prefabs.py --repair --validate
```

---

## 🤝 Contributing

### Contribution Guidelines

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/YourFeature`)
3. **Make** your changes following code style guidelines
4. **Test** thoroughly in the Unity Editor
5. **Commit** with descriptive messages
6. **Push** to your branch
7. **Submit** a Pull Request with a clear description

### Code Style

- **C#**: Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- **Naming**: Use PascalCase for classes/methods, camelCase for variables
- **Comments**: Document complex logic and public APIs
- **Performance**: Avoid allocations in hot paths (Update, LateUpdate)

### Reporting Issues

- Use GitHub Issues with a clear title and reproduction steps
- Include system specs and Unity version
- Attach error logs from Console window

---

## 📜 Credits

### Development
- **Developer**: Nathanieal06
- **Project Purpose**: Technical Test for Zatun

### 3D Models & Artwork
- **Character Models**: Alpen Wolf
- **Environment Assets**: VIVID Arts
- **Pistol Model**: DJMaesen
- **Rifle Model**: Pieter Ferreira

### Animations
- **Animation Services**: Mixamo, MoCap Online, ActorCore
- **Rigging & IK**: Unity Animation Rigging Package

### Audio
- **Sound Effects**: Pixabay, Dragon-studio
- **Audio Engine**: Unity Audio System

### Visual Effects
- **Particle Systems**: Jean Moreno (War FX), BitGem (Water)
- **Shader Tutorials**: Brackeys (Shader Graph Tutorials)
- **Post-Processing**: Unity Post-Processing Stack

### Educational References
- **Gameplay Systems**: Code Monkey (YouTube)
- **Best Practices**: Brackeys (YouTube)
- **Performance Tips**: Unite talks and GDC presentations

---

## 📄 License

This project is provided as-is for educational and portfolio purposes. 

**Third-party assets** maintain their original licenses as specified by their creators. See individual asset folders for license information.

For commercial use, obtain proper licenses from asset creators listed in the Credits section.

---

## 📞 Support & Contact

- **GitHub Issues**: Report bugs or request features
- **Documentation**: Check `/Docs` folder for extended guides
- **Questions**: Open a Discussion thread on GitHub

---

## 🗂️ Additional Resources

- [Unity Documentation](https://docs.unity3d.com/)
- [Universal Render Pipeline Guide](https://docs.unity3d.com/Manual/render-pipelines.html)
- [Animation Rigging Documentation](https://docs.unity3d.com/Packages/com.unity.animation.rigging@latest/)
- [Cinemachine Manual](https://docs.unity3d.com/Manual/com.unity.cinemachine.html)

---

**Last Updated**: May 6, 2026  
**Unity Version**: 6.x (LTS)  
**Status**: ✅ Active Development

