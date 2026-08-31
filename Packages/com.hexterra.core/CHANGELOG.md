# Changelog

## [Unreleased]

### New

- Added `HexMap.Cells` and `HexGrid.Cells`. The map's cells as an `IReadOnlyList<HexCell>`.

### Changes

- `HexCell.neighbours` is a `HexCell[]` now, not a `GameObject[]`. Code reading it no longer needs `GetComponent<HexCell>()`.
- Renamed the Package Manager entry to `HexTerra Core`. The `com.hexterra.core` id is unchanged.
- Lowered the minimum Unity version to 2022.3 and declared the `com.unity.test-framework` dependency.

### Fixes

- Auto-regenerate triggers only on `HexMap` and `NoisePreset` edits. It was rebuilding on any component change in the scene.

## [0.2.0] - 2026-08-30

A pass over the noise system: new shaping and layering nodes, a rebuilt inspector for the graph.

### New

- Added `TerraceNoise`. Snaps a field to a set number of flat heights with a ramp between each.
- Added `RemapNoise`. Stretches a chosen input window to fill `[0, 1]`, with a "Calibrate to source range" button that reads the actual range off the source.
- Added `SmoothNoise`. A box blur that softens a field's shape rather than its values.
- Added `LayeredNoise`. Folds a list of sources together, each with its own blend mode, weight and optional mask.
- Added a `scale` field to `PerlinNoise`, `SimplexNoise` and `WorleyNoise`, so each generator sets its own feature size.
- Added `FractalNoise.octaveRotation`. Turns each octave a few degrees past the last so their axis-aligned artefacts cancel instead of stacking into streaks. Defaults to 30.
- Rebuilt the `Noise2D` inspector. Type picker on every slot, `LayeredNoise` layers as a reorderable list, an insert/remove menu for slipping a combiner above a node, and each node boxed with its type as the heading.
- Added the `Steppe` preset, built on the new nodes.

### Changes

- Split `bands` in two. `NoisePreset.maxHeight` sets the tallest a cell can be, and `HeightmapConverter.Render`'s `steps` sets the preview's grey levels.
- Reworked how the `HexMap` inspector saves noise edits: Overwrite, Save as New and Reset. Edits now survive an inspector rebuild.
- `FractalNoise` presets render differently. `octaveRotation` stops octave artefacts stacking into streaks.

### Fixes

- `PerlinNoise` clamps its output to `[0, 1]`. `Mathf.PerlinNoise` can drift slightly past the edges.

## [0.1.0]

Initial release. Hex grid and terrain generation, pluggable heightmap sources, and a composable `Noise2D` graph.
