## 📋 Описание изменений / Change Description

<!-- Кратко опишите суть изменений и проблему, которую они решают. -->
<!-- Briefly describe the changes and the problem they solve. -->

---

## 🔍 Чек-лист качества / Code Quality Checklist

- [ ] **Компиляция / Build**: Код компилируется без предупреждений и ошибок (`dotnet build -warnaserror:CS4014`).
- [ ] **Тестирование / Tests**: Все 225+ модульных и Headless UI-тестов пройдены успешно (`dotnet test`).
- [ ] **Покрытие кода / Code Coverage**: Изменения покрыты автотестами, покрытие не снижается.
- [ ] **Локализация / Localization**: Все новые строки переведены на русский и английский (100% паритет ключей в `ru/text.ftl` и `en-US/text.ftl`).
- [ ] **Архитектура / Architecture**: Зависимости зарегистрированы через интерфейсы (`IDataManager`, `IUpdater`, `ILoginManager`, `IThemeService`).
- [ ] **История изменений / Changelog**: Добавлена запись в `CHANGELOG.md` в секцию `[Unreleased]`.
- [ ] **Форматирование / Formatting**: Соблюдаются правила `.editorconfig` (`dotnet format --verify-no-changes`).

---

## 🧪 Как проверялось / How Tested

<!-- Опишите сценарии ручного или автоматического тестирования. -->
