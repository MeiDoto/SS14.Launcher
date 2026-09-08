# ❄️ Nix & NixOS Packaging Guide for SS14.Launcher

[🇬🇧 Read in English](README.md) | [🇷🇺 Читать на русском](README.ru.md)

This directory contains Nix packaging expressions for **Space Station 14 Launcher**, allowing reproducible builds and deployment on NixOS and non-NixOS Linux distributions via Nix package manager.

---

## 🏗️ Architecture & Requirements

The launcher package is declared in `nix/package.nix` using Nixpkgs' `buildDotnetModule`. It manages:
- **.NET SDK & Runtime**: Targets the required .NET Core SDK.
- **Native Avalonia UI Dependencies**:
  - X11 & Wayland: `libx11`, `libice`, `libsm`, `libxi`, `libxcursor`, `libxext`, `libxrandr`, `libxkbcommon`, `wayland`.
  - Font & Graphics: `fontconfig`, `freetype`, `libGL`.
  - Audio backends: `alsa-lib`, `libpulseaudio`, `pipewire`, `libjack2`.
  - Accessibility & IPC: `at-spi2-atk`, `at-spi2-core`, `dbus`, `glib`.
- **SoundFonts**: Bundles `soundfont-fluid` (`FluidR3_GM2-2.sf2`) via `ROBUST_SOUNDFONT_OVERRIDE`.
- **Desktop Integration**: Generates `.desktop` menu entries and application icons using `makeDesktopItem` and `copyDesktopItems`.

---

## 🔄 How to Update the Nix Package

When a new version of SS14.Launcher is released:

### Step 1: Update Version & Git Hash
1. Open `nix/package.nix`.
2. Update the `version` attribute (e.g. `version = "1.2.5";`).
3. Set the `hash` inside `fetchFromGitHub` to an empty string `""` or calculate the new hash via `nix-prefetch-github`:
   ```bash
   nix-prefetch-github space-wizards SS14.Launcher --rev v1.2.5 --fetch-submodules
   ```
4. Insert the resulting hash into `package.nix`.

### Step 2: Regenerate NuGet Dependencies (`deps.json`)
Nix requires a deterministic lockfile for NuGet packages. Regenerate it from the project root:
```bash
nix run .#fetch-deps nix/deps.json
```

### Step 3: Build and Test the Package
Compile the package in the isolated Nix build sandbox:
```bash
nix build .
```

### Step 4: Verify the Built Executable
Run the resulting binary from the Nix store symlink:
```bash
./result/bin/space-station-14-launcher
```

---

## 💡 Integrating into NixOS Configuration

To include the package in your NixOS system or home-manager configuration:

```nix
# In configuration.nix or home.nix
{ pkgs, ... }:
{
  environment.systemPackages = [
    (pkgs.callPackage ./nix/package.nix { })
  ];
}
```
