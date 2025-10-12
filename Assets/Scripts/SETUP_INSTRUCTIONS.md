# Player Controller Setup Instructions

## Quick Setup Steps

### 1. Add PlayerController Script to Your Player Prefab
- Select your Player prefab in the Project window
- Click "Add Component" in the Inspector
- Search for "PlayerController" and add it

### 2. Add Player Input Component
- With the Player prefab still selected, click "Add Component"
- Search for "Player Input" and add it
- In the Player Input component:
  - Set **Actions** to your `InputSystem_Actions` asset
  - Set **Default Map** to "Player"
  - Set **Behavior** to "Invoke Unity Events"

### 3. Connect Input Events to PlayerController
In the Player Input component, you'll see event sections. Connect them as follows:

**Player > Move**
- Click the "+" button
- Drag your Player GameObject into the object field
- Select `PlayerController > OnMove` from the dropdown

**Player > Look**
- Click the "+" button
- Drag your Player GameObject into the object field
- Select `PlayerController > OnLook` from the dropdown

**Player > Jump**
- Click the "+" button
- Drag your Player GameObject into the object field
- Select `PlayerController > OnJump` from the dropdown

**Player > Sprint**
- Click the "+" button
- Drag your Player GameObject into the object field
- Select `PlayerController > OnSprint` from the dropdown

### 4. Verify Camera Assignment
- The script will auto-detect the camera if it's a child of the player
- If not, manually assign the Camera Transform in the PlayerController component

### 5. Adjust Settings (Optional)
In the PlayerController component, you can tweak:
- **Move Speed**: Base movement speed (default: 6)
- **Sprint Multiplier**: How much faster sprinting is (default: 1.5x)
- **Acceleration/Deceleration**: How quickly you reach max speed
- **Jump Height**: How high you jump (default: 2)
- **Gravity**: Downward force (default: -20)
- **Mouse Sensitivity**: Look speed (default: 2)
- **Max Look Angle**: Vertical look limit (default: 90°)

## Controls
- **WASD / Arrow Keys**: Move
- **Mouse**: Look around
- **Space**: Jump
- **Left Shift**: Sprint
- **Escape**: Unlock cursor (built into Unity)

## Testing
1. Save your prefab
2. Make sure the Player prefab is in your scene
3. Press Play
4. You should be able to move, look around, jump, and sprint!

## Troubleshooting
- **Can't look around**: Make sure cursor is locked (click in Game view)
- **Not moving**: Check that Player Input events are connected
- **Falling through floor**: Add a ground collider to your scene
- **Camera not found error**: Assign the camera transform manually in the Inspector
