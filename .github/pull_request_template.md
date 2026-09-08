## 📝 Описание / Description

<!-- Кратко опишите, что делает этот PR и зачем / Briefly describe what this PR does and why -->

## 🔧 Изменения / Changes

<!-- Список ключевых изменений / List the specific changes made -->
- 

## 🧪 Тестирование / Testing

<!-- Как проверялись изменения? / How was this tested? -->
- [ ] Все модульные тесты успешно проходят / Unit tests pass (`dotnet test`)
- [ ] Сборка компилируется без ошибок и предупреждений / Build succeeds with 0 warnings
- [ ] Форматирование кода проверено / Code formatting verified (`dotnet format --verify-no-changes`)

## 📋 Чеклист качества / Quality Checklist

- [ ] **Error Handling**: Нет пустых `catch {}` блоков — все исключения типизированы и логируются через Serilog / No empty catch blocks, all exceptions are logged
- [ ] **Async Safety**: Нет `async void` вне UI-событий Avalonia / No `async void` outside UI event handlers
- [ ] **Task Discard**: Фоновые задачи (fire-and-forget) используют оператор сброса `_ = Method()` / Background tasks use `_ = Method()` discard pattern
- [ ] **Localization (i18n)**: Новые строки пользовательского интерфейса добавлены в `en-US` и `ru` `.ftl` файлы / UI strings added to both `en-US` and `ru` locale files
- [ ] **Documentation**: Публичные методы задокументированы XML-комментариями / Public methods documented with XML comments
