# ❄️ Руководство по сборке и пакетированию в Nix & NixOS

[🇬🇧 Read in English](README.md) | [🇷🇺 Читать на русском](README.ru.md)

В данном каталоге содержатся выражения пакетирования для пакетного менеджера **Nix**, обеспечивающие воспроизводимую сборку и запуск **Space Station 14 Launcher** в NixOS и любых дистрибутивах Linux с установленным Nix.

---

## 🏗️ Архитектура пакета и зависимости

Декларация пакета расположена в `nix/package.nix` на базе модуля Nixpkgs `buildDotnetModule`. Она управляет:
- **.NET SDK и средой выполнения**: Сборка целевого SDK .NET.
- **Нативными зависимостями графического интерфейса Avalonia**:
  - Графические подсистемы X11 и Wayland: `libx11`, `libice`, `libsm`, `libxi`, `libxcursor`, `libxext`, `libxrandr`, `libxkbcommon`, `wayland`.
  - Шрифты и рендеринг: `fontconfig`, `freetype`, `libGL`.
  - Звуковые серверы: `alsa-lib`, `libpulseaudio`, `pipewire`, `libjack2`.
  - Доступность и IPC: `at-spi2-atk`, `at-spi2-core`, `dbus`, `glib`.
- **Звуковыми банками SoundFont**: Автоматическое подключение `soundfont-fluid` (`FluidR3_GM2-2.sf2`) через переменную `ROBUST_SOUNDFONT_OVERRIDE`.
- **Интеграцией с рабочим столом**: Автоматическая генерация `.desktop` файла и иконок приложения с помощью `makeDesktopItem` и `copyDesktopItems`.

---

## 🔄 Порядок обновления пакета Nix

При выходе новой версии лаунчера:

### Шаг 1: Обновление версии и хэша Git
1. Откройте `nix/package.nix`.
2. Измените поле `version` (например, `version = "1.2.5";`).
3. Задайте пустой хэш `hash = ""` внутри `fetchFromGitHub` или вычислите его через `nix-prefetch-github`:
   ```bash
   nix-prefetch-github space-wizards SS14.Launcher --rev v1.2.5 --fetch-submodules
   ```
4. Вставьте полученный SHA-256 хэш в `package.nix`.

### Шаг 2: Перегенерация зависимостей NuGet (`deps.json`)
Nix требует детерминированный список NuGet-пакетов. Сгенерируйте его из корня проекта:
```bash
nix run .#fetch-deps nix/deps.json
```

### Шаг 3: Сборка и тестирование в песочнице Nix
Запустите компиляцию пакета в изолированном окружении:
```bash
nix build .
```

### Шаг 4: Проверка работоспособности
Запустите собранный бинарник по ссылке на хранилище Nix Store:
```bash
./result/bin/space-station-14-launcher
```

---

## 💡 Подключение в конфигурацию NixOS

Для добавления пакета в общесистемный профиль `configuration.nix` или профиль `home-manager`:

```nix
# В configuration.nix или home.nix
{ pkgs, ... }:
{
  environment.systemPackages = [
    (pkgs.callPackage ./nix/package.nix { })
  ];
}
```
