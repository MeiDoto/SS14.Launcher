# 🎨 SS14.Launcher Customization & Extensibility Guide

[🇬🇧 Read in English](CUSTOMIZATION.md) | [🇷🇺 Читать на русском](CUSTOMIZATION.ru.md)

This guide documents the visual personalization, runtime performance tuning, and developer tooling capabilities provided by **Space Station 14 Launcher**.

---

## 📑 Table of Contents

1. [Visual Theming & UI Customization](#-visual-theming--ui-customization)
   - [1.1. Custom Color Palette](#11-custom-color-palette)
   - [1.2. Background Images & Visual Effects](#12-background-images--visual-effects)
   - [1.3. Tab Strip Layouts](#13-tab-strip-layouts)
2. [Game Engine & Performance Tuning](#-game-engine--performance-tuning)
   - [1.1. High Process Priority](#21-high-process-priority)
   - [2.2. Dedicated GPU Offloading](#22-dedicated-gpu-offloading)
   - [2.3. Tiered JIT Optimization](#23-tiered-jit-optimization)
   - [2.4. Custom Launch Arguments & Environment Overrides](#24-custom-launch-arguments--environment-overrides)
3. [Developer & Server Host Features](#-developer--server-host-features)
   - [3.1. Custom Hub Routing](#31-custom-hub-routing)
   - [3.2. Engine & Content Overrides](#32-engine--content-overrides)
   - [3.3. Replay Management & Debug Log Inspection](#33-replay-management--debug-log-inspection)

---

## 🎨 Visual Theming & UI Customization

The Launcher Customizer (`Options` -> `Personalization` or Customizer Dialog) allows extensive theming:

### 1.1. Custom Color Palette
Users can define dynamic hex colors that reactively update the Avalonia application resource dictionary:
- **Accent Color** (`CustomAccentColor`): Base branding color for buttons, active tabs, and sliders.
- **Accent Hover Color** (`CustomAccentHoverColor`): Interactive highlight states.
- **Window Background Color** (`CustomWindowBgColor`): Master background tone.
- **Card Background Color** (`CustomCardBgColor`): Panel cards and server list entry backgrounds.
- **Text Color** (`CustomTextColor`): Foreground contrast typography.

### 1.2. Background Images & Visual Effects
- **Custom Wallpaper**: Support for `.png`, `.jpg`, and `.webp` images.
- **Opacity Slider**: Blends the background image with the card canvas from 0% to 100%.
- **Blur Radius**: Applies hardware-accelerated Gaussian blur to prevent busy wallpapers from interfering with text readability.

### 1.3. Tab Strip Layouts
Configure navigation placement via `TabStripPlacement`:
- **Top**: Classic desktop application navigation.
- **Left**: Modern vertical sidebar for widescreen displays.
- **Bottom**: Compact handheld/deck-style ergonomics.

---

## 🚀 Game Engine & Performance Tuning

Accessible under the **Options** -> **Game Performance** section:

### 2.1. High Process Priority
Sets the operating system process priority class of the launched game client to `AboveNormal` or `High` (`ProcessPriorityClass.High` on Windows, `nice -n -10` on Linux/macOS). Ensures consistent game frame rendering during CPU contention.

### 2.2. Dedicated GPU Offloading
Ensures dual-GPU laptops (e.g. Intel + NVIDIA or AMD + AMD) execute the game on the discrete performance GPU:
- **Linux**: Injects `DRI_PRIME=1`, `__NV_PRIME_RENDER_OFFLOAD=1`, and `__GLX_VENDOR_LIBRARY_NAME=nvidia`.
- **Windows**: Configures DirectX high-performance adapter preference flags via `DXGI` application profile registration.

### 2.3. Tiered JIT Optimization
Enables .NET runtime flags optimizing JIT compilation speed and code generation:
- `DOTNET_TieredPGO=1`: Dynamic Profile-Guided Optimization.
- `DOTNET_TC_QuickJitForLoops=1`: Accelerated loop compilation for faster game map loading.

### 2.4. Custom Launch Arguments & Environment Overrides
Under **Development Settings**, users can append custom CLI flags (e.g. `--cvar display.vsync=false`) or supply custom environment variables (`ROBUST_SOUNDFONT_OVERRIDE=...`).

---

## 🛠️ Developer & Server Host Features

### 3.1. Custom Hub Routing
Server communities can configure custom hub URLs in `CVars.HubUrls`. The launcher merges community servers seamlessly into the server browser while retaining all search, filtering, and latency measurement features.

### 3.2. Engine & Content Overrides
For game developers testing local pull requests:
- **Engine Override**: Direct the launcher to boot a locally built Robust client DLL rather than downloading official CDN packages.
- **Content Override**: Point the launcher to a local Git working directory for instant live testing without packing zip manifests.

### 3.3. Replay Management & Debug Log Inspection
- Built-in **Replay Viewer Tab**: Parse, inspect, and launch round recordings (`.zip`) stored in the replay directory.
- **Integrated Log Viewer**: Searchable, real-time diagnostic log stream with Serilog level filters (`Verbose`, `Debug`, `Info`, `Warning`, `Error`) and copy-to-clipboard functionality.
