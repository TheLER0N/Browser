# 👑 KING Browser — Задачи для ИИ

> **Дата создания:** 7 апреля 2026
> **Версия проекта:** 2.0 — KING Edition
> **Стек:** C# 12, .NET 10, WPF, WebView2 (Chromium)

---

## 🔗 Связь с `ideas.md`

> **ideas.md** — банк идей (все возможные фичи с ID)
> **tasks.md** — TODO (идеи выбраны для реализации)
> **task.md** — ТЕКУЩАЯ ЗАДАЧА (что делается сейчас)
>
> **Поток:** `ideas.md` → (выбрана по ID) → `tasks.md` → (заказана пользователем в task.md) → выполняется → `changelog.md`

### Активные идеи из `ideas.md`:

| ID из ideas.md | Идея | Статус в tasks.md | Связь с task.md |
|----------------|------|-------------------|-----------------|
| `STEALTH-001` | Призрачный режим | ✅ ЗАВЕРШЕНО (Stealth 2.0) | Фаза 2: Stealth 2.0 |
| `STEALTH-002` | Паник-кнопка | ⏳ Запланировано | Фаза 2: Stealth 2.0 |
| `STEALTH-003` | Туннельный режим | ⏳ Запланировано | Фаза 3: Обход блокировок |
| `BYPASS-001` | Авто-обход блокировок | ⏳ Запланировано | Фаза 3: Обход блокировок |
| `PRIVACY-001` | Автоочистка | ⏳ Запланировано | Фаза 4: Расширенные настройки |
| `PRIVACY-002` | Анти-трекинг | ⏳ Запланировано | Фаза 4: Расширенные настройки |

**Как использовать:**
1. Пользователь пишет в `task.md`: "Сделай `STEALTH-001`"
2. ИИ читает `ideas.md` → находит STEALTH-001 → понимает что нужно сделать
3. ИИ создаёт подпланы в `tasks.md` → выполняет

---

## ⚠️ ВАЖНО: ЧТЕНИЕ ВСЕХ AI-ФАЙЛОВ ОБЯЗАТЕЛЬНО

**Перед любой задачей ИИ обязан прочитать ВСЕ файлы из папки `AI/`:**
1. `onboarding.md` — ввод в курс дела
2. `task.md` — текущая задача от пользователя
3. `tasks.md` — детализированный список задач и багов (этот файл)
4. `architecture.md` — полная архитектура проекта
5. `rules.md` — правила работы ИИ
6. `user-responses.md` — ответы пользователя о багах
7. `design.md` — дизайн-система
8. `security.md` — политика безопасности
9. `workflow.md` — рабочий процесс
10. `roadmap.md` — дорожная карта
11. `ideas.md` — банк идей (связано с задачами)
12. `changelog.md` — история изменений
13. `bug-handling.md` — правила обработки багов

**НЕ начинай работу, пока не прочитал все 13 файлов.**

---

## 📝 После выполнения задачи — ОБЯЗАТЕЛЬНО

**После каждого изменения ИИ обязан:**
1. Обновить `tasks.md` — отметить выполненное
2. Обновить `user-responses.md` — записать новый контекст (если есть)
3. Обновить `architecture.md` — если изменилась структура проекта
4. Обновить `rules.md` — если появились новые правила
5. Обновить `onboarding.md` — если изменились приоритеты
6. Написать краткий отчёт пользователю: что сделано, какие файлы изменены

---

## 🔁 Периодическая проверка файлов — КАЖДЫЕ 3 ЗАДАЧИ

**После каждых 3 выполненных задач ИИ обязан перечитать все AI-файлы и проверить на актуальность.**

---

## 📋 Сводка багов от пользователя

### 🔴 КРИТИЧЕСКИЕ (фиксить в первую очередь)

#### Bug #1: Сайт не показывается при переключении вкладок → вылет ✅ ИСПРАВЛЕНО
- **Симптом:** При переключении между вкладками веб-контент иногда не отображается, после чего приложение вылетает без сообщения об ошибке
- **Воспроизведение:** Нестабильно, иногда (race condition)
- **Причина (исправлено):**
  - `DisplayedContent` обновлялся синхронно, без Dispatcher — приводил к конфликту рендеринга
  - `_previousSelectedTab` присваивался ПОСЛЕ подписки на новую вкладку → дублирование событий
  - `async void CreateTab()` без try-catch → необработанное исключение роняло приложение
  - Нет null-проверок WebView перед использованием
