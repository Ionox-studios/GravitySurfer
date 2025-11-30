# Jump Cooldown UI Setup Guide

## Overview
The Jump Cooldown UI displays a visual indicator showing when the jump ability is ready. It consists of:
- A "JUMP" text label
- A fill image that gradually fills up as the cooldown recharges
- Color transitions from red (cooling down) to green (ready)

## Scripts Created
1. **JumpCooldownUI.cs** - Controls the jump cooldown UI display
2. **VehicleController.cs** - Updated with public getters for jump data

## Unity UI Setup Instructions

### Step 1: Create the UI Container
1. In your Canvas, create a new **Empty GameObject**
2. Rename it to `JumpCooldownIndicator`
3. Add a **RectTransform** component if not already present
4. Position it where you want on screen (recommended: bottom-left or bottom-center)
5. Recommended size: Width: 120, Height: 60

### Step 2: Create the Background
1. Right-click `JumpCooldownIndicator` → UI → **Image**
2. Rename it to `Background`
3. Set the color to a dark semi-transparent color (e.g., RGBA: 0, 0, 0, 150)
4. Make it fill the parent container

### Step 3: Create the Fill Image
1. Right-click `JumpCooldownIndicator` → UI → **Image**
2. Rename it to `FillImage`
3. **Important Settings:**
   - Image Type: **Filled**
   - Fill Method: **Horizontal** (or Vertical/Radial based on preference)
   - Fill Origin: **Left** (or Bottom if using Vertical)
   - Fill Amount: 1 (this will be controlled by the script)
4. Set color to red (this is the cooldown color, will change via script)
5. Add padding from edges (recommended: 5-10 pixels margin)

### Step 4: Create the "JUMP" Text
1. Right-click `JumpCooldownIndicator` → UI → **Text - TextMeshPro**
   - If prompted to import TMP Essentials, click "Import TMP Essentials"
2. Rename it to `JumpText`
3. Set the text to: **JUMP**
4. **Text Settings:**
   - Font Size: 24-32 (adjust to fit)
   - Alignment: Center (both horizontal and vertical)
   - Color: White (will change with script)
   - Font Style: Bold (recommended)
5. Make it fill the parent or position over the fill image

### Step 5: Attach the Script
1. Select the `JumpCooldownIndicator` GameObject
2. Click **Add Component**
3. Search for and add `JumpCooldownUI`
4. **Assign References in Inspector:**
   - **Vehicle Controller**: Drag your player's VehicleController here (or leave empty for auto-find)
   - **Fill Image**: Drag the `FillImage` GameObject here
   - **Jump Text**: Drag the `JumpText` GameObject here (optional)
5. **Configure Visual Settings:**
   - **Ready Color**: Green (0, 255, 0) - shown when jump is ready
   - **Cooldown Color**: Red (255, 0, 0) - shown during cooldown
   - **Use Gradient**: ✓ Checked - smoothly transitions colors

## Alternative Fill Layouts

### Vertical Fill (Bottom to Top)
- Fill Method: **Vertical**
- Fill Origin: **Bottom**
- Looks like a charging bar rising upward

### Radial Fill (Circular)
- Fill Method: **Radial 360**
- Fill Origin: **Bottom** or **Top**
- Creates a circular "pie chart" fill effect
- Great for a more modern UI look

### Radial 90 (Quarter Circle)
- Fill Method: **Radial 90**
- Useful for corner indicators

## Customization Options

### Different Color Schemes
In the Inspector under `JumpCooldownUI`:
- **Ready Color**: The color when jump is available
- **Cooldown Color**: The color during cooldown
- Try: Blue → Cyan, Orange → Yellow, Purple → Pink

### Disable Color Gradient
- Uncheck **Use Gradient** for instant color switching at 100%

### Add Icons
- Replace the "JUMP" text with a jump icon sprite
- Create an Image instead of TextMeshPro
- Assign your icon sprite to the Image component

### Add Glow/Pulse Effect
Add an **Outline** or **Shadow** component to the text for emphasis

### Positioning Examples
- **Bottom-Left**: Anchors: Min (0, 0), Max (0, 0), Pivot (0, 0), Pos (20, 20)
- **Bottom-Center**: Anchors: Min (0.5, 0), Max (0.5, 0), Pivot (0.5, 0), Pos (0, 20)
- **Top-Right**: Anchors: Min (1, 1), Max (1, 1), Pivot (1, 1), Pos (-20, -20)

## Testing
1. Enter Play Mode
2. Press the Jump button (default: Space)
3. Watch the indicator:
   - Should turn red and empty when you jump
   - Should gradually fill back up over 1 second (default cooldown)
   - Should turn green when fully charged
4. Try jumping repeatedly to see the cooldown in action

## Troubleshooting

### Fill image doesn't fill
- Check that Image Type is set to **Filled** (not Simple/Sliced)
- Verify Fill Image is assigned in the script component

### Colors don't change
- Make sure Jump Text reference is assigned (optional)
- Check that Ready Color and Cooldown Color are different

### Can't find VehicleController
- Assign it manually in the Inspector
- Or ensure your player GameObject has the VehicleController script attached

### Fill updates slowly
- This is normal - the cooldown is 1 second by default
- Change `jumpCooldown` value in VehicleController to adjust speed

## Next Steps
You can duplicate this setup for other abilities like:
- Boost cooldown indicator
- Attack cooldown indicator
- Health/shield bars

Just modify the script to read different cooldown values!
