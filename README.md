# SS14 Launcher — Custom Edition

> Кастомная сборка лаунчера Space Station 14 с расширенными возможностями.  
> A custom build of the Space Station 14 launcher with extended features.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/UI-Avalonia-blue)](https://avaloniaui.net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![Latest Release](https://img.shields.io/github/v/release/MeiDoto/SS14.Launcher)](https://github.com/MeiDoto/SS14.Launcher/releases/latest)

---

## Table of Contents / Содержание

- [English](#english)
- [Русский](#русский)

---

<a name="english"></a>

## English

A fork of the official Space Station 14 launcher built on .NET 10 and Avalonia UI. Adds smart server search, UI theming, performance tweaks, a self-updater, replays viewer, developer tools, and more.

### Installation

Download the latest release for your platform:

| Platform | File | Notes |
|----------|------|-------|
| **Windows x64** | `SS14.Launcher_Windows.zip` | Extract anywhere, run `Space Station 14 Launcher.exe` |
| **Linux x64** | `SS14.Launcher_Linux.tar.gz` | Extract, run `./SS14.Launcher` |

**Desktop shortcuts** — after launch, go to **Options → Create Desktop Shortcut** to add shortcuts to your desktop and application menu (works on both Windows and Linux). Alternatively, run `create-shortcut.bat` (Windows) or `setup-desktop.sh` (Linux) from the extracted folder.

### Building from Source

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download), Python 3 (for release builds only).

```bash
# Clone the repo
git clone --recursive https://github.com/MeiDoto/SS14.Launcher.git
cd SS14.Launcher

# Run in development mode
dotnet run --project SS14.Launcher/SS14.Launcher.csproj

# Run tests
dotnet test SS14.Launcher.Tests/SS14.Launcher.Tests.csproj

# Build release packages for Windows and Linux (x64)
python3 publish.py windows linux --x64-only
# Output: SS14.Launcher_Windows.zip, SS14.Launcher_Linux.tar.gz
```

### Features

#### Self-Updater

The launcher checks `MeiDoto/SS14.Launcher` releases on GitHub at startup. If a new version is found, it shows a prompt with **Update Now**, **Later**, and **Skip Version** options. Downloads support resume (HTTP Range), SHA256 integrity verification, and exponential backoff retries. Before applying, the updater backs up current binaries to `_backup/`.

The update logic lives in [`LauncherUpdateManager.cs`](SS14.Launcher/Utility/LauncherUpdateManager.cs). Key config:

```csharp
// ConfigConstants.cs
public const string LauncherCustomVersion = "1.1.5";     // current version tag
public const string LauncherGitHubRepo = "MeiDoto/SS14.Launcher"; // repo to check
```

#### Server Search

Multi-stage fuzzy search implemented in [`SearchAlgorithm.cs`](SS14.Launcher/Utility/SearchAlgorithm.cs) and [`AdvancedAlgorithms.cs`](SS14.Launcher/Utility/AdvancedAlgorithms.cs):

1. **Exact match** → score 1000
2. **Starts with** → score 800
3. **Word boundary** → score 600
4. **Substring** → score 400 (penalized by position)
5. **Damerau-Levenshtein distance** → fuzzy matching with transpositions
6. **Jaro-Winkler similarity** → prefix-weighted string similarity
7. **Trigram cosine similarity** → character n-gram comparison

Ping values are smoothed with a 1D Kalman filter (`KalmanLatencyTracker`) with 3.5σ chi-squared outlier rejection to prevent UI flickering from network jitter.

#### Recommended Filter

The **Recommended** filter in the server list scores servers by a composite quality metric:

```
score = playerScore × 0.40 + stabilityScore × 0.30 + latencyScore × 0.30
```

- `playerScore` — based on player count relative to max slots (sweet spot: 20–80% full)
- `stabilityScore` — uptime consistency and player velocity over time
- `latencyScore` — inverse of Kalman-smoothed ping

Servers scoring above a configurable threshold appear in the Recommended list.

#### UI Customizer

Open via **Options → Customize Launcher**. Built-in presets:

| Theme | Style |
|-------|-------|
| Classic | Default dark theme |
| Cyberpunk | Neon accents, purple/pink tones |
| Syndicate | Red and black |
| Solar | Warm orange tones |
| Deep Space | Deep blue |
| Matrix | Green-on-black terminal look |
| Monochrome | Grayscale |

You can also configure individual elements:

```
CustomAccentColor         — hex color, e.g. #ADA24B
CustomBackgroundImagePath — path to background image
CustomLogoImagePath       — path to logo image
CustomBackgroundOpacity   — 0.1 to 1.0
CustomButtonColor         — hex color for buttons
CustomTabSelectedColor    — hex color for active tab
CustomHeaderColor         — hex color for header bar
CustomTextColor           — hex color for text
CustomFontSize            — 12.0 to 22.0 (default: 15.0)
CustomWindowTitle         — custom window title text
CustomTabPlacement        — Top, Bottom, Left, Right
```

There is also a built-in style script editor (`CustomUserCode`) for advanced CSS-like customization.

#### Performance Tweaks

Configurable via **Options → Performance** and **Developer Tab**:

| Setting | CVar | Default | Description |
|---------|------|---------|-------------|
| Tiered PGO | `EnableTieredPGO` | `true` | Dynamic Profile-Guided Optimization |
| Server GC | `ForceServerGC` | `true` | Multi-core garbage collector |
| Low-Pause GC | `LowPauseGc` | `false` | Reduces GC pause spikes |
| High Priority | `HighProcessPriority` | `false` | Sets game process to high priority |
| Dedicated GPU | `ForceDedicatedGpu` | `false` | Forces discrete GPU via env vars |
| Max Perf JIT | `MaxPerformanceJit` | `false` | Aggressive JIT optimizations |
| Low-Latency Net | `LowLatencyNetworking` | `false` | TCP_NODELAY and socket tuning |
| DNS-over-HTTPS | `DnsOverHttps` | `false` | Route DNS through HTTPS |
| Server Preload | `FastLaunchPreload` | `false` | Pre-downloads content for favorites |
| Cache Cleaner | `SmartCacheCleaner` | `false` | Auto-cleans old cached content |

These inject environment variables (`DOTNET_TieredPGO`, `DOTNET_gcServer`, etc.) into the game client process at launch time. See [`Connector.cs`](SS14.Launcher/Models/Connector.cs) for the injection logic.

#### Proxy Support

SOCKS5 and HTTP proxy support for routing launcher and/or game client traffic:

```
ProxyEnabled           — true/false
ProxyType              — SOCKS5 or HTTP
ProxyHost              — default: 127.0.0.1
ProxyPort              — default: 1080
ProxyUsername           — optional auth
ProxyPassword           — optional auth
ProxyApplyToGameClient — route game traffic through proxy
ProxyApplyToLauncher   — route launcher traffic through proxy
```

#### Developer Tab

Enable with `ShowDevelopmentTab = true` in settings. Provides:

| Setting | Description |
|---------|-------------|
| Custom Launch Arguments | Extra CLI args passed to game client |
| Log Level | Default, Verbose, Debug, Information, Warning, Error |
| Uncapped FPS | Removes frame rate limiter |
| Crash Dumps | Enables crash dump generation |
| Render Validation | GPU debug validation layer |
| Simulated Ping/Jitter/Packet Loss | Network condition simulation |
| Graphics Backend | Default, OpenGL, Vulkan |
| Display Mode | Default, Windowed, Fullscreen, Borderless |
| FPS Overlay, Net Graph, Physics Debug | Debug overlays |
| Custom Env Vars | Arbitrary environment variables for game process |

#### Replays

Enable with `ShowReplaysTab = true`. The replays tab scans `~/.local/share/Space Station 14/replays/` (Linux) or `%AppData%/Space Station 14/replays/` (Windows) for `.zip` replay files. Supports search/filter by name, date sorting, one-click playback, and deletion.

#### Localization

English (`en-US`) and Russian (`ru`) with instant hot-switching. Locale files are in Fluent format:

```
SS14.Launcher/Assets/Locale/en-US/text.ftl
SS14.Launcher/Assets/Locale/ru/text.ftl
```

### Project Structure

```
SS14.Launcher/
├── SS14.Launcher/           # Main app (Avalonia UI, .NET 10)
│   ├── Assets/Locale/       # Fluent localization files (en-US, ru)
│   ├── Models/              # Business logic, networking, auth
│   │   ├── Connector.cs     # Game process launch & env var injection
│   │   ├── Data/CVars.cs    # All configuration variables
│   │   └── ServerStatus/    # Server polling, caching, hubs
│   ├── Utility/
│   │   ├── AdvancedAlgorithms.cs    # Kalman filter, Damerau-Levenshtein, scoring
│   │   ├── SearchAlgorithm.cs       # Multi-stage server search
│   │   ├── LauncherUpdateManager.cs # Self-updater with GitHub API
│   │   └── DesktopIntegration.cs    # Cross-platform shortcut creator
│   ├── ViewModels/          # MVVM ViewModels
│   └── Views/               # Avalonia XAML views
├── SS14.Launcher.Tests/     # NUnit test project
├── SS14.Loader/             # Native/managed game loader
├── Robust.LoaderApi/        # Engine loader API (git submodule)
├── PublishFiles/             # Desktop entries, icons, install scripts
├── publish.py               # Release packaging script
├── download_net_runtime.py  # .NET runtime bundler
└── exe_set_subsystem.py     # Windows PE subsystem setter
```

### Environment Variables

The launcher respects the following env vars:

| Variable | Description |
|----------|-------------|
| `SS14_LAUNCHER_OVERRIDE_AUTH` | Override auth server URL (dev only) |

---

<a name="русский"></a>

## Русский

Форк официального лаунчера Space Station 14 на .NET 10 и Avalonia UI. Умный поиск серверов, темы оформления, настройки производительности, автообновление, просмотр реплеев, инструменты разработчика.

### Установка

Скачайте последний релиз:

| Платформа | Файл | Примечание |
|-----------|------|------------|
| **Windows x64** | `SS14.Launcher_Windows.zip` | Распаковать, запустить `Space Station 14 Launcher.exe` |
| **Linux x64** | `SS14.Launcher_Linux.tar.gz` | Распаковать, запустить `./SS14.Launcher` |

**Ярлыки** — после запуска: **Настройки → Создать ярлык** (работает на Windows и Linux). Или запустите `create-shortcut.bat` (Windows) / `setup-desktop.sh` (Linux) из папки с лаунчером.

### Сборка из исходников

Требования: [.NET 10 SDK](https://dotnet.microsoft.com/download), Python 3 (только для релизной сборки).

```bash
# Клонирование
git clone --recursive https://github.com/MeiDoto/SS14.Launcher.git
cd SS14.Launcher

# Запуск в режиме разработки
dotnet run --project SS14.Launcher/SS14.Launcher.csproj

# Тесты
dotnet test SS14.Launcher.Tests/SS14.Launcher.Tests.csproj

# Сборка релизных пакетов (Windows + Linux, x64)
python3 publish.py windows linux --x64-only
# Результат: SS14.Launcher_Windows.zip, SS14.Launcher_Linux.tar.gz
```

### Возможности

#### Автообновление

Лаунчер проверяет новые версии на GitHub при каждом запуске. Если доступно обновление — предлагает **Обновить**, **Позже** или **Пропустить версию**. Загрузка поддерживает докачку (HTTP Range), проверку SHA256 и автоматические повторные попытки с экспоненциальной задержкой. Перед обновлением создаётся резервная копия в `_backup/`.

#### Поиск серверов

Многоступенчатый нечёткий поиск ([`SearchAlgorithm.cs`](SS14.Launcher/Utility/SearchAlgorithm.cs)):

1. **Точное совпадение** → 1000 баллов
2. **Начинается с запроса** → 800
3. **Совпадение по началу слова** → 600
4. **Подстрока** → 400 (со штрафом за позицию)
5. **Расстояние Дамерау-Левенштейна** → нечёткий поиск с учётом транспозиций
6. **Сходство Джаро-Винклера** → сравнение с весом на префикс
7. **Косинусное сходство триграмм** → сравнение символьных n-грамм

Пинг сглаживается фильтром Калмана с отсечением выбросов на уровне 3.5σ (хи-квадрат).

#### Фильтр «Рекомендованные»

Составной скоринг серверов:

```
score = playerScore × 0.40 + stabilityScore × 0.30 + latencyScore × 0.30
```

- `playerScore` — оптимальная заполненность 20–80%
- `stabilityScore` — стабильность аптайма и динамика онлайна
- `latencyScore` — обратная величина сглаженного пинга

#### Кастомизатор

Открывается через **Настройки → Кастомизировать**. Встроенные темы:

| Тема | Описание |
|------|----------|
| Classic | Стандартная тёмная |
| Cyberpunk | Неоновые акценты |
| Syndicate | Красно-чёрная |
| Solar | Тёплые оранжевые тона |
| Deep Space | Глубокий синий |
| Matrix | Зелёный терминал |
| Monochrome | Чёрно-белая |

Настраиваемые параметры:

| Параметр | Описание |
|----------|----------|
| `CustomAccentColor` | Акцентный цвет (hex, напр. `#ADA24B`) |
| `CustomBackgroundImagePath` | Путь к фоновому изображению |
| `CustomBackgroundOpacity` | Прозрачность фона (0.1–1.0) |
| `CustomButtonColor` | Цвет кнопок |
| `CustomFontSize` | Размер шрифта (12–22, по умолчанию 15) |
| `CustomWindowTitle` | Заголовок окна |
| `CustomTabPlacement` | Расположение вкладок: Top, Bottom, Left, Right |

#### Производительность

Настройки в **Опции → Производительность**:

| Настройка | По умолчанию | Описание |
|-----------|-------------|----------|
| Tiered PGO | ✅ | Динамическая оптимизация компиляции |
| Server GC | ✅ | Многопоточный сборщик мусора |
| Low-Pause GC | ❌ | Снижение пауз GC |
| High Priority | ❌ | Повышенный приоритет процесса |
| Dedicated GPU | ❌ | Принудительно дискретная видеокарта |
| Low-Latency Net | ❌ | TCP_NODELAY и тюнинг сокетов |
| Server Preload | ❌ | Предзагрузка контента избранных серверов |
| Cache Cleaner | ❌ | Автоочистка старого кэша |

Эти настройки передаются игровому клиенту через переменные окружения (`DOTNET_TieredPGO`, `DOTNET_gcServer` и др.) при запуске. Логика в [`Connector.cs`](SS14.Launcher/Models/Connector.cs).

#### Прокси

Поддержка SOCKS5 и HTTP прокси для маршрутизации трафика лаунчера и/или игрового клиента. Настраивается в **Опции → Прокси**.

#### Вкладка разработчика

Включается через `ShowDevelopmentTab = true`. Позволяет задать кастомные аргументы запуска, уровень логирования, симулировать пинг/потери пакетов, переключить графический бекенд (OpenGL/Vulkan), включить оверлеи отладки (FPS, сеть, физика).

#### Реплеи

Включается через `ShowReplaysTab = true`. Сканирует `~/.local/share/Space Station 14/replays/` на Linux и `%AppData%/Space Station 14/replays/` на Windows. Поиск, сортировка по дате, воспроизведение и удаление.

#### Локализация

Английский и русский с мгновенным переключением. Файлы локализации в формате Fluent:

```
SS14.Launcher/Assets/Locale/en-US/text.ftl
SS14.Launcher/Assets/Locale/ru/text.ftl
```

### Структура проекта

```
SS14.Launcher/
├── SS14.Launcher/           # Основное приложение (Avalonia UI, .NET 10)
│   ├── Assets/Locale/       # Файлы локализации (en-US, ru)
│   ├── Models/              # Бизнес-логика, сеть, авторизация
│   │   ├── Connector.cs     # Запуск игры и внедрение переменных окружения
│   │   ├── Data/CVars.cs    # Все конфигурационные переменные
│   │   └── ServerStatus/    # Опрос серверов, кэширование, хабы
│   ├── Utility/
│   │   ├── AdvancedAlgorithms.cs    # Фильтр Калмана, Дамерау-Левенштейн, скоринг
│   │   ├── SearchAlgorithm.cs       # Многоступенчатый поиск серверов
│   │   ├── LauncherUpdateManager.cs # Автообновление через GitHub API
│   │   └── DesktopIntegration.cs    # Кроссплатформенные ярлыки
│   ├── ViewModels/          # MVVM ViewModels
│   └── Views/               # Avalonia XAML разметка
├── SS14.Launcher.Tests/     # Тесты (NUnit)
├── SS14.Loader/             # Нативный загрузчик игры
├── Robust.LoaderApi/        # API загрузчика движка (git submodule)
├── PublishFiles/             # .desktop файлы, иконки, скрипты установки
├── publish.py               # Скрипт сборки релизов
├── download_net_runtime.py  # Загрузчик .NET рантайма для бандла
└── exe_set_subsystem.py     # Установка PE-подсистемы для Windows
```

---

## License / Лицензия

MIT — see [LICENSE.txt](LICENSE.txt).
