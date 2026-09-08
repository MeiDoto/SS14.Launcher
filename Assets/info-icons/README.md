# Server Info Icons Specification & Asset Pipeline

[🇬🇧 Read in English](README.md) | [🇷🇺 Читать на русском](README.ru.md)

This directory contains the vector master files and specifications for server informational link icons rendered in `ServerInfoLinkControl` (e.g., Discord, Wiki, Website, Forum, Telegram, GitHub, Patreon, Boosty).

---

## 🎨 Asset Guidelines

1. **Source Vector Assets**:
   - Master files must be saved in SVG format in this directory (`Assets/info-icons/`).
   - SVG files must use a square aspect ratio (`1:1`), preferably `24x24` or `32x32` `viewBox`.
   - Paths must be simplified and optimized (remove unnecessary groups, metadata, XML declarations, and editor metadata).
   - Icons must be monochrome (single-color silhouettes or path shapes using `currentColor` or white `#FFFFFF`), allowing dynamic XAML styling and theme adaptation (light/dark mode contrast).

2. **Security & Sanitization**:
   - No embedded scripts (`<script>`), event handlers (`onload`, `onclick`), external CSS stylesheets, or external entity references (`ENTITY`) are permitted in SVG files.

3. **Exported PNG Assets**:
   - Export 32-bit RGBA PNG files at `32x32` and `64x64` (for HiDPI displays) into `SS14.Launcher/Assets/info-icons/`.
   - PNG files must be lossless-compressed (e.g. using `oxipng` or `pngquant`).

---

## 💻 Code Integration Pipeline

To register and render a new info link icon in the launcher:

1. **Place Vector Source**: Save the clean SVG in `Assets/info-icons/<icon-name>.svg`.
2. **Export Bitmap Asset**: Place the rendered PNG into `SS14.Launcher/Assets/info-icons/<icon-name>.png`.
3. **Register Valid Icon Key**:
   Add the canonical identifier to the `ValidIcons` hash set in `SS14.Launcher/Controls/ServerInfoLinkControl.xaml.cs`:
   ```csharp
   private static readonly HashSet<string> ValidIcons = new()
   {
       "discord", "wiki", "website", "forum", "github", "telegram", "patreon", "boosty", "<icon-name>"
   };
   ```
4. **Register in Icon Resource Loader**:
   Add the asset resource URI mapping in `SS14.Launcher/Utility/IconsLoader.cs`:
   ```csharp
   LoadIcon("<icon-name>", "avares://SS14.Launcher/Assets/info-icons/<icon-name>.png");
   ```
5. **Verify Theming**:
   Ensure the icon displays cleanly against both default dark theme backgrounds and customized user themes.
