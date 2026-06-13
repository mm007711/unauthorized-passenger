# GAL Story Files

Edit `gal_story.json` for flow, scenes, hotspots, backgrounds, commands, languages, and portrait library entries.
Edit the separated text CSV files for visible writing and per-language portrait cues:

- `Text/story_text_zh-CN.csv`
- `Text/story_text_en.csv`
- `Text/story_text_ja.csv`

## Text CSV

Current single-language columns:

```csv
category,key,description,speaker,text,portrait_slot,portrait_character,portrait_expression,portrait_facing,portrait_animation,portrait_path,note
```

Important keys:

- `game.title`: menu title.
- `node.<nodeId>`: speaker, dialogue text, and optional portrait cue.
- `choice.<nodeId>.<01>`: choice text by node and order.
- `choice.<choiceId>`: choice text by custom choice id.
- `explore.<pointId>`: hotspot label.
- `ui.*`: interface text.

The runtime hot-reloads `gal_story.json` and the active language CSV about once per second in Play mode.

## Portraits

Portrait slots are `left`, `center`, and `right`. Put image files under:

```text
Portraits/<character>/<expression>.png
```

Supported image formats: `.png`, `.jpg`, `.jpeg`. If an image is missing, the runtime shows a translucent placeholder block so layout and animation can still be tested.

Portrait CSV fields:

- `portrait_slot`: `left`, `center`, or `right`.
- `portrait_character`: character id from `gal_story.json` `portraits`.
- `portrait_expression`: expression/difference name.
- `portrait_facing`: `auto`, `left`, or `right`.
- `portrait_animation`: `none`, `fade`, `shake`, `bounce`, or `pop`.
- `portrait_path`: optional explicit image path.

Commands also support portraits:

- `show_portrait` / `portrait`
- `hide_portrait`
- `hide_portraits` / `clear_portraits`
- `animate_portrait` / `portrait_animation`

Use the in-game HUD `Portrait` button or the Settings page `Portrait Debug` button to test slots, facing, expressions, and animations. Keyboard shortcut: `P`.

## Explore Hotspots

`explorePoints` in `gal_story.json` define map-like interaction:

- `scene`: background where the button appears.
- `nodeId`: story node to play; leave empty for map-only movement.
- `background`: target background after clicking.
- `x` / `y`: normalized position from 0 to 1.
- `width` / `height`: button size on a 1920x1080 reference canvas.
- `requiredFlag`: optional flag or item gate.

Clicking a hotspot with a different target background uses a fade transition.

Hotspots can also run commands before normal movement. The current exterior test uses:

```json
{
  "command": "enter_fbx_scene",
  "path": "FbxScenes/car",
  "amount": 7
}
```

- `path`: a Unity `Resources` path without extension. Put FBX prefabs under `Assets/Resources/FbxScenes/`.
- `amount`: pixelation size for the runtime mosaic effect. Higher values look chunkier.
- Esc exits the FBX scene and returns to the GAL explore layer.

## Runtime Controls

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

Reserved folders:

- `Backgrounds_Painted/`: final painted background replacements.
- `Portraits/`: character standing portraits.
- `UISkins/`: future UI art sprites.
