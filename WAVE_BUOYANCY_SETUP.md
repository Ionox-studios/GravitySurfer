# Wave Buoyancy System Setup

## Overview
The `WaveBuoyancy` component provides a physics-based buoyancy model that makes the player bounce along wave surfaces. When the player is inside or below a wave, spring forces push them up so their feet align with the wave surface.

## How It Works

### Detection System
- Uses raycasts from the **bottom of the player's collider** (not center)
- Casts one ray upward and one downward to detect wave surfaces
- Only responds to GameObjects tagged with `"wave"`

### Physics Behavior
1. **Inside/Below Wave**: Applies upward spring force proportional to depth
   - Deeper submersion = stronger upward force
   - Includes damping to prevent bouncing
   
2. **Above Wave**: Lets the existing hover system handle positioning
   - No buoyancy force applied
   - `SimpleSurfaceAligner` hover takes over

3. **No Wave**: No buoyancy forces (neutral state)

## Setup Instructions

### 1. Add the Component
Attach `WaveBuoyancy.cs` to your player GameObject (the same object that has `SimpleSurfaceAligner` and `SimpleInputHandler`).

### 2. Configure Settings

#### Buoyancy Settings
- **Buoyancy Force** (100): Spring force strength pushing player up
  - Higher = more aggressive bounce
  - Lower = gentler floating
  
- **Buoyancy Damping** (10): Prevents oscillation/bouncing
  - Higher = more stable, less bouncy
  - Lower = more springy
  
- **Raycast Distance** (20): How far to check for waves above/below
  - Should be larger than expected wave heights
  
- **Show Debug Rays** (true): Visualize raycasts in Scene view
  - Cyan = detected wave above (buoyancy active)
  - Green = detected wave below (hover mode)
  - Red = no wave detected

#### Collider Settings
- **Collider Bottom Offset**: Auto-detected from player's collider
  - Manual override if needed
  - Distance from center to bottom of player

#### References
- **Surface Aligner**: Auto-finds `SimpleSurfaceAligner` component
  - Optional: Can work independently

### 3. Ensure Wave Tagging
The `WaveRoadMesh` script now automatically tags itself as `"wave"` on Start.

**Manual Setup (if needed):**
1. Select your wave GameObject in the hierarchy
2. In Inspector, set Tag dropdown to `"wave"`
3. If "wave" tag doesn't exist:
   - Tags & Layers → Add Tag
   - Create new tag called `wave`

### 4. Tuning Tips

**For Smooth Floating:**
- Increase Buoyancy Damping (15-20)
- Moderate Buoyancy Force (50-100)

**For Bouncy Surfing:**
- Decrease Buoyancy Damping (5-8)
- Higher Buoyancy Force (150-200)

**For Heavy Feel:**
- Lower Buoyancy Force (30-50)
- Higher Damping (15-25)

## Integration with Existing Systems

### SimpleSurfaceAligner
- Works alongside the existing hover system
- Buoyancy takes priority when inside waves
- Hover system handles above-wave positioning

### Movement
- Doesn't interfere with `SimpleInputHandler` controls
- Player can still move/turn while floating
- Jump still works as normal

## Debug Visualization

When `Show Debug Rays` is enabled, you'll see:
- **Cyan Ray**: Pointing to wave above (buoyancy active)
- **Blue Ray**: Wave surface normal at hit point
- **Magenta Ray**: Current buoyancy force direction/magnitude
- **Green Ray**: Wave detected below (above wave mode)
- **Red Rays**: No wave detected

## Common Issues

**Player sinks through wave:**
- Increase Buoyancy Force
- Check that wave has `MeshCollider` component
- Verify wave is tagged correctly

**Player bounces too much:**
- Increase Buoyancy Damping
- Decrease Buoyancy Force slightly

**No buoyancy effect:**
- Check that wave GameObject has tag `"wave"`
- Verify Raycast Distance is large enough
- Ensure wave has a collider component
- Check Layer Mask settings (default = Everything)

**Player gets stuck:**
- Reduce Buoyancy Damping
- Increase Buoyancy Force
- Check for mesh collider issues on wave

## Technical Notes

- Uses `FixedUpdate` for physics calculations
- Forces applied via `Rigidbody.AddForce` with `ForceMode.Acceleration`
- Collider bottom offset auto-detected from bounds
- Compatible with frozen rotation constraints
- Works with dynamic wave deformation

## Future Enhancements

Possible additions:
- Lateral water resistance/drag
- Wave momentum transfer (push player in wave direction)
- Splash effects when entering/exiting waves
- Different buoyancy for different wave materials
- Angular damping when in waves
