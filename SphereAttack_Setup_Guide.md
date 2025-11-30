# Sphere Attack Setup Guide

## Overview
The enemy attack has been changed from a rotating cylinder to a **growing spherical raycast** with a transparent red visual indicator.

## Changes Made

### Code Changes
1. **Removed** cylinder attack object rotation logic
2. **Added** sphere-based Physics.OverlapSphere detection
3. **Added** visual sphere that grows during attack
4. **Added** one-hit-per-attack prevention

### New Inspector Fields
- `Attack Duration` - How long the sphere takes to grow (default: 0.5s)
- `Attack Max Radius` - Maximum size of the attack sphere (default: 5)
- `Attack Sphere Visual` - Optional visual sphere GameObject

### Removed Fields
- `Attack Object` - No longer needed
- `Attack Swing Time` - Replaced by Attack Duration

## Unity Setup Instructions

### Step 1: Create the Visual Sphere
1. **Right-click** in the Hierarchy
2. Select **3D Object > Sphere**
3. **Rename** it to "AttackSphereVisual"
4. **Position** it at (0, 0, 0) initially

### Step 2: Configure the Visual Sphere Material
1. **Create a new Material**:
   - Right-click in Assets folder
   - Create > Material
   - Name it "AttackSphereMaterial"

2. **Set Material Properties**:
   - **Rendering Mode**: Transparent (or Fade)
   - **Color**: Red with low alpha (e.g., RGB: 255, 0, 0, Alpha: 60-100)
   - **Shader**: Standard or URP/Lit (depending on your pipeline)

3. **Apply Material**:
   - Drag "AttackSphereMaterial" onto the sphere
   - The sphere should now be semi-transparent red

### Step 3: Configure the Sphere GameObject
1. **Remove the Sphere Collider**:
   - Select the sphere
   - Remove or disable the Sphere Collider component
   - This is just for visuals, not collision

2. **Set Initial State**:
   - The sphere will be hidden by default
   - It will only appear during attacks

### Step 4: Assign to Enemy
1. **Select your Enemy GameObject** in the Hierarchy
2. **Find the EnemyBehavior component** in the Inspector
3. **Attack Section**:
   - **Attack Interval**: 1 (time between attacks)
   - **Attack Duration**: 0.5 (how long sphere grows)
   - **Attack Max Radius**: 5 (adjust based on your game scale)
   - **Attack Damage**: 10
   - **Attack Push Force**: 10
   - **Attack Sphere Visual**: Drag the "AttackSphereVisual" GameObject here

### Step 5: Parent the Sphere (Optional but Recommended)
1. **Drag** the "AttackSphereVisual" onto the Enemy GameObject
2. This makes it a child of the enemy
3. **Reset** its local position to (0, 0, 0)
4. The script will move it to the enemy's position during attacks

### Step 6: Remove Old Attack Object
1. If you had an old cylinder attack object, you can now **delete it**
2. Or simply don't assign anything to the removed fields

## How It Works

### Attack Behavior
1. Enemy approaches player during capture phase
2. Once in position, attack timer starts
3. When attack triggers:
   - Sphere appears at enemy position
   - Grows from 0 to max radius over the duration
   - Physics.OverlapSphere checks for player each frame
   - If player is within the growing sphere, they take damage ONCE
   - After hit or completion, sphere disappears

### Visual Feedback
- **Transparent Red Sphere** grows outward from enemy
- **Visible Warning** gives player time to react
- **Shrinks back** to invisible when attack ends

## Customization Tips

### Adjust Sphere Appearance
- **More Transparent**: Lower alpha value (30-50) for subtle effect
- **More Opaque**: Higher alpha value (100-150) for obvious warning
- **Different Color**: Change material color for different enemy types
- **Add Emission**: Make it glow for better visibility

### Adjust Attack Feel
- **Faster Attack**: Reduce Attack Duration (0.2-0.3s) for quick strikes
- **Slower Attack**: Increase Attack Duration (1.0s+) for telegraphed attacks
- **Larger Range**: Increase Attack Max Radius
- **More Frequent**: Decrease Attack Interval

### Add Visual Effects (Optional)
1. Add a **Particle System** as child of the sphere
2. Trigger particles when attack starts
3. Add **Sound Effects** in the UpdateAttack method
4. Add **Screen Shake** on successful hit

## Testing

1. **Play the game**
2. **Approach the enemy** to trigger capture
3. **Watch for the red sphere** to appear and grow
4. **Verify**:
   - Sphere grows smoothly
   - Player takes damage when inside sphere
   - Only one hit per attack
   - Sphere disappears after attack

## Troubleshooting

### Sphere doesn't appear
- Check that Attack Sphere Visual is assigned in Inspector
- Check that the sphere's material has transparency enabled
- Check that the sphere isn't disabled in the hierarchy

### Sphere doesn't grow
- Verify Attack Duration > 0
- Verify Attack Max Radius > 0
- Check console for errors

### Player not taking damage
- Ensure player has "Player" tag
- Ensure player has PlayerHealth component
- Check Attack Damage value is > 0
- Verify player is within Attack Max Radius

### Sphere stays visible
- Check that the sphere is properly deactivated after attacks
- Look for errors in the console

## Optional: Multiple Visual Styles

You can create different sphere prefabs for variety:
1. **Warning Sphere**: Yellow, grows slowly
2. **Danger Sphere**: Red, grows quickly  
3. **Power Attack**: Large radius, bright color
4. **Quick Jab**: Small radius, very fast

Just swap the Attack Sphere Visual reference to different prefabs!
