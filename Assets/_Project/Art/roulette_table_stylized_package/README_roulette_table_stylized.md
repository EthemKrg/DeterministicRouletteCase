# Stylized Roulette Table Asset

Generated with Blender 4.2 LTS as a game-ready roulette table package.

## Contents

- `roulette_table_stylized.blend`: source scene with switchable collections.
- `roulette_table_stylized.fbx`: combined FBX with EU and US variant objects plus both spin actions.
- `roulette_table_stylized_eu.fbx`: European-only import version.
- `roulette_table_stylized_us.fbx`: American-only import version.
- `roulette_table_stylized.obj` / `.mtl`: static European OBJ fallback.
- `roulette_table_stylized_us.obj` / `.mtl`: static American OBJ fallback.
- `textures/`, `*.fbm`: exported texture folders for the procedural felt and wood image maps.
- `preview_eu.png`, `preview_us.png`: quick render previews.
- `asset_manifest.json`: pocket sequences, counts, action names, and export list.

## Blender Collections

- `Table_Base`
- `Layout_EU`
- `Layout_US`
- `Wheel_EU`
- `Wheel_US`
- `Props`

The `.blend` opens with the European variant visible and the American layout/wheel hidden. Toggle `Layout_EU` + `Wheel_EU` or `Layout_US` + `Wheel_US` to switch versions on the shared table.

## Animations

- `EU_spin_loop`: loopable European wheel spin, frames 1-120.
- `US_spin_loop`: loopable American wheel spin, frames 1-120.

OBJ files are static by format limitation.

## Import Notes

- Export target is Unity/Web generic: meter scale, Y-up, `-Z` forward.
- Use the per-variant FBX files for direct engine import when you do not want overlapping switchable variant objects.
- Use the combined FBX when you want both variants in one file and will toggle objects/collections in the engine.