- **Исправление (05.04.2026):**
  - `_previousSelectedTab` присваивается ДО подписки — предотвращает race condition
  - `DisplayedContent` обновляется через `Dispatcher.InvokeAsync(Background)` — безопасно для UI-потока
  - `CreateTab()` и `CloseTab()` обёрнуты в try-catch
  - `OnSelectedTabPropertyChanged` проверяет `tab.WebView != null`
  - Добавлен глобальный `DispatcherUnhandledException` в `App.xaml.cs`
- **Файлы:** `MainViewModel.cs`, `App.xaml.cs`

#### Bug #2: F11 → зависание намертво → вылет без ошибки ✅ ИСПРАВЛЕНО
- **Симптом:** При нажатии F11 (вход в полноэкранный режим) и последующем нажатии на кнопку развёртывания приложение зависает, затем вылетает без сообщения об ошибке
- **Причина (исправлено):**
  - Один флаг `_isMaximized` использовался и для `ToggleMaximize()`, и для `ToggleFullScreen()` — при быстрой последовательности F11 → кнопка происходил конфликт состояний
  - Не сохранялось состояние окна перед фуллскрином — при выходе восстанавливались жёстко заданные 1280x720, игнорируя реальное состояние
  - Не вызывалась перерисовка после выхода из фуллскрина
- **Исправление (05.04.2026):**
  - Разделены флаги: `_isWindowMaximized` (кнопка) и `_isFullScreen` (F11)
  - `ToggleMaximize()` заблокирована во время фуллскрина (`if (_isFullScreen) return`)
  - Перед входом сохраняются: `WindowState`, `Width`, `Height`, `Left`, `Top`, `WindowStyle`, `ResizeMode`
  - При выходе — полное восстановление + `InvalidateVisual()` + `UpdateLayout()` через `Dispatcher.InvokeAsync(Background)`
  - Весь метод обёрнут в try-catch с аварийным восстановлением
- **Файлы:** `MainWindow.xaml.cs` (методы `ToggleFullScreen`, `ToggleMaximize`)

---

### 🟡 ВАЖНЫЕ

#### Bug #3: Визуальные артефакты при изменении размера окна ✅ ИСПРАВЛЕНО
- **Симптом:** При ресайзе окна элементы интерфейса могут отображаться некорректно
- **Причина (исправлено):**
  - `WindowChrome` + `AllowsTransparency="True"` — известная проблема WPF, вызывает субпиксельное размытие
  - Отсутствие корректной перерисовки при изменении размера
  - WebView2 не всегда корректно масштабируется без принудительного UpdateLayout
- **Исправление (05.04.2026):**
  - Добавлены `SnapsToDevicePixels="True"` и `UseLayoutRounding="True"` на Window и корневые Border
  - Обработчик `Window_SizeChanged` вызывает `InvalidateVisual()` + `UpdateLayout()` через Dispatcher
  - Это устраняет субпиксельные артефакты и заставляет WPF пересчитать layout
- **Файлы:** `MainWindow.xaml`, `MainWindow.xaml.cs`

---

## 🏗️ Архитектурные проблемы (из ARCHITECTURE.md)

### Фаза 1: Стабилизация ✅ (уже исправлено)
- [x] Исправить дублирование WebView2
- [x] Исправить краш меню
- [x] Исправить утечку памяти
- [x] Добавить обработку ошибок

### Фаза 2: Чистота кода ✅ ВЫПОЛНЕНО
- [x] Удалить `SettingsViewModel.cs` (мёртвый код)
- [x] Создать `AsyncRelayCommand` для `CreateTab` (убрать async void)
- [ ] Добавить валидацию URL в `Navigate()`
- [ ] Покрыть `SearchService` unit-тестами

