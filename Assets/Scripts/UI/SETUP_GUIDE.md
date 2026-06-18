# Shadow Beat: Fragmentos de Luz — UI Polish Kit
## Complete Unity Setup Guide

> Project integration note: the scripts are under the `ShadowBeat.UI` namespace so
> they can coexist with the current playable menu controller. Add
> `using ShadowBeat.UI;` when referencing them from another script. Level scene
> names, unlock progress, best scores, and Lux shape preferences are already
> aligned with this project's existing `PlayerPrefs` keys.

---

## FILES INCLUDED

| Script | Purpose |
|---|---|
| `MainMenuController.cs` | Animated main menu, logo intro, parallax, panel routing |
| `LevelSelectController.cs` | Scrollable level cards, scroll-snap, shape selector |
| `LevelCard.cs` | Individual card: thumbnail, stars, lock overlay, glow |
| `GameHUDController.cs` | In-game score roll, crystals, combo bar, pause/win/lose |
| `SettingsPanelController.cs` | Volume sliders, toggle switches, language picker |
| `ToggleSwitch.cs` | iOS-style animated on/off toggle |
| `NeonButton.cs` | Hover glow, ripple click, spring press — drop-in button |
| `JuiceManager.cs` | Screen shake, hit flash, freeze frame, particle pools |
| `SceneTransitionManager.cs` | Fade, slide, radial wipe, neon flash scene loads |

---

## SCENE SETUP

### 1. MainMenu Scene

**Canvas (Screen Space – Overlay, Sort Order 0)**

```
Canvas
├── BackgroundRoot
│   ├── BGLayer_Far      ← RawImage, parallax speed 10  (parallaxLayers[0])
│   ├── BGLayer_Mid      ← RawImage, parallax speed 22  (parallaxLayers[1])
│   └── BGLayer_Near     ← RawImage, parallax speed 40  (parallaxLayers[2])
├── StarParticles        ← ParticleSystem (continuous, tiny white dots)
├── GlowParticles        ← ParticleSystem (slow drifting orbs)
│
├── LogoRoot             ← RectTransform, center, Y=0
│   ├── TitleText        ← TMP "SHADOW BEAT"  font: Montserrat ExtraBold, size 72
│   └── SubtitleText     ← TMP "Fragmentos de Luz"  font: Montserrat Light, size 28
│
├── MainPanel            ← CanvasGroup
│   ├── PlayButton       ← NeonButton, accent #4DFFC8
│   ├── SettingsButton   ← NeonButton, accent #4DC8FF
│   ├── CreditsButton    ← NeonButton, accent #C84DFF
│   └── QuitButton       ← NeonButton, accent #FF4D4D
│
├── LevelSelectPanel     ← CanvasGroup (starts inactive)
│   └── [LevelSelectController here]
│
├── SettingsPanel        ← CanvasGroup (starts inactive)
│   └── [SettingsPanelController here]
│
├── CreditsPanel         ← CanvasGroup (starts inactive)
│
└── FadeOverlay          ← CanvasGroup > Image (black, covers full screen)
```

**MainMenuController Inspector wiring:**
- mainPanel → MainPanel CanvasGroup
- levelSelectPanel → LevelSelectPanel CanvasGroup
- settingsPanel → SettingsPanel CanvasGroup
- logoRect → LogoRoot RectTransform
- logoGroup → LogoRoot CanvasGroup
- titleText / subtitleText → the TMP components
- playButton / settingsButton / creditsButton / quitButton → NeonButton components
- parallaxLayers[0..2] → BGLayer_Far/Mid/Near RectTransforms
- parallaxSpeeds → [10, 22, 40]
- fadeOverlay → FadeOverlay CanvasGroup
- introDuration → 1.8 | fadeDuration → 0.45

---

### 2. LevelCard Prefab (280 × 360)

```
LevelCard (RectTransform 280×360)  ← LevelCard.cs here
├── Background        Image   cornerRadius ~20  color #0D0F1E
├── Thumbnail         Image   (assigned per level)
├── GradientOverlay   Image   bottom gradient #00000000 → #000000CC
├── AccentLine        Image   3px top strip, color set by LevelData.accentColor
├── SelectionRing     Image   outline sprite, hidden by default
├── GlowPulse         Image   soft radial gradient, alpha driven by script
└── InfoArea
    ├── WorldNameLabel  TMP  "MUNDO 1"  size 14  color #FFFFFF88  tracking 200
    ├── LevelNameLabel  TMP  "Bosque Neon"  size 22  Bold  color white
    ├── StarsContainer  HorizontalLayoutGroup spacing 4
    │   ├── Star_0  Image
    │   ├── Star_1  Image
    │   └── Star_2  Image
    ├── BestScoreLabel  TMP  size 14  color #FFFFFF66
    └── LockOverlay
        ├── LockDim   Image  #00000088 fill
        └── LockIcon  Image  padlock sprite, center
```

**LevelCard.cs Inspector wiring:**
- Assign each Image/TMP field by name as above
- starFilled → star sprite (filled)
- starEmpty  → star sprite (empty/outline)
- hoverScaleTarget → 1.06
- hoverSpeed → 8
- pressScale → 0.95

---

### 3. In-Game HUD

```
HUD Canvas (Sort Order 10)
├── TopBar
│   ├── ScoreLabel         TMP right-aligned, size 38 Bold
│   ├── ScorePopup         TMP (floating "+100" label, usually alpha 0)
│   ├── CrystalRow
│   │   ├── CrystalIcon    Image (crystal sprite)
│   │   └── CrystalLabel   TMP, size 28 Bold
│   ├── ComboGroup         CanvasGroup (hidden when combo < 2)
│   │   ├── ComboLabel     TMP "x5 COMBO!"
│   │   └── ComboBarFill   Image fillAmount
│   └── PauseButton        NeonButton (⏸ icon)
├── EnergyBarRoot
│   ├── EnergyFill         Image Filled/Horizontal
│   └── EnergyGlow         Image (soft glow behind bar)
├── PausePanel             CanvasGroup (hidden at start)
│   ├── ResumeButton
│   ├── RestartButton
│   ├── MainMenuButton
│   ├── MusicSlider
│   └── SFXSlider
├── GameOverPanel          CanvasGroup (hidden at start)
│   ├── FinalScoreLabel
│   ├── BestScoreLabel
│   ├── StarImages[0..2]
│   ├── RetryButton
│   └── MenuButton
└── WinPanel               CanvasGroup (hidden at start)
    ├── WinScoreLabel
    ├── WinBestLabel
    ├── StarImages[0..2]
    ├── ConfettiParticles
    ├── NextButton
    └── MenuButton
```

**Calling GameHUDController from your GameManager:**
```csharp
var hud = FindObjectOfType<GameHUDController>();
hud.AddScore(100, crystal.transform.position);
hud.AddCrystal();
hud.SetEnergy(player.energyNormalized);
hud.ShowGameOver(totalScore, starsEarned);
hud.ShowWin(totalScore, starsEarned);
```

---

### 4. JuiceManager (Global)

```
_Managers (DontDestroyOnLoad)
└── JuiceManager.cs
    └── HitFlashOverlay  ← Image full-screen, alpha 0, raycast off
```

**Usage from any script:**
```csharp
JuiceManager.Instance.Shake(0.6f);
JuiceManager.Instance.HitFlash(Color.red);
JuiceManager.Instance.FreezeFrame(0.05f);
JuiceManager.Instance.CrystalCollectEffect(pos, Color.cyan);
JuiceManager.Instance.SpawnDeathBurst(player.transform.position);
JuiceManager.Instance.ComboEffect(comboCount);
```

---

### 5. SceneTransitionManager (Global)

```
_Managers (DontDestroyOnLoad)
└── SceneTransitionManager.cs
    ├── WipeGroup      CanvasGroup → WipePanel Image (black)
    ├── RadialPanel    Image (Filled, Radial360, Sort Order 998)
    └── FlashGroup     CanvasGroup → FlashPanel Image (white)
```

**Usage:**
```csharp
// From MainMenuController:
SceneTransitionManager.Instance.LoadScene("Level_01", TransitionType.RadialWipe);

// From GameHUDController restart:
SceneTransitionManager.Instance.ReloadCurrent(TransitionType.NeonFlash);

// After win:
SceneTransitionManager.Instance.LoadScene(nextBuildIndex, TransitionType.SlideLeft);
```

---

## RECOMMENDED FONTS

Download free from Google Fonts:
- **Montserrat ExtraBold** – title, level names, score
- **Montserrat Light** – subtitles, world names
- **Orbitron Bold** – score counter, combo
- **Inter Regular** – body text, settings labels

Import as TextMeshPro Font Assets via `Window → TextMeshPro → Font Asset Creator`.

---

## RECOMMENDED COLORS (Neon Dark Theme)

```
Background deep:   #080B14
Background mid:    #0D1020
Accent cyan:       #4DFFC8   (main CTA, play button)
Accent blue:       #4DC8FF   (settings, info)
Accent magenta:    #FF4DCC   (special events, portals)
Accent yellow:     #FFD84D   (stars, gold)
Accent red:        #FF4D4D   (danger, game over)
Text primary:      #FFFFFF
Text secondary:    #FFFFFF88
Panel bg:          #0D0F1EEB  (93% opacity)
Border:            #FFFFFF18
```

---

## PARTICLE SYSTEMS

### Star Background (MainMenu)
- Shape: Box (cover full screen)
- Start Speed: 0 | Start Size: 1–4px
- Rate over Time: 80
- Lifetime: 4s | Renderer: Billboard

### Crystal Burst (On collect)
- Burst: 12 particles | Shape: Sphere radius 0.1
- Start Speed: 2–5 | Start Size: 0.05–0.15
- Lifetime: 0.6s | Color over Lifetime: crystal color → transparent
- Gravity Modifier: -0.3

### Win Confetti
- Burst: 60 particles | Shape: Cone angle 60
- Start Speed: 4–9 | Start Size: 0.1–0.3
- Color: random from gradient (cyan, magenta, yellow, white)
- Gravity: 1.2 | Lifetime: 2.5s

---

## QUICK START CHECKLIST

- [ ] Copy all 9 .cs files into `Assets/Scripts/UI/`
- [ ] Install TextMeshPro (Window → Package Manager → TextMeshPro)
- [ ] Import Google Fonts and create TMP Font Assets
- [ ] Set up MainMenu scene with Canvas hierarchy above
- [ ] Create LevelCard prefab and assign to LevelSelectController
- [ ] Add _Managers GameObject with JuiceManager + SceneTransitionManager
- [ ] Assign all Inspector fields per this guide
- [ ] Add your level scenes to File → Build Settings in order
- [ ] Set PlayerPrefs key "UL_00" = 1 to unlock first level by default
- [ ] Test with Play → check intro animation → navigate to level select → play level → win/lose

---

*Shadow Beat UI Kit — designed for App Store quality feel.*
*All scripts use only built-in Unity APIs + TextMeshPro. No external packages required.*
