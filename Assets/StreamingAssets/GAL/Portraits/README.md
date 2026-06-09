# Portrait Assets

Put character standing portraits here. The runtime checks these paths in order:

- `Portraits/<character>/<expression>.png`
- `Portraits/<character>/<expression>.jpg`
- `Portraits/<character>_<expression>.png`
- explicit `portrait_path` from the text table or command

Current placeholder character:

- `test`: put files such as `Portraits/test/neutral.png`, `happy.png`, `sad.png`, `angry.png`, `surprised.png`.

If no image exists, the runtime shows a translucent placeholder block with the character and expression name so layout can still be tested.