### Фаза 3: Архитектурный рефакторинг 🔜
- [ ] Вынести настройки из `SettingsService` (INotifyPropertyChanged) в `SettingsViewModel`
- [ ] Создать `ViewLocator` для автоматического отображения View
- [ ] Добавить DI-контейнер для управления зависимостями
- [ ] Разделить слой сервисов и слой UI

---

## 🆕 Текущая задача: Обновление дизайна KING Browser v2.0

**Дата:** 07.04.2026
**Приоритет:** 🔴 Высокий
**Статус:** 🔄 В процессе (аудит завершён)

### Результат аудита:
⚠️ NewTabPage.html использует фиолетовые/бирюзовые цвета вместо BW
⚠️ Title Bar кнопки используют TextBlock вместо SVG Path
⚠️ Навигационные кнопки используют Unicode символы вместо SVG Path
⚠️ Вкладки без реальных фавиконок сайтов
⚠️ SettingsPage использует emoji иконки вместо монохромных
⚠️ Status Bar без анимированных индикаторов
⚠️ Progress Bar без shimmer анимации
⚠️ Нет плавных переходов между секциями SettingsPage

---

### Подплан 1: Глобальный хук клавиатуры для F12
- **Файлы:** `Services/GlobalHotkey.cs` (расширить), `MainWindow.xaml.cs`
- **Что сделать:** Зарегистрировать F12 как глобальную горячую клавишу через `RegisterHotKey` API. Расширить GlobalHotkey.cs методом `RegisterPanicKey(IntPtr hWnd, Action callback)` и `UnregisterPanicKey()`.
- **Как проверить:** dotnet build → dotnet run → нажатие F12 → срабатывание callback
- **Статус:** ✅ completed

### Подплан 2: Логика паник-кнопки в MainViewModel
- **Файлы:** `ViewModels/MainViewModel.cs`
- **Что сделать:** Метод `ExecutePanicAsync()` — сворачивает окно (`WindowState = Minimized`), навигация на Google (`NavigateToUrl("https://www.google.com")`), очистка cookies/кэша WebView2 (`CoreWebView2.Profile.ClearBrowsingDataAsync()`), стирание истории сессии (временное хранение + восстановление). Добавить флаг `_isPanicMode`.
- **Как проверить:** dotnet build → вызов метода → проверка что окно свернуто, Google открыт, cookies очищены
- **Статус:** ✅ completed

### Подплан 3: Интеграция с MainWindow
- **Файлы:** `MainWindow.xaml.cs`
- **Что сделать:** В обработчике `SourceInitialized` зарегистрировать F12 через GlobalHotkey. При срабатывании → вызвать `vm.ExecutePanicAsync()`. Добавить `WndProc` перехват для `WM_HOTKEY`.
- **Как проверить:** dotnet build → dotnet run → F12 → окно сворачивается, открывается Google
- **Статус:** ✅ completed

