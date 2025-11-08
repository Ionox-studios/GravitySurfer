# Simple Surface Aligner - Test Scene Setup Guide

This guide walks you through setting up a test scene to test the `SimpleSurfaceAligner` controller with surface normal alignment.

## What This Does

- **Simple Movement**: WASD/Arrow keys to move and turn
- **Surface Alignment**: As you approach a surface, your object's bottom aligns to the surface's normal
- **Visual Feedback**: Debug rays show you what the raycast is detecting

## Step-by-Step Setup

### 1. Create a New Scene (or use existing)

1. In Unity, go to **File > New Scene** (or use your current scene)
2. Save the scene as `SurfaceAlignmentTest`

### 2. Create the Ground/Terrain

You need surfaces to align to. Here are a few options:

#### Option A: Simple Plane
1. Right-click in Hierarchy > **3D Object > Plane**
2. Name it `Ground`
3. Scale it up if needed (e.g., Scale: `5, 1, 5`)

#### Option B: Terrain with Slopes
1. Right-click in Hierarchy > **3D Object > Terrain**
2. Use the terrain tools to create hills and slopes
3. This will give you varied surface normals to test against

#### Option C: Multiple Angled Surfaces
1. Create several planes at different angles:
   - Right-click in Hierarchy > **3D Object > Plane**
   - Rotate them (e.g., Rotation: `0, 0, 30` for a sloped surface)
   - Position them around the scene
2. This lets you test alignment on different slopes

### 3. Create the Player/Vehicle Object

1. Right-click in Hierarchy > **3D Object > Capsule** (or Cube)
2. Name it `Player` or `Vehicle`
3. Position it above the ground (e.g., Position: `0, 5, 0`)

### 4. Add Required Components to the Player

1. Select the `Player` object
2. Click **Add Component** button in the Inspector
3. Add these components:

   #### a. Rigidbody
   - Click **Add Component** > **Physics > Rigidbody**
   - Settings:
     - Mass: `1` (default)
     - Drag: `0.5` (to slow it down a bit)
     - Angular Drag: `0.5`
     - ✅ Use Gravity: **Checked**
     - ✅ Is Kinematic: **Unchecked**

   #### b. SimpleSurfaceAligner
   - Click **Add Component** > Search for `SimpleSurfaceAligner`
   - Settings (you can adjust these):
     - Move Speed: `10`
     - Turn Speed: `100`
     - Alignment Speed: `5`
     - Raycast Distance: `10`
     - Ground Layer: `Everything` (or create a "Ground" layer)
     - ✅ Show Debug Rays: **Checked** (so you can see what's happening)

   #### c. SimpleInputHandler
   - Click **Add Component** > Search for `SimpleInputHandler`
   - Settings:
     - **Surface Aligner**: Should auto-populate with the `SimpleSurfaceAligner` component (if not, drag it in)
     - **Input Actions**: Drag the `InputSystem_Actions` asset from your Assets folder into this field
       - Located at: `Assets/InputSystem_Actions.inputactions`

### 5. Setup the Camera

1. Select the **Main Camera** in the Hierarchy
2. Position it to see your player and ground (e.g., Position: `0, 10, -10`, Rotation: `30, 0, 0`)
3. **Optional**: Make it follow the player:
   - Drag the `Player` object into the Main Camera's parent in the hierarchy (this makes it a child)
   - Adjust the camera's local position to follow from behind (e.g., Local Position: `0, 5, -10`)

### 6. Create Layers (Optional but Recommended)

1. Go to **Edit > Project Settings > Tags and Layers**
2. Add a new layer called `Ground`
3. Select your ground plane/terrain
4. Set its **Layer** to `Ground` (top of Inspector)
5. Go back to your `Player` object
6. In the `SimpleSurfaceAligner` component, set **Ground Layer** to `Ground`

This ensures the raycast only hits ground objects, not the player itself.

### 7. Test the Scene

1. Click the **Play** button ▶️
2. Use controls:
   - **W/S** or **Up/Down arrows**: Move forward/backward
   - **A/D** or **Left/Right arrows**: Turn left/right
3. Watch the debug rays in the Scene view:
   - **Green ray**: Raycast hit a surface
   - **Blue ray**: Surface normal direction
   - **Red ray**: No surface detected
4. As you approach the ground or slopes, your object should rotate to align with the surface normal

## Troubleshooting

### Object doesn't align to surface
- Check that **Show Debug Rays** is enabled and you see the rays
- Make sure the **Raycast Distance** is long enough to reach the ground
- Verify the **Ground Layer** matches the layer of your ground objects
- Increase **Alignment Speed** for faster rotation

### Object falls through the ground
- Add a **Collider** to your ground (Plane should have one by default)
- Make sure the Player has a **Capsule Collider** or **Box Collider**
- Check that neither object has **Is Trigger** checked

### Object spins wildly
- Reduce **Alignment Speed** (try `2` or `3`)
- Make sure your ground is relatively smooth
- Check that the **Rigidbody** constraints aren't freezing rotation

### Controls don't work
- Verify the `SimpleInputHandler` component is attached
- Check that the `Surface Aligner` field is populated
- **Check that `Input Actions` field has the `InputSystem_Actions` asset assigned**
- Make sure the scene is in Play mode

### Can't see anything
- Check camera position and rotation
- Make sure your Player and Ground are visible in the Scene view
- Adjust lighting (add a **Directional Light** if needed)

## Advanced Testing

### Test Different Surface Angles
1. Create cubes or planes at various angles (15°, 30°, 45°, etc.)
2. Drive the player over them and watch how it aligns

### Test Transitions
1. Create a ramp that goes from flat to steep
2. Watch how smoothly the player rotates during the transition

### Adjust Parameters
- **Alignment Speed**: Higher = faster rotation, lower = smoother/slower
- **Raycast Distance**: How far ahead the raycast looks
- **Move Speed**: How fast the player moves
- **Turn Speed**: How quickly the player turns

## How It Works (Simple Explanation)

1. **Raycast**: Every frame, the script shoots a ray downward from the player
2. **Surface Normal**: If it hits something, it gets the surface's "normal" (perpendicular direction)
3. **Alignment**: The script rotates the player so its "up" direction matches the surface normal
4. **Smoothing**: Uses `Slerp` to smoothly interpolate between current and target rotation

The result: Your object's bottom stays aligned with whatever surface is below it!

## Next Steps

Once this is working, you can:
- Add hover forces (like in `HoverVehicleController`)
- Add more complex terrain
- Experiment with different alignment speeds and raycast distances
- Add visual effects or particle systems
- Create obstacles and challenges

Enjoy testing! 🚀
