#!/usr/bin/env bash
set -e

# Определение каталога с лаунчером
LAUNCHER_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_PATH="$LAUNCHER_DIR/SS14.Launcher"
ICON_PATH="$LAUNCHER_DIR/SS14.png"

echo "=== Установка ярлыков Space Station 14 Launcher ==="
echo "Каталог лаунчера: $LAUNCHER_DIR"

if [ ! -f "$BIN_PATH" ]; then
    echo "Ошибка: не найден бинарник $BIN_PATH"
    exit 1
fi

# Делаем скрипты исполняемыми
chmod +x "$BIN_PATH" 2>/dev/null || true
if [ -f "$LAUNCHER_DIR/bin_x64/SS14.Launcher" ]; then
    chmod +x "$LAUNCHER_DIR/bin_x64/SS14.Launcher" 2>/dev/null || true
fi
if [ -f "$LAUNCHER_DIR/bin_x64/loader/SS14.Loader" ]; then
    chmod +x "$LAUNCHER_DIR/bin_x64/loader/SS14.Loader" 2>/dev/null || true
fi

# Установка иконки в системную директорию
ICONS_DIR="$HOME/.local/share/icons/hicolor/256x256/apps"
mkdir -p "$ICONS_DIR"
if [ -f "$ICON_PATH" ]; then
    cp -f "$ICON_PATH" "$ICONS_DIR/SS14.png"
    echo "✔ Иконка установлена в $ICONS_DIR/SS14.png"
fi

# Создание .desktop файла для меню приложений
APPS_DIR="$HOME/.local/share/applications"
mkdir -p "$APPS_DIR"
DESKTOP_FILE="$APPS_DIR/SS14.desktop"

cat > "$DESKTOP_FILE" << EOF
[Desktop Entry]
Type=Application
Version=1.5
Name=Space Station 14 Launcher
Name[ru]=Лаунчер Space Station 14
GenericName=Space Station 14 Launcher
GenericName[ru]=Лаунчер Space Station 14
Comment=A multiplayer disaster simulator
Comment[ru]=Многопользовательский симулятор космической станции
Icon=$ICONS_DIR/SS14.png
Exec="$BIN_PATH" %u
Path=$LAUNCHER_DIR
Categories=Game;
Keywords=game;gaming;launcher;multiplayer;ss14;
StartupNotify=true
StartupWMClass=SS14.Launcher
SingleMainWindow=true
Terminal=false
PrefersNonDefaultGPU=false
EOF

chmod +x "$DESKTOP_FILE"
echo "✔ Ярлык добавлен в меню приложений ($DESKTOP_FILE)"

# Определение каталога рабочего стола через xdg-user-dir или дефолтные пути
DESKTOP_DIR=""
if command -v xdg-user-dir >/dev/null 2>&1; then
    DESKTOP_DIR="$(xdg-user-dir DESKTOP 2>/dev/null || true)"
fi
if [ -z "$DESKTOP_DIR" ] || [ ! -d "$DESKTOP_DIR" ]; then
    if [ -d "$HOME/Рабочий стол" ]; then
        DESKTOP_DIR="$HOME/Рабочий стол"
    elif [ -d "$HOME/Desktop" ]; then
        DESKTOP_DIR="$HOME/Desktop"
    fi
fi

if [ -n "$DESKTOP_DIR" ] && [ -d "$DESKTOP_DIR" ]; then
    DESKTOP_SHORTCUT="$DESKTOP_DIR/SS14.desktop"
    cp -f "$DESKTOP_FILE" "$DESKTOP_SHORTCUT"
    chmod +x "$DESKTOP_SHORTCUT"
    
    # Доверие ярлыку в KDE / GNOME
    if command -v gio >/dev/null 2>&1; then
        gio set "$DESKTOP_SHORTCUT" metadata::trusted true 2>/dev/null || true
    fi
    echo "✔ Ярлык создан на рабочем столе ($DESKTOP_SHORTCUT)"
fi

# Обновление кэша рабочего стола и иконок
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$APPS_DIR" 2>/dev/null || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -f -t "$HOME/.local/share/icons/hicolor" 2>/dev/null || true
fi

echo ""
echo "✨ Готово! Space Station 14 Launcher успешно интегрирован в систему."