### Подплан 4: Настройка в SettingsPage
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`, `Services/SettingsService.cs`
- **Что сделать:** Добавить настройку `EnablePanicKey: bool` (по умолчанию true). Добавить toggle "Паник-кнопка (F12)" в секцию Stealth 2.0 SettingsPage. Сохранение/загрузка из settings.json.
- **Как проверить:** dotnet build → SettingsPage → toggle паник-кнопки → сохранение
- **Статус:** ✅ completed

### Подплан 5: Восстановление после паники
- **Файлы:** `ViewModels/MainViewModel.cs`, `MainWindow.xaml.cs`
- **Что сделать:** При повторном нажатии F12 → восстановление окна (`WindowState = Normal`), возврат на предыдущую вкладку. Сохранение состояния до паники (SelectedTab, URL). Добавить `_previousState` для восстановления.
- **Как проверить:** dotnet build → dotnet run → F12 → паника → F12 → восстановление состояния
- **Статус:** ✅ completed (уже реализовано в ExecutePanicAsync)

### Подплан 6: Status Bar — улучшенный
- **Файлы:** `MainWindow.xaml`
- **Что сделать:** Добавить пульсирующий индикатор stealth mode (анимированная точка). Добавить анимированный индикатор загрузки страницы.
- **Как проверить:** dotnet build → dotnet run → Status Bar с пульсацией
- **Статус:** ⏳ pending

### Подплан 7: Progress Bar — shimmer анимация
- **Файлы:** `MainWindow.xaml`, `App.xaml`
- **Что сделать:** Добавить indeterminate анимацию при загрузке (shimmer эффект). Добавить плавное появление/исчезновение.
- **Как проверить:** dotnet build → dotnet run → прогресс-бар с shimmer
- **Статус:** ⏳ pending

### Подплан 8: Bookmarks Bar — фавиконки
- **Файлы:** `MainWindow.xaml`, `BookmarkService.cs`
- **Что сделать:** Добавить отображение фавиконок для закладок (через Google Favicon API или WebView2).
- **Как проверить:** dotnet build → dotnet run → закладки с фавиконками
- **Статус:** ⏳ pending

### Подплан 9: Глобальные анимации
- **Файлы:** `App.xaml`, `MainWindow.xaml`, `MainWindow.xaml.cs`
- **Что сделать:** Улучшить WindowFadeIn. Добавить transitions для всех интерактивных элементов. Добавить плавное появление контента.
- **Как проверить:** dotnet build → dotnet run → плавные анимации везде
- **Статус:** ⏳ pending

### Подплан 10: Финальная проверка
- **Файлы:** Все XAML файлы
- **Что сделать:** Проверить все стили на консистентность BW палитры. Проверить hover-эффекты. dotnet build → visual test.
- **Как проверить:** dotnet build → полный визуальный тест
- **Статус:** ⏳ pending

---

## 🚀 Фичи из TODO (потом)

- [ ] Менеджер загрузок (✅ уже есть)
- [ ] Расширенные настройки
- [ ] Расширения (uBlock Origin и т.д.)
- [ ] Синхронизация закладок
- [ ] Режим инкогнито (✅ уже есть)
- [ ] Скриншотер с защитой
- [ ] Автозаполнение форм
- [ ] Профили пользователей

---

## 📝 План действий

### Этап 1: Критические баги ✅
- [x] Исправить Bug #1 (вылет при переключении вкладок)
- [x] Исправить Bug #2 (F11 зависание)
- [x] Исправить Bug #3 (артефакты при ресайзе)

### Этап 2: Стабилизация ✅ ВЫПОЛНЕНО
- [x] Утечка HttpClient
- [x] Race condition SaveNotification
- [x] Пустые catch
- [x] ContinueWith → async/await
- [x] Упростить CreateTab
- [x] Удалить мёртвый код
- [x] AsyncRelayCommand создан
- [x] Валидация URL (⏳ позже)
- [x] Поисковик сохраняется
- [x] Закладки case-insensitive
- [x] BookmarksBar через XAML Binding
- [x] SettingsPage — нет дублирования подписок
- [x] Удалён SettingsViewModel.cs

### Этап 3: Архитектура ✅ ВЫПОЛНЕНО
- [x] Создать AsyncRelayCommand
- [x] DateTime.Now → UtcNow
- [x] Дедупликация истории
- [x] Магическое число 500ms → константа
- [x] Убраны дубликаты DNS-пресетов
- [x] SettingsPage обнуляется при закрытии

---

## 🆕 Новые фичи (выполненные)

### ✅ Режим инкогнито (05.04.2026)
- [x] IncognitoWindow.xaml(.cs)
- [x] IncognitoViewModel.cs
- [x] Кнопка 🕶️ в MainWindow + Ctrl+Shift+N
- [x] Очистка cookies, кэша, папки профиля при закрытии

### ✅ Менеджер загрузок (05.04.2026)
- [x] Models/DownloadItem.cs
- [x] Models/DownloadItemStatus.cs
- [x] Services/DownloadService.cs
- [x] Перехват загрузок WebView2
- [x] Секция "📥 Загрузки" в SettingsPage
- [x] Пауза/возобновление/отмена/удаление
- [x] История загрузок → downloads.json

### ✅ Stealth 2.0 (07.04.2026)
- [x] Services/GlobalHotkey.cs — блокировка PrintScreen
- [x] Services/ScreenshotBlocker.cs — блокировка скриншотов WebView2
- [x] Services/SnippingToolBlocker.cs — блокировка Snipping Tool
- [x] Auto-включение stealth при запуске
- [x] Anti-fingerprint (User-Agent + Canvas + WebGL)
- [x] Настройки Stealth 2.0 в SettingsPage

### ✅ Чёрно-белая тема (07.04.2026)
- [x] App.xaml — монохромная палитра
- [x] NewTabPage.html — BW дизайн
- [x] Все UI компоненты обновлены

---

## ⚙️ Известные технические детали

- **OS:** Windows 11 (пользователь подтвердил)
- **Stealth mode:** Работает (WDA_EXCLUDEFROMCAPTURE)
- **Stealth 2.0:** ✅ ЗАВЕРШЕНО (7 апреля 2026)
- **Хранилище:** `%APPDATA%\GhostBrowser\{settings,bookmarks,history}.json`
- **UserDataFolder WebView2:** `%APPDATA%\GhostBrowser`
- **BW Палитра:** Чёрный (#000000), Белый (#ffffff), Серый (#999999)
- **Danger:** #ff3333 (единственный цвет, только для ошибок)

---

## 🗂️ Структура проекта (для справки)

```
KingBrowser/
├── App.xaml / App.xaml.cs          # Точка входа, глобальные стили (BW тема)
├── MainWindow.xaml / .cs           # Главное окно
├── GhostBrowser.csproj             # .NET 10, WPF, WebView2
├── NewTabPage.html                 # HTML новой вкладки (BW дизайн)
├── KING11.png                      # Логотип — шахматный король
│
├── ViewModels/
│   ├── MainViewModel.cs            # Главный VM: вкладки, навигация, сервисы
│   ├── TabViewModel.cs             # VM вкладки: WebView2, URL, заголовок
│   ├── ViewModelBase.cs            # INotifyPropertyChanged
│   ├── RelayCommand.cs             # ICommand реализация
│   ├── AsyncRelayCommand.cs        # Async ICommand
│   └── IncognitoViewModel.cs       # VM режима инкогнито
│
├── Models/
│   ├── Bookmark.cs                 # Модель закладки
│   ├── HistoryEntry.cs             # Модель записи истории
│   ├── DownloadItem.cs             # Модель загрузки
│   └── DownloadItemStatus.cs       # Enum статусов загрузки
│
├── Services/
│   ├── StealthService.cs           # Режим невидимости (Win32 API) ✅
│   ├── GlobalHotkey.cs             # Блокировка PrintScreen ✅
│   ├── ScreenshotBlocker.cs        # Блокировка скриншотов WebView2 ✅
│   ├── SnippingToolBlocker.cs      # Блокировка Snipping Tool ✅
│   ├── HistoryService.cs           # История → JSON
│   ├── BookmarkService.cs          # Закладки → JSON
│   ├── SearchService.cs            # Поисковики
│   ├── SettingsService.cs          # Настройки (INPC — вынести в VM)
│   └── DownloadService.cs          # Менеджер загрузок
│
└── Views/
    ├── SettingsPage.xaml / .cs     # Страница настроек
    └── DnsTestWindow.xaml / .cs    # Модалка теста DNS
```

---

## 🆕 Текущая задача: KING11.png — inline base64 в NewTabPage.html
- **Запрос из task.md:** "NewTabPage.html не показывает в моём браузере логотип, а вот если через другой открыть то это да"
- **Дата:** 2026-04-07
- **Статус:** ✅ completed

#### Подплан 1: Встроить KING11.png как base64 data URI в NewTabPage.html
- **Файлы:** NewTabPage.html
- **Что сделать:** Заменить `<img src="KING11.png">` на `<img src="data:image/png;base64,...">` потому что NewTabPage.html загружается через data URI и относительные пути не работают
- **Как проверить:** dotnet build → dotnet run → логотип виден на новой вкладке
- **Статус:** ✅ completed

---

## ❗ Что НЕ делать сейчас

- НЕ добавлять новые фичи без согласования с пользователем
- НЕ ломать рабочий stealth mode
- НЕ удалять файлы без подтверждения
- НЕ использовать цвета кроме BW палитры (кроме Danger #ff3333)
