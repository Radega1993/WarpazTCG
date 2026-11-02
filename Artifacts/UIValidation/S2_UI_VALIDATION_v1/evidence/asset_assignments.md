# Asset assignments (Battle)

| Element | Assignment | Result |
| --- | --- | --- |
| Background Image.sprite | warpaz_battleboard_background | Present |
| Background.raycastTarget | false | PASS |
| BattleBoardArtBinder | Present on BattleCompositionRoot | PASS |
| CharacterSlotView Frame | warpaz_character_slot_frame (via binder ApplyArt) | Present in prefab/slots |
| Panel 9-slice | warpaz_panel_frame_9slice on *PanelFrame children | Present |
| PA / Resolution icons | PlayerAreaView EnsureIcon | Present (Priority/Rival) |
| MatchDebugHud | Not in Battle scene | N/A (documented BLOCKED for capture 14) |
