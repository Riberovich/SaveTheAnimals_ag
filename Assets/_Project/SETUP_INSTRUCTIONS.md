# M1/A1 Setup Instructions

## Quick Setup Guide for Unity

Follow these steps to test the balloon pop functionality:

### 1. Open the Gameplay Scene
- In Unity, navigate to `Assets/_Project/Scenes/`
- Double-click `Gameplay.unity` to open it

### 2. Create AudioManager GameObject
1. In the Hierarchy, right-click → Create Empty
2. Rename it to "AudioManager"
3. With AudioManager selected, click "Add Component" in the Inspector
4. Search for "AudioManager" and add the script
5. The AudioManager will show a warning about missing audio clips (this is okay for now)

**Optional:** Add pop sound effects
- If you have pop sound effect files (.wav, .mp3, etc.), drag them into `Assets/_Project/Audio/`
- Select the AudioManager in the Hierarchy
- In the Inspector, expand "Pop Sound Effects"
- Change the size to match the number of clips you have (recommended: 8+)
- Drag your audio clips into the slots

### 3. Create BalloonSpawner GameObject
1. In the Hierarchy, right-click → Create Empty
2. Rename it to "BalloonSpawner"
3. With BalloonSpawner selected, click "Add Component"
4. Search for "BalloonSpawner" and add the script

**Optional:** Customize the balloon
- In the Inspector, you can adjust:
  - Balloon Size (default: 2)
  - Balloon Color (default: Red)
  - Spawn Position (default: 0, 0, 0)
- If you have a balloon sprite, drag it into the "Balloon Sprite" field

### 4. Save the Scene
- Press Ctrl+S (Windows) or Cmd+S (Mac) to save the scene

### 5. Test in Play Mode
- Press the Play button (or F5)
- A balloon should appear in the center of the screen
- Click/tap the balloon to see the pop animation
- The balloon should scale up (punch), then shrink and disappear
- If you added audio clips, you should hear a randomized pop sound

## What Should Happen
✅ Balloon appears when you press Play
✅ Clicking/tapping the balloon triggers the pop animation
✅ Balloon scales up quickly (punch effect)
✅ Balloon shrinks and fades out
✅ Balloon is destroyed after the animation
✅ Random pop sound plays (if audio clips are assigned)

## Troubleshooting
See the main README for common issues and solutions.
