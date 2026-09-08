# 🧪 Fake Robust.Client Test Harness

[🇬🇧 Read in English](README.md) | [🇷🇺 Читать на русском](README.ru.md)

`SS14.Launcher.FakeRobustToolbox` is a lightweight development and integration testing fixture designed to simulate game client launches, `Robust.LoaderApi` interop, and client-to-launcher callback protocols without requiring a real Space Station 14 build.

---

## 🎯 Architecture & Purpose

1. **Loader API Verification (`Robust.LoaderApi`)**:
   Implements `ILoaderEntryPoint` with the assembly attribute `[assembly: LoaderEntryPoint(typeof(Robust.Client.FakeClient))]`.
   When the launcher loads an engine build, it locates the entry point via reflection and invokes `Main(IMainArgs args)`.
2. **Redial Protocol Verification**:
   Tests the launcher's `RedialApi` interface (`IMainArgs.RedialApi.Redial(Uri, string)`). The fake client invokes redial to `ss14://localhost:1212` with multiline message reasons, allowing automated or manual verification of reconnection flows and dialog display.
3. **Local HTTP Server Mock**:
   The `server/` directory contains helper scripts and mock endpoints (`/status`, `/info`) to act as a local SS14 server responding to status checks.

---

## 🚀 How to Build & Run

### 1. Build the Fake Client
```bash
cd SS14.Launcher.FakeRobustToolbox
dotnet build
```

### 2. Package and Install Test Engine
The `server/run` script packages the compiled assembly into an engine zip archive named `meme.zip` and installs it to the local launcher engine directory:

```bash
cd server
./run
```

This will:
1. Compile `Robust.Client.dll`.
2. Archive it into `client.zip`.
3. Copy it to `~/.local/share/Space Station 14/launcher/engines/meme.zip`.
4. Spin up a local Python HTTP server on port `1212` serving status and info endpoints.

### 3. Connect via Launcher
In the launcher, configure an engine override or trigger connection to `ss14://localhost:1212` to verify client launch, argument passing, and redial behavior.
