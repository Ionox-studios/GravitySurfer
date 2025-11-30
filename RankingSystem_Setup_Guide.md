# Race Ranking System Setup Guide

## Overview
This ranking system tracks all racers (player + enemies) in real-time based on their lap number and track progress. It displays the player's current position in the race.

## Components Created

### 1. **IRacer.cs** (Interface)
- Defines the contract for all racers
- Methods: GetCurrentLap(), GetCurrentNodeIndex(), GetTotalNodes(), GetTransform(), IsAlive(), GetRacerName()

### 2. **RaceRankingSystem.cs** (Ranking Manager)
- Singleton that manages all racers
- Calculates positions based on lap number and node progress
- Updates rankings periodically (default: 0.5 seconds)
- Handles dead racers automatically

### 3. **EnemyBehavior.cs** (Updated)
- Now implements IRacer interface
- Tracks lap count and current waypoint index
- Automatically excluded from rankings when dead

### 4. **PlayerRacer.cs** (Player Component)
- Implements IRacer for the player
- Gets lap data from GameController
- Tracks progress using road nodes from RespawnManager

### 5. **RaceRankingUI.cs** (UI Display)
- Displays player's rank (e.g., "1st / 4")
- Optional detailed rankings list
- Updates in real-time

### 6. **GameController.cs** (Updated)
- Added GetCurrentLap() and GetTotalLaps() public methods
- Allows PlayerRacer to access lap information

## Setup Instructions

### Step 1: Add PlayerRacer to Player
1. Select your Player GameObject in the hierarchy
2. Add Component → **PlayerRacer**
3. Configure:
   - **Racer Name**: "Player" (or your preferred name)
   - **Respawn Manager**: Auto-assigned, or drag your RespawnManager component
   - **Road Nodes**: Add the same road nodes used by RespawnManager (these track progress along the track)

### Step 2: Configure Enemies
Each enemy should already have **EnemyBehavior** attached. Update it:
1. Set **Racer Name**: Give each enemy a unique name ("Enemy 1", "Enemy 2", etc.)
2. Ensure **Waypoints** array is populated (used for tracking progress)
3. The enemy will automatically start at lap 0 and increment as they complete laps

### Step 3: Create RaceRankingSystem
1. Create an empty GameObject in your scene (e.g., "RaceRankingSystem")
2. Add Component → **RaceRankingSystem**
3. Configure:
   - **Racer Objects**: Add all racer GameObjects (Player + all Enemies)
   - **Update Interval**: 0.5 (how often to recalculate, in seconds)
   - **Show Debug Info**: Check to see rank numbers above racers in Scene view

### Step 4: Setup UI Display
1. Create a new TextMeshProUGUI element in your Canvas (UI → Text - TextMeshPro)
2. Position and style it (e.g., top-right corner)
3. Add Component → **RaceRankingUI** to the text GameObject
4. Configure:
   - **Ranking Text**: Drag the TextMeshProUGUI component
   - **Player Object**: Drag your Player GameObject
   - **Show Detailed Rankings**: 
     - **False**: Shows just player rank (e.g., "2nd / 4")
     - **True**: Shows full ranking list
   - **Update Interval**: 0.1 (UI updates faster than rankings)

## How It Works

### Ranking Calculation
Racers are ranked based on:
1. **Lap number** (higher lap = better position)
2. **Current node/waypoint index** (higher index = further along track)
3. **Distance to next node** (closer to next node = slightly ahead)

### Example Scenarios
- Player on Lap 2, Node 10 > Enemy on Lap 2, Node 5 → Player is 1st
- Enemy on Lap 3, Node 1 > Player on Lap 2, Node 50 → Enemy is 1st (higher lap)
- Dead enemies are automatically removed from rankings

### Lap Tracking
- **Player**: Lap tracked by GameController (updated via LapCounter trigger)
- **Enemies**: Lap tracked internally in EnemyBehavior (when they complete waypoint loop)

### Node Tracking
- **Player**: Continuously finds closest road node from RespawnManager's node list
- **Enemies**: Uses their current waypoint index from pathfinding

## Testing

1. **Enable Debug Mode**: Check "Show Debug Info" on RaceRankingSystem
2. **Run the game**: You'll see rank numbers above each racer in Scene view
3. **Check Console**: Rankings are printed when debug is enabled
4. **Watch UI**: Player's position should update as they race

## Troubleshooting

### "No racer objects assigned!"
- Make sure you've added Player and Enemy GameObjects to the RaceRankingSystem's Racer Objects list

### "GameObject does not implement IRacer interface!"
- Player needs PlayerRacer component
- Enemies need EnemyBehavior component (should already have it)

### Rankings not updating
- Check that RaceRankingSystem's Update Interval is reasonable (0.5 is good)
- Verify racers have valid lap and node data

### Player rank shows "-- / --"
- Ensure PlayerRacer is attached to player
- Check that GameController.Instance is not null
- Verify road nodes are assigned in PlayerRacer

### Dead enemies still showing
- Check that enemy's currentHealth reaches 0 when dying
- Verify EnemyBehavior.IsAlive() returns false when dead

## Advanced Customization

### Custom Rank Display Format
Edit `RaceRankingUI.ShowPlayerRanking()` to change how ranks are displayed:
```csharp
// Example: Show as "Position: 1st out of 4"
rankingText.text = $"Position: {GetRankSuffix(playerRank)} out of {totalRacers}";
```

### More Precise Distance Tracking
Currently, distance to next node is a placeholder. To implement:
1. Expose waypoints in IRacer interface
2. Calculate actual distance in RaceRankingSystem.GetDistanceToNextNode()

### Lap Increment for Enemies
Enemies currently increment lap when waypoint loops. To use lap triggers like the player:
1. Make enemies trigger the same LapCounter
2. Add lap tracking to EnemyBehavior similar to player

## Notes
- Rankings update every 0.5 seconds by default (configurable)
- Dead racers are automatically excluded
- System is fully modular - easy to add/remove racers at runtime
- Compatible with existing lap and respawn systems
