# Space Station 14 Launcher (Custom Edition)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/UI-Avalonia_11-7F52FF)](https://avaloniaui.net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)
[![Latest Release](https://img.shields.io/github/v/release/MeiDoto/SS14.Launcher?color=brightgreen)](https://github.com/MeiDoto/SS14.Launcher/releases/latest)

Кастомная сборка лаунчера Space Station 14 на платформе **.NET 10** и **Avalonia UI** с расширенным функционалом, продвинутой кастомизацией интерфейса, оптимизацией сетевого стека и встроенными инструментами разработчика.

A custom fork of the Space Station 14 launcher powered by **.NET 10** and **Avalonia UI**, featuring deep UI theming, smart search, network optimizations, and developer utilities.

---

## Навигация / Navigation

- [Русский](#-русский)
  - [Быстрый старт и установка](#быстрый-старт-и-установка)
  - [Основные возможности](#основные-возможности)
  - [Песочница скриптов оформления](#песочница-скриптов-оформления)
  - [Сборка из исходников](#сборка-из-исходников)
- [English](#-english)
  - [Installation & Quick Start](#installation--quick-start)
  - [Key Features](#key-features)
  - [Theming Script Sandbox](#theming-script-sandbox)
  - [Building from Source](#building-from-source)

---

## 🇷🇺 Русский

### Быстрый старт и установка

Готовые релизные архивы доступны на странице [Releases](https://github.com/MeiDoto/SS14.Launcher/releases/latest):

| Платформа | Файл | Инструкция по запуску |
|---|---|---|
| **Windows x64** | `SS14.Launcher_Windows.zip` | Распаковать архив и запустить `Space Station 14 Launcher.exe` |
| **Linux x64** | `SS14.Launcher_Linux.tar.gz` | Распаковать архив, дать права на запуск (`chmod +x SS14.Launcher`) и запустить `./SS14.Launcher` |

*Интеграция с системой*: в окне **Опции** доступна кнопка создания ярлыков на рабочем столе и в меню приложений для Windows и Linux (XDG Desktop).

---

### Основные возможности

- **Умный многоступенчатый поиск серверов**:
  - Комбинированный алгоритм (префиксы, подстроки, расстояние Дамерау-Левенштейна, сходство Джаро-Винклера и триграмм).
  - Сглаживание пинга с помощью фильтра Калмана с отсечением сетевых всплесков (3.5σ).
  - Категория «Рекомендованные» на основе комплексной оценки заполненности, стабильности и сетевой задержки.

- **Кастомизация и оформление**:
  - Выбор встроенных тем (*Classic, Cyberpunk, Syndicate, Solar, Deep Space, Matrix, Monochrome*).
  - Настройка цветов интерфейса, акцентов, шрифта, прозрачности и фоновых изображений.
  - Изменение расположения вкладок (Сверху, Снизу, Слева, Справа).
  - Генератор процедурных Sci-Fi палитр и экспорт/импорт конфигураций.

- **Безопасность и аккаунт**:
  - Спойлерная защита `User ID` и `HWID` (скрыты по умолчанию с возможностью показа).
  - Безопасное копирование данных в буфер обмена с визуальной индикацией (`Скопировано ✓`).
  - Отображение полного суммарного времени в игре с грамматическим склонением числительных.

- **Сетевой стек и производительность**:
  - Поддержка SOCKS5 и HTTP прокси для лаунчера и игрового процесса.
  - Настройки оптимизации: Dynamic Tiered PGO, Server GC, Low-Pause GC, Low-Latency Sockets (`TCP_NODELAY`), запуск на дискретной видеокарте.
  - Happy Eyeballs параллельное подключение к IPv4/IPv6.

- **Инструменты разработчика и логи**:
  - Вкладка **DEV**: запуск с кастомными аргументами, переключение графических бекендов (OpenGL/Vulkan), встроенные оверлеи (FPS, Network Graph, Physics Debug), симуляция пинга и потерь пакетов.
  - Просмотрщик логов с разделением по уровням (*Все, Ошибки, Предупреждения, Инфо, Дебаг*) и поиском.
  - Просмотр и запуск локальных реплеев (`.zip`) и тестовых сборок.

---

### Песочница скриптов оформления

В окне кастомизации доступна текстовая консоль/песочница для быстрой настройки внешнего вида:

```text
accent #00FFCC         # Установить акцентный цвет
button #1E293B         # Установить цвет кнопок
font 16                # Изменить размер шрифта (12-22)
opacity 0.85           # Прозрачность фона (0.1-1.0)
tabs left              # Перенести вкладки (top, bottom, left, right)
preset cyberpunk       # Применить готовый пресет (classic, syndicate, solar...)
random                 # Сгенерировать случайную гармоничную Sci-Fi тему
clear                  # Сбросить настройки к значениям по умолчанию
```

---

### Сборка из исходников

**Требования**: [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), Python 3.

```bash
# Клонирование репозитория
git clone --recursive https://github.com/MeiDoto/SS14.Launcher.git
cd SS14.Launcher

# Запуск лаунчера в режиме разработки
dotnet run --project SS14.Launcher/SS14.Launcher.csproj

# Запуск тестов
dotnet test SS14.Launcher.Tests/SS14.Launcher.Tests.csproj

# Сборка готовых пакетов под Windows и Linux
python3 publish.py windows linux --x64-only
```

---

## 🇬🇧 English

### Installation & Quick Start

Pre-built binaries are available on the [Releases](https://github.com/MeiDoto/SS14.Launcher/releases/latest) page:

| Platform | Archive | Run Instructions |
|---|---|---|
| **Windows x64** | `SS14.Launcher_Windows.zip` | Extract archive and launch `Space Station 14 Launcher.exe` |
| **Linux x64** | `SS14.Launcher_Linux.tar.gz` | Extract archive, grant execute permissions (`chmod +x SS14.Launcher`), and run `./SS14.Launcher` |

*System Integration*: Desktop and application menu shortcuts can be created directly from the **Options** tab on both Windows and Linux.

---

### Key Features

- **Multi-Stage Fuzzy Server Search**:
  - Combined scoring algorithm using prefix matching, word boundary heuristics, Damerau-Levenshtein distance, Jaro-Winkler, and trigram cosine similarity.
  - Adaptive 1D Kalman latency filter with outlier rejection (3.5σ) to eliminate ping jitter.
  - "Recommended" smart filter ranking servers based on capacity, uptime stability, and connection latency.

- **Deep UI Theming & Customization**:
  - Built-in theme presets (*Classic, Cyberpunk, Syndicate, Solar, Deep Space, Matrix, Monochrome*).
  - Fine-grained controls for accent colors, buttons, fonts, opacity, and custom background/logo images.
  - Flexible tab strip placement (*Top, Bottom, Left, Right*).
  - Procedural Sci-Fi palette generator and theme export/import via JSON.

- **Account & Security Enhancements**:
  - Spoiler-masked `User ID` and `HWID` fields with copy confirmation indicators.
  - Account diagnostics summary and session status overview.
  - Natural playtime formatting with proper language pluralization.

- **Performance & Networking**:
  - Configurable SOCKS5 and HTTP proxy support.
  - Runtime optimizations: Dynamic Tiered PGO, Server GC, Low-Pause GC, Low-Latency Sockets (`TCP_NODELAY`), discrete GPU enforcement.
  - Happy Eyeballs IPv4/IPv6 parallel connection resolution.

- **Developer Utilities & Logs**:
  - **DEV Tab**: custom command-line arguments, graphics backend selector (OpenGL/Vulkan), debug overlays (FPS, Net Graph, Physics), simulated latency and packet loss.
  - Filterable Log Viewer with level chips (*All, Errors, Warnings, Info, Debug*) and search.
  - Replay manager for local `.zip` recordings and local client build launcher.

---

### Theming Script Sandbox

The customization dialog includes a lightweight command sandbox for styling:

```text
accent #00FFCC         # Set accent color (hex)
button #1E293B         # Set button background color
font 16                # Set font size (12-22)
opacity 0.85           # Background opacity (0.1-1.0)
tabs left              # Tab strip placement (top, bottom, left, right)
preset cyberpunk       # Load a preset (classic, syndicate, solar...)
random                 # Generate procedural sci-fi palette
clear                  # Reset visuals to defaults
```

---

### Building from Source

**Prerequisites**: [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), Python 3.

```bash
# Clone the repository
git clone --recursive https://github.com/MeiDoto/SS14.Launcher.git
cd SS14.Launcher

# Run the launcher in debug mode
dotnet run --project SS14.Launcher/SS14.Launcher.csproj

# Run test suite
dotnet test SS14.Launcher.Tests/SS14.Launcher.Tests.csproj

# Build release packages for Windows and Linux
python3 publish.py windows linux --x64-only
```

---

## License / Лицензия

MIT License — see [LICENSE.txt](LICENSE.txt).
