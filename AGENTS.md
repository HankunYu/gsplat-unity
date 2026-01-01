# Repository Guidelines

## Project Structure & Module Organization
This is a Unity package. Core runtime code lives in `Runtime/`, editor tooling is in `Editor/`, and shaders are under `Runtime/Shaders/`. Render-pipeline integrations live in `Runtime/SRP/`. Reference docs are in `Documentation~/`, with images in `Documentation~/Images/`.

## Build, Test, and Development Commands
There are no CLI build or test scripts in this repo. Use the Unity Editor:
- Install the package via `Window > Package Manager > + > Install package from disk...` and select `package.json`.
- Edit C# scripts under `Runtime/` and `Editor/`, then let Unity recompile.
If you add new scripts or shaders, ensure their `.meta` files are present and committed.

## Coding Style & Naming Conventions
- C# follows Unity conventions: 4-space indentation, braces on new lines, PascalCase for types and public members, camelCase for locals and private fields (often prefixed with `m_`).
- Keep namespaces under `Gsplat` where applicable.
- Shader files use the existing naming patterns in `Runtime/Shaders/` (e.g., `Gsplat.*`, `SortCommon.*`).
No formatter or linter is configured; match the surrounding style.

## Testing Guidelines
No automated tests are defined in this repository. Validate changes by:
- Opening a Unity project that consumes the package.
- Importing a `.ply` file into `Assets/` and attaching `Gsplat Renderer` to a GameObject.
- Verifying rendering in the target render pipeline (BiRP/URP/HDRP).

## Commit & Pull Request Guidelines
Recent commit messages are short, sentence-style summaries and sometimes include release tags or issue numbers (e.g., `v1.1.2`, `Supports ... (#9)`). Follow the same pattern.
For pull requests, include:
- A concise summary of changes and rationale.
- Any relevant Unity version, render pipeline, and platform tested.
- Screenshots or short clips if visuals change.

## Configuration Notes
- The sorting pass requires D3D12, Metal, or Vulkan; ensure the Graphics API matches the target platform.
- Gamma color space is required for best 3DGS results unless assets are trained in linear space.
