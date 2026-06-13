# GAL Template Editing Guide

This Unity GAL template is organized as JSON flow data, separated language CSV files, and replaceable art folders. Use the CSV files for planner-facing writing work; use `gal_story.json` for node flow, jumps, commands, backgrounds, hotspots, language table paths, and portrait library entries.

## Main Files

- Flow config: `Assets/StreamingAssets/GAL/gal_story.json`
- Chinese text: `Assets/StreamingAssets/GAL/Text/story_text_zh-CN.csv`
- English text: `Assets/StreamingAssets/GAL/Text/story_text_en.csv`
- Japanese text: `Assets/StreamingAssets/GAL/Text/story_text_ja.csv`
- Old wide-table reference: `Assets/StreamingAssets/GAL/Text/story_text_placeholder.csv`
- Backgrounds: `Assets/StreamingAssets/GAL/Backgrounds/`
- Portraits: `Assets/StreamingAssets/GAL/Portraits/`
- Runtime script: `Assets/_Project/Scripts/GalTemplate/GalTemplateRuntime.cs`
- Portrait controller: `Assets/_Project/Scripts/GalTemplate/GalPortraitController.cs`

## Language Tables

Each language has its own CSV. `gal_story.json` uses `languages[].textTable` to choose the active file:

```json
{
  "id": "en",
  "displayName": "English",
  "textTable": "Text/story_text_en.csv"
}
```

Single-language CSV columns:

```csv
category,key,description,speaker,text,portrait_slot,portrait_character,portrait_expression,portrait_facing,portrait_animation,portrait_path,note
```

Planner-facing columns:

- `speaker`: displayed speaker name.
- `text`: dialogue, choice, hotspot, or UI text. Use `\n` for a manual line break.
- `portrait_slot`: `left`, `center`, or `right`.
- `portrait_character`: character id, for example `test`.
- `portrait_expression`: expression name, for example `neutral`, `happy`, `sad`, `angry`, `surprised`.
- `portrait_facing`: `auto`, `left`, or `right`.
- `portrait_animation`: `none`, `fade`, `shake`, `bounce`, or `pop`.
- `portrait_path`: optional explicit image path.
- `note`: free planner notes; ignored by runtime.

Common keys:

- `game.title`: main menu title.
- `node.<nodeId>`: story node text and optional portrait cue.
- `choice.<nodeId>.<01>`: choice text by node and order.
- `choice.<choiceId>`: choice text by custom choice id.
- `explore.<pointId>`: hotspot label.
- `ui.*`: interface text.

The runtime hot-reloads `gal_story.json` and the active CSV about once per second in Play mode. The Settings page also has a reload button.

## Portrait Assets

Put portrait images here:

```text
Assets/StreamingAssets/GAL/Portraits/<character>/<expression>.png
```

Examples:

```text
Assets/StreamingAssets/GAL/Portraits/test/neutral.png
Assets/StreamingAssets/GAL/Portraits/test/happy.png
```

Supported formats: `.png`, `.jpg`, `.jpeg`. If no image exists, the runtime shows a translucent placeholder block with the character and expression name, so layout can still be tested before final art arrives.

Portrait library entries live in `gal_story.json`:

```json
{
  "id": "test",
  "displayName": "test character",
  "folder": "Portraits/test",
  "defaultExpression": "neutral",
  "width": 520,
  "height": 900,
  "scale": 1
}
```

## Portrait Commands

Portraits can be controlled from a node or choice command:

```json
{
  "command": "show_portrait",
  "slot": "left",
  "character": "test",
  "expression": "happy",
  "facing": "auto",
  "animation": "pop"
}
```

Supported commands:

- `show_portrait` / `portrait`: show or refresh one slot.
- `hide_portrait`: hide one slot.
- `hide_portraits` / `clear_portraits`: hide all slots.
- `animate_portrait` / `portrait_animation`: play animation on one slot.

## Portrait Debug Panel

Open it from the in-game HUD button `Portrait`, from Settings `Portrait Debug`, or with keyboard `P`. It can cycle slot, character, expression, facing, and animation, then show, refresh, animate, hide one slot, or hide all portraits.

Secondary overlay behavior is consistent: Back returns to Settings when opened from Settings; Exit closes overlays and returns to the original title/game screen.

## Recommended Flow

```text
opening lines
-> show_explore
-> player picks a place
-> short location dialogue, optionally with portrait cues
-> show_explore
-> player keeps exploring
```

## Explore Hotspots

`explorePoints` in `gal_story.json` define map-like interaction:

- `scene`: background where the button appears.
- `nodeId`: story node to play; leave empty for map-only movement.
- `background`: target background after clicking.
- `x` / `y`: normalized position from 0 to 1.
- `width` / `height`: button size on a 1920x1080 reference canvas.
- `requiredFlag`: optional flag or item gate.

Hotspots can run commands before moving or playing dialogue. For a black-screen transition into a runtime FBX scene:

```json
{
  "id": "front_door",
  "displayName": "出门",
  "scene": "entry",
  "x": 0.52,
  "y": 0.52,
  "width": 170,
  "height": 46,
  "commands": [
    {
      "command": "enter_fbx_scene",
      "path": "FbxScenes/car",
      "amount": 7
    }
  ]
}
```

FBX scene assets live under `Assets/Resources/FbxScenes/`. The command `path` is the Resources path without file extension. `amount` controls the pixelated mosaic post effect, and Esc exits the FBX scene back to the GAL explore layer.

## Controls

- Left click / Space: continue.
- Right click: hide or show dialogue.
- A: auto mode.
- Ctrl: skip mode.
- H: history.
- P: portrait debug.
- S: save slots.
- L: load slots.
- Ctrl+S: quick save to slot 1.
- Ctrl+L: load latest slot.
- Esc: close panel or return to title.
