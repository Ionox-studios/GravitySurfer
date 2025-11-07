# Wave Surfing Road - Setup Guide

## System Overview

This system creates CPU-efficient wave-based roads for an F-Zero style surfing game using:

1. **SplineRoad** - Defines the road path using control points
2. **WaveGenerator** - Creates traveling waves (CPU-friendly sine waves)
3. **WaveRoadMesh** - Generates and deforms the road mesh
4. **BarycentricAlignment** - Aligns vehicle to deformed surfaces
5. **WaveSurfingController** - Adds surfing boost mechanics

---

## Setup Instructions

### Step 1: Create the Road

1. Create an empty GameObject: `GameObject > Create Empty`
2. Name it "WaveRoad"
3. Add component: `SplineRoad`
4. Add component: `WaveGenerator`
5. Add component: `WaveRoadMesh`
6. Add component: `MeshRenderer`

### Step 2: Set Up Control Points

1. Create several empty GameObjects as children of your scene (not the WaveRoad)
2. Name them "ControlPoint1", "ControlPoint2", etc.
3. Position them to define your road path (these are like the track markers)
4. In the `SplineRoad` component:
   - Set the size of "Control Points" to match your number of points
   - Drag each ControlPoint GameObject into the array
   - Adjust "Road Width" (default 10)
   - Adjust "Width Segments" (default 10) - more = smoother

### Step 3: Configure Wave Generator

In the `WaveGenerator` component:
- **Wave Height**: 1-3 for gentle waves, 5+ for extreme
- **Wave Length**: 10-20 for good surfing
- **Wave Speed**: 5-10 (speed waves travel)
- **Wave Direction**: (0, 1) = forward, (1, 0) = sideways, etc.
- Enable "Use Multiple Waves" for more organic look

### Step 4: Configure Road Mesh

In the `WaveRoadMesh` component:
- Drag the `SplineRoad` component into "Spline Road" field
- Drag the `WaveGenerator` component into "Wave Generator" field
- Set "Length Segments" to 100-200 (more = smoother, but slower)
- Enable "Update Mesh Every Frame"

### Step 5: Add Material

1. Create a new Material in your Assets
2. Assign it to the WaveRoad's MeshRenderer
3. Optionally add a texture that tiles

### Step 6: Set Layer for Collision

1. Create a new Layer called "WaveRoad" (Edit > Project Settings > Tags and Layers)
2. Assign the WaveRoad GameObject to this layer
3. Remember this layer for the vehicle setup

---

## Vehicle Setup

### Step 7: Update Your Hover Vehicle

On your hover vehicle GameObject:

1. **Add BarycentricAlignment component**:
   - Set "Use Alignment" to FALSE initially (test standard hover first)
   - Set "Surface Layer" to include your WaveRoad layer
   - Set "Alignment Strength" to 5-10
   - Set "Max Ray Distance" to hover height * 2

2. **Add WaveSurfingController component**:
   - Set "Wave Boost Multiplier" to 1.5-2.0
   - Set "Wave Layer" to your WaveRoad layer
   - Enable "Show Surfing Info" for debugging

3. **Update HoverVehicleController**:
   - In "Ground Layer", include your WaveRoad layer
   - Adjust hover height as needed

### Step 8: Test Standard Mode First

1. Enter Play Mode
2. Vehicle should hover normally over the wavy surface
3. Waves should be moving beneath you
4. Adjust wave parameters in real-time to get the feel you want

### Step 9: Enable Barycentric Alignment

1. Stop Play Mode
2. On `BarycentricAlignment`, enable "Use Alignment"
3. Enter Play Mode
4. Vehicle should now tilt and align to the wave surface
5. Try surfing down waves to get speed boost!

---

## Tips & Tweaks

### Performance
- Reduce "Length Segments" in WaveRoadMesh if FPS drops
- Reduce "Width Segments" in SplineRoad for simpler mesh
- Disable "Update Mesh Every Frame" for static waves

### Wave Direction Examples
- **(0, 1)** - Waves travel along the road (forward)
- **(1, 0)** - Waves travel across the road (sideways)
- **(0.5, 1)** - Diagonal waves
- **(-0.3, 1)** - Slight angle against road direction

### Surfing Feel
- Higher waves + faster speed = more extreme surfing
- Multiple waves creates more complex patterns
- Adjust "Min/Max Surfing Angle" to control boost zones

### Common Issues
- **Vehicle falls through**: Increase hover height or max ray distance
- **Jittery vehicle**: Decrease alignment strength
- **No boost when surfing**: Check wave direction matches road direction
- **Mesh not updating**: Ensure "Update Mesh Every Frame" is enabled

---

## Advanced: Multi-Directional Waves

For waves that hit the road from different angles:

```csharp
// In WaveGenerator, add these to Additional Waves:
Wave 1: amplitude=1, wavelength=15, speed=5, direction=(1, 0.5)
Wave 2: amplitude=0.7, wavelength=10, speed=7, direction=(-0.5, 1)
Wave 3: amplitude=0.5, wavelength=20, speed=3, direction=(0, 1)
```

This creates crossing wave patterns for interesting surfing dynamics!

---

## Next Steps

1. Create multiple control points to make your track
2. Experiment with wave parameters
3. Add visual effects (particle trails when surfing?)
4. Create UI to show surf boost amount
5. Add speed boosts/penalties based on alignment
6. Test on WebGL build!

The system is designed to be WebGL-friendly with CPU-based calculations. No shaders or GPU compute needed!
