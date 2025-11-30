# DirectSceneWinTrigger Setup Guide

## Overview
The `DirectSceneWinTrigger` script allows you to create multiple instant win zones that each load different scenes. This is perfect for:
- Creating branching endings (e.g., "Good Ending" vs "Secret Ending")
- Level selection through gameplay (reach Zone A or Zone B)
- Quick transitions to different cutscenes based on player choice

## Quick Setup

### Step 1: Create the Trigger GameObjects
1. In your level scene, create empty GameObjects where you want the win zones
   - Right-click in Hierarchy → Create Empty
   - Name them something descriptive like "WinZone_GoodEnding" and "WinZone_SecretEnding"
   - Position them where you want players to reach

### Step 2: Add Colliders
1. Select each win zone GameObject
2. Add Component → Box Collider (or Sphere Collider)
3. **Important:** Check "Is Trigger" on the collider
4. Adjust the size to cover the area you want

### Step 3: Add the Script
1. Select each win zone GameObject
2. Add Component → DirectSceneWinTrigger
3. Configure the settings:
   - **Target Scene Name**: Type the exact name of the scene to load (e.g., "CutsceneGoodEnding", "Level2", "Credits")
   - **Transition Delay**: How long to wait before loading (0 = instant)
   - **Show Win Panel**: Check if you want the win UI to appear first
   - **Debug Mode**: Leave checked to see log messages

### Step 4: Make Sure Scenes Are in Build Settings
1. Go to File → Build Settings
2. Make sure your target scenes are in the "Scenes In Build" list
3. If not, drag them from the Project window into the list

## Example: Two Different Endings

Let's say you want two different endings for the last level:

### Win Zone 1 - Normal Ending
- GameObject name: `WinZone_NormalEnding`
- Position: At the normal finish line
- DirectSceneWinTrigger settings:
  - Target Scene Name: `CutsceneNormalEnding`
  - Transition Delay: `2.0`
  - Show Win Panel: `✓ Checked`

### Win Zone 2 - Secret Ending  
- GameObject name: `WinZone_SecretPath`
- Position: Hidden area or alternate route
- DirectSceneWinTrigger settings:
  - Target Scene Name: `CutsceneSecretEnding`
  - Transition Delay: `1.0`
  - Show Win Panel: `✓ Checked`

## Using on Any Level

This works on **any level**, not just the last one! You can:
- Set up Level 1 with two paths that lead to different Level 2 variations
- Create a hub level with multiple win zones leading to different levels
- Make a "choose your path" level with multiple endings

## Visual Helpers

In the Scene view, the triggers show as:
- **Cyan wireframes** when properly configured
- **Yellow wireframes** when missing the target scene name

## Troubleshooting

**"Scene not found" error:**
- Make sure the scene name is spelled exactly right (case-sensitive!)
- Check that the scene is added to Build Settings

**Trigger not activating:**
- Make sure the collider has "Is Trigger" checked
- Verify your player has the "Player" tag
- Check Debug Mode is on to see log messages

**Both triggers activating:**
- Each trigger only fires once per level load
- Make sure the zones don't overlap

## Advanced: Custom Win Messages

If you want different messages for each ending:
1. Check "Show Win Panel"
2. Enter text in "Custom Win Message" (future feature - not yet implemented)

## Mixing with Normal Win Triggers

You can use both:
- `WinTrigger` - Uses the normal lap/race completion system
- `DirectSceneWinTrigger` - Instant scene transition

Just use the one that fits your needs for each zone!
