# Soul Reaver Editor

First-pass Windows editor/research tool for the PlayStation release of **Legacy of Kain: Soul Reaver**.

This build can:

- Open the provided `.cue` or `.bin`.
- Browse the raw MODE2/2352 disc as ISO9660.
- Parse `/BIGFILE.DAT` into Soul Reaver internal folders/files.
- Preview hex, ASCII, and extracted strings.
- Export any outer ISO file or internal BIGFILE entry.
- Replace a selected ISO file or internal BIGFILE entry with an exact same-size file in a new patched `.bin` copy.
- Search selected resources for text, hex bytes, and embedded PlayStation signatures.
- Surface candidate TIM palettes/textures, XA/STR audio/video, and Soul Reaver sound chunks.
- Load mapped Soul Reaver room/unit entries in the **Level Editor** tab.
- Display a top-down terrain mesh, object/intro placements, and room portals.
- Follow portal links from the current room to load connected rooms into one scene.
- Double-click portal boxes in the level canvas to open or focus the connected room.
- Show friendly area labels beside internal room codes, such as `Silenced Cathedral - cathy1`.
- Show portal targets as normalized room names with target suffixes, such as `Silenced Cathedral / Train Route - train1 (target 6)`.
- Decode room names by following the room name pointer, including larger room files that used to appear as `(unnamed)`.
- Show friendly object names beside raw intro codes, such as `Camera Path / Trigger Path - campath`.
- Switch the level canvas between top-down and 3D orbit wireframe views.
- Rotate the 3D camera with left-drag on empty space or the Camera tab presets/nudge buttons, and pan the canvas with right-drag or middle-drag.
- Show world orientation cues for `TOP +Z`, `BOTTOM -Z`, `+X`, `-X`, and vertical `UP +Y`/`DOWN -Y` in 3D.
- Render large linked/whole-zone maps with cached terrain edges, per-frame vertex projection, and offscreen culling to reduce lag without dropping level rooms.
- Drag existing objects in top-down mode to move their X/Z placement.
- Edit existing object/intro position and rotation values, snap moved objects to nearby terrain Y, preserve unused spectral positions, and move spectral positions only when the object already has one.
- Show move-safety diagnostics when a moved object is off this room's decoded terrain, far from the nearest terrain Y, or inside/near a portal stream boundary.
- Save either all object edits in a room or only the selected object edit into a new patched `.bin` copy with rebuilt raw CD sector checksums.
- Warn before saving patches that move special, player/cutscene, warp-gate, stream, or unusually large-offset objects, with source-researched notes for known fragile records such as the `raziel` loader anchor used during Underworld startup relocation.

The level editor currently supports existing placement edits. Adding/removing entries and rebuilding terrain tables requires safe table growth and pointer relocation, so those controls are intentionally not enabled yet. Palette/audio candidates can already be identified, exported, changed externally, and patched back when the replacement is exactly the same size.

## Build

Run:

```powershell
.\build.ps1
```

The executable is written to:

```text
bin\SoulReaverEditor.exe
```
