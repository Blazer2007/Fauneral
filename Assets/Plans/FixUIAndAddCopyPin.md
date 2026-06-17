# Fix UI Visibility and Add PIN Copy Feature

## Project Overview
- **Game Title**: Fauneral
- **High-Level Concept**: Multiplayer card game with online/local play.
- **Players**: Multiplayer (LAN/Online)
- **Render Pipeline**: URP

## Implementation Steps

### 1. Fix PlayerSpawner NullReferenceException [DONE]
- **Description**: Add null check for `SceneManager` in `OnEnable` and `OnDisable` of `PlayerSpawner.cs`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### 2. Fix JoinRoom Scene "Blue Screen" [DONE]
- **Description**: In `JoinRoom.unity`, create a `PinGroup` object to contain PIN-related UI elements (InputField, JoinButton, etc.). Update `JoinRoomUI._pinContainer` to reference this group instead of the entire Canvas.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### 3. Implement "Click to Copy PIN" [DONE]
- **Description**: Add `CopyPinToClipboard()` method to `LobbyMenuUI.cs` using `GUIUtility.systemCopyBuffer`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

### 4. Update LobbyMenu Scene for PIN Copying [DONE]
- **Description**: Add a `Button` component to the PIN text in `LobbyMenu.unity` and wire its `onClick` event to `LobbyMenuUI.CopyPinToClipboard()`.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

## Verification & Testing
- **JoinRoom Scene**: Open `JoinRoom` in LAN mode. The scene should NOT be blue; the PIN input should be hidden, but the list should be visible.
- **Lobby PIN**: Enter a lobby. Click the PIN text. Verify that the PIN is copied to the system clipboard (paste it somewhere else).
- **Spawn**: Verify no `NullReferenceException` appears in the console when the game starts.