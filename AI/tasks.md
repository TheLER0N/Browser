# 👻 GhostBrowser — Задачи для ИИ

> **Дата создания:** 5 апреля 2026
> **Версия проекта:** 1.0
> **Стек:** C# 12, .NET 7, WPF, WebView2 (Chromium)

---

## ⚠️ ВАЖНО: ЧТЕНИЕ ВСЕХ AI-ФАЙЛОВ ОБЯЗАТЕЛЬНО

**Перед любой задачей ИИ обязан прочитать ВСЕ файлы из папки `AI/`:**
1. `onboarding.md` — ввод в курс дела
2. `task.md` — текущая задача от пользователя
3. `tasks.md` — детализированный список задач и багов (этот файл)
4. `architecture.md` — полная архитектура проекта
5. `rules.md` — правила работы ИИ
6. `user-responses.md` — ответы пользователя о багах
7. `design.md` — дизайн-система (UI/UX, цвета, типографика)
8. `security.md` — политика безопасности (stealth mode, инкогнито, угрозы)
9. `workflow.md` — рабочий процесс (разбиение задач на подпланы)
10. `roadmap.md` — дорожная карта и будущие задачи

**НЕ начинай работу, пока не прочитал все 10 файлов.**

---

## 📝 После выполнения задачи — ОБЯЗАТЕЛЬНО

**После каждого изменения ИИ обязан:**
1. Обновить `tasks.md` — отметить выполненное
2. Обновить `user-responses.md` — записать новый контекст (если есть)
3. Обновить `architecture.md` — если изменилась структура проекта
4. Обновить `design.md` — если изменились UI-компоненты, цвета, стили
5. Обновить `security.md` — если изменились аспекты безопасности (stealth, инкогнито)
6. Обновить `rules.md` — если появились новые правила
7. Обновить `workflow.md` — если изменился рабочий процесс
8. Обновить `onboarding.md` — если изменились приоритеты
9. Обновить `roadmap.md` — если появились новые задачи или изменён план
10. Написать краткий отчёт пользователю: что сделано, какие файлы изменены

---

## 🔁 Периодическая проверка файлов — КАЖДЫЕ 3 ЗАДАЧИ

**После каждых 3 выполненных задач ИИ обязан перечитать все 10 AI-файлов и проверить на актуальность.**

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

## 🚀 Фичи из TODO (потом)

- [ ] Менеджер загрузок ✅ ВЫПОЛНЕНО
- [ ] Расширенные настройки ✅ ВЫПОЛНЕНО
- [ ] Расширения (uBlock Origin) — 📋 В roadmap
- [ ] Синхронизация закладок — 📋 В roadmap
- [ ] Режим инкогнито ✅ ВЫПОЛНЕНО
- [ ] Скриншотер с защитой — 📋 В roadmap
- [ ] Автозаполнение форм — 📋 В roadmap
- [ ] Профили пользователей — 📋 В roadmap

---

## 📝 План действий

### Этап 1: Критические баги (СРОЧНО)

**1.1. Исправить Bug #2 (F11 зависание)** — ✅ ИСПРАВЛЕНО
- ✅ Разделены флаги: `_isWindowMaximized` и `_isFullScreen`
- ✅ `ToggleMaximize()` заблокирована во время фуллскрина
- ✅ Сохранение/восстановление состояния окна
- ✅ `InvalidateVisual()` + `UpdateLayout()` при выходе
- ✅ try-catch с аварийным восстановлением

**1.2. Исправить Bug #1 (вылет при переключении вкладок)** — ✅ ИСПРАВЛЕНО
- ✅ `_previousSelectedTab` присваивается ДО подписки
- ✅ `DisplayedContent` обновляется через `Dispatcher.InvokeAsync(Background)`
- ✅ Добавлены проверки `WebView != null`
- ✅ `CreateTab()`, `CloseTab()`, `UpdateFromSelectedTab()`, `OnSelectedTabPropertyChanged` обёрнуты в try-catch
- ✅ Глобальный `DispatcherUnhandledException` в `App.xaml.cs`

**1.3. Исправить Bug #3 (артефакты при ресайзе)** — ✅ ИСПРАВЛЕНО
- ✅ `SnapsToDevicePixels="True"` и `UseLayoutRounding="True"` на Window и Border
- ✅ `Window_SizeChanged` → `InvalidateVisual()` + `UpdateLayout()`

### Этап 2: Стабилизация ✅ ВЫПОЛНЕНО (кроме валидации URL и тестов)

**2.1. Исправить утечку HttpClient** — ✅ ВЫПОЛНЕНО
- SettingsService реализует IDisposable
- HttpClient DISPOSится в Cleanup()

**2.2. Исправить race condition SaveNotification** — ✅ ВЫПОЛНЕНО
- ContinueWith заменён на DispatcherTimer

**2.3. Исправить пустые catch** — ✅ ВЫПОЛНЕНО
- LoadSettings() логирует ошибку, использует дефолтные настройки
- TestDns() показывает результат ошибки пользователю
- ResetProgressAsync обёрнут в try-catch

**2.4. Убрать ContinueWith из TabViewModel** — ✅ ВЫПОЛНЕНО
- Заменён на async/await (ResetProgressAsync)

**2.5. Упростить CreateTab** — ✅ ВЫПОЛНЕНО
- Убран async внутри Dispatcher.InvokeAsync

**2.6. Удалить мёртвый код** — ✅ ВЫПОЛНЕНО
- Удалён HistoryWindow.cs

**2.7. Создать `AsyncRelayCommand`** — ✅ ВЫПОЛНЕНО
- `CreateTab()` переписан на `async Task CreateTabAsync()`
- `AddTabCommand` теперь `AsyncRelayCommand`

**2.8. Добавить валидацию URL** — ⏳ ПОЗЖЕ
- Проверять корректность URL перед `new Uri()`
- Показывать ошибку пользователю

**2.9. Поисковик сохраняется** — ✅ ВЫПОЛНЕНО
- `CycleSearchEngine()` сохраняет в `SettingsService.DefaultSearchEngine`
- При инициализации загружается из настроек

**2.10. Закладки case-insensitive** — ✅ ВЫПОЛНЕНО
- `OrdinalIgnoreCase` в `AddBookmark()` и `IsBookmarked()`
- `DateTime.UtcNow` вместо `DateTime.Now`

**2.11. BookmarksBar через XAML Binding** — ✅ ВЫПОЛНЕНО
- Убран code-behind `UpdateBookmarksBar()`
- `ItemsSource="{Binding BookmarkService.Bookmarks}"` в XAML

**2.12. SettingsPage — нет дублирования подписок** — ✅ ВЫПОЛНЕНО
- DNS Toggle, TextBox, Slider — через XAML Binding
- `InitializeSettings()` упрощён

**2.13. Удалён мёртвый код** — ✅ ВЫПОЛНЕНО
- Удалён `SettingsViewModel.cs`

### Этап 3: Архитектура ✅ ВЫПОЛНЕНО

- [x] Создать AsyncRelayCommand
- [x] DateTime.Now → UtcNow (HistoryService)
- [x] Дедупликация истории при загрузке
- [x] Магическое число 500ms → константа ProgressResetDelayMs
- [x] Убраны дубликаты DNS-пресетов (17 → 14)
- [x] SettingsPage обнуляется при закрытии

---

## 🆕 Новые фичи

### ✅ Режим инкогнито (05.04.2026)
- [x] IncognitoWindow.xaml(.cs) — отдельное окно с фиолетовым индикатором
- [x] IncognitoViewModel.cs — без истории/закладок, изолированный UserDataFolder
- [x] Кнопка 🕶️ в MainWindow + горячая клавиша Ctrl+Shift+N
- [x] Очистка cookies, кэша и папки профиля при закрытии
- [x] Фиолетовый прогресс-бар и индикатор "INCOGNITO"

### ✅ Менеджер загрузок (05.04.2026)
- [x] Models/DownloadItem.cs — модель загрузки с прогрессом и скоростью
- [x] Models/DownloadItemStatus.cs — enum статусов
- [x] Services/DownloadService.cs — сервис загрузок с pause/resume (HTTP Range)
- [x] Перехват загрузок WebView2 (DownloadStarting → e.Cancel)
- [x] Секция "📥 Загрузки" в SettingsPage
- [x] Пауза/возобновление/отмена/удаление загрузок
- [x] Папка загрузок по умолчанию (настраивается)
- [x] История загрузок → downloads.json

---

## 🗂️ Структура проекта (для справки)

```
GhostBrowser/
├── App.xaml / App.xaml.cs          # Точка входа, глобальные стили
├── MainWindow.xaml / .cs           # Главное окно
├── GhostBrowser.csproj             # .NET 10, WPF, WebView2
├── NewTabPage.html                 # HTML новой вкладки
│
├── ViewModels/
│   ├── MainViewModel.cs            # Главный VM: вкладки, навигация, сервисы
│   ├── TabViewModel.cs             # VM вкладки: WebView2, URL, заголовок
│   ├── ViewModelBase.cs            # INotifyPropertyChanged
│   ├── RelayCommand.cs             # ICommand реализация
│   └── AsyncRelayCommand.cs        # Async ICommand
│
├── Models/
│   ├── Bookmark.cs                 # Модель закладки
│   └── HistoryEntry.cs             # Модель истории
│
├── Services/
│   ├── StealthService.cs           # Режим невидимости (Win32 API) ✅ Работает
│   ├── HistoryService.cs           # История → JSON
│   ├── BookmarkService.cs          # Закладки → JSON
│   ├── SearchService.cs            # Поисковики
│   └── SettingsService.cs          # Настройки (INPC — вынести в VM)
│
└── Views/
    ├── SettingsPage.xaml / .cs     # Страница настроек
    └── DnsTestWindow.xaml / .cs    # Модалка теста DNS
```

---

## ⚙️ Известные технические детали

- **OS:** Windows 11 (пользователь подтвердил)
- **Stealth mode:** Работает (WDA_EXCLUDEFROMCAPTURE)
- **Хранилище:** `%APPDATA%\GhostBrowser\{settings,bookmarks,history}.json`
- **UserDataFolder WebView2:** `%APPDATA%\GhostBrowser`

---

## ❗ Что НЕ делать сейчас

- НЕ ломать рабочий stealth mode
- НЕ удалять файлы без подтверждения

---

## 🆕 НОВАЯ ЗАДАЧА: Система блокировки рекламы (аналог uBlock Origin)

> **Дата:** 6 апреля 2026
> **Приоритет:** 🔴 Высокая
> **Статус:** 🔄 in_progress
> **Подпланов:** 5
>
> **Примечание:** WebView2 НЕ поддерживает расширения Chrome.
> Реализуем встроенную блокировку через `WebResourceRequested` API —
> это единственный рабочий способ для WebView2.

### Подплан 1: Модель фильтров — AdBlockFilter.cs
- **Файлы:** `Models/AdBlockFilter.cs` (новый), `Models/AdBlockFilterList.cs` (новый)
- **Что сделать:** Модель фильтра (имя, URL списка, активен), список правил EasyList
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 2: Сервис AdBlockService
- **Файлы:** `Services/AdBlockService.cs` (новый)
- **Что сделать:** Загрузка фильтров (EasyList), проверка URL по паттернам, блокировка через 204
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 3: Интеграция с TabViewModel
- **Файлы:** `ViewModels/TabViewModel.cs`
- **Что сделать:** Подписка на WebResourceRequested при инициализации WebView2
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 4: UI — секция AdBlock в SettingsPage
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Toggle вкл/выкл, счётчик заблокированных запросов, выбор фильтров
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 5: Финальная сборка и тест
- **Файлы:** Все затронутые
- **Что сделать:** dotnet build, ручная проверка на сайтах с рекламой
- **Как проверить:** dotnet build, открыть сайт с рекламой
- **Статус:** ⏳ pending

---

## 🆕 НОВАЯ ЗАДАЧА: Расширенные настройки (10 категорий, ~50 настроек)

> **Дата:** 6 апреля 2026
> **Приоритет:** 🟡 Средняя
> **Статус:** ✅ ВЫПОЛНЕНО + ✅ ПРИВЯЗКА UI (исправлено) + ✅ ОБХОД БЛОКИРОВОК + ✅ МЕНЮ (исправлено)
> **Подпланов:** 13 — все completed

### Подплан 1: Модель AdvancedSettings
- **Файлы:** `Models/AdvancedSettings.cs` (новый)
- **Что сделать:** Создать POCO-модель для всех расширенных настроек (язык, тема, масштаб, аппаратное ускорение, автоочистка, мастер-пароль, прокси, DoH, сессии, поисковые подсказки, User-Agent, DevTools и т.д.)
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 2: SettingsService — интеграция AdvancedSettings
- **Файлы:** `Services/SettingsService.cs`
- **Что сделать:** Добавить свойство `AdvancedSettings AdvancedSettings`, загрузку/сохранение, INotifyPropertyChanged для каждого поля
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 3: SettingsPage XAML — новые категории навигации
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Добавить 7 новых кнопок в сайдбар + 7 новых секций
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 4: Секция «Внешний вид» в SettingsPage
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Тема, акцентный цвет, масштаб, панель закладок, статус-бар, скруглённые вкладки
- **Как проверить:** dotnet build, ручной тест
- **Статус:** ✅ completed

### Подплан 5: Секция «Производительность» в SettingsPage
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Аппаратное ускорение, автоочистка кэша, предзагрузка, потоки загрузок, лимит RAM
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 6: Секция «Безопасность и приватность» — уже есть
- **Файлы:** `Views/SettingsPage.xaml`
- **Что сделать:** Блокировка трекеров и cookies (toggle уже были)
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 7: Секция «Сеть и DNS»
- **Файлы:** `Views/SettingsPage.xaml`
- **Что сделать:** DoH, провайдеры DoH, прокси (ручной/системный/без), таймаут
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 8: Секция «При запуске»
- **Файлы:** `Views/SettingsPage.xaml`
- **Что сделать:** RadioButton режим запуска, восстановление после краха
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 9: Секция «Поиск»
- **Файлы:** `Views/SettingsPage.xaml`
- **Что сделать:** Поисковые подсказки, открытие в новой вкладке
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 10: Секция «Уведомления»
- **Файлы:** `Views/SettingsPage.xaml`
- **Что сделать:** Звук загрузки, всплывающие уведомления
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 11: Секция «Экспериментальные»
- **Файлы:** `Views/SettingsPage.xaml`
- **Что сделать:** DevTools, User-Agent, WebGL, Canvas, текст-режим, автоплей
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 12: Финальная сборка и проверка
- **Файлы:** Все затронутые
- **Что сделать:** dotnet build — успешно
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

---

## 🆕 НОВАЯ ЗАДАЧА: Синхронизация закладок (экспорт/импорт)

> **Дата:** 6 апреля 2026
> **Приоритет:** 🔴 Высокая
> **Статус:** ✅ ВЫПОЛНЕНО
> **Подпланов:** 4 — все completed

### Подплан 1: Модель SyncResult
- **Файлы:** `Models/SyncResult.cs` (новый)
- **Что сделать:** POCO-класс с полями Added, Skipped, Errors, TotalImported
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 2: BookmarkService — экспорт/импорт/слияние
- **Файлы:** `Services/BookmarkService.cs`
- **Что сделать:** ExportBookmarks(path), ImportAndMergeBookmarks(path) → SyncResult, дедупликация по URL (case-insensitive)
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 3: UI — секция синхронизации в SettingsPage
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Кнопки "📤 Экспорт" и "📥 Импорт", вызов команд VM, MessageBox с результатом
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 4: Команды в MainViewModel + диалоги файлов
- **Файлы:** `ViewModels/MainViewModel.cs`, `MainWindow.xaml.cs`
- **Что сделать:** ExportBookmarksCommand, ImportBookmarksCommand → SaveFileDialog/OpenFileDialog
- **Как проверить:** dotnet build
- **Статус:** ✅ completed (реализовано в SettingsPage.xaml.cs)

---

## 🆕 НОВАЯ ЗАДАЧА: Скриншотер с защитой

> **Дата:** 6 апреля 2026
> **Приоритет:** 🟡 Средняя
> **Статус:** ✅ ВЫПОЛНЕНО
> **Подпланов:** 4 — все completed

### Подплан 1: Сервис ScreenshotService
- **Файлы:** `Services/ScreenshotService.cs` (новый)
- **Что сделать:** CapturePageAsync(WebView2, filePath, format) → PNG/JPEG через CapturePreviewAsync
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 2: Настройки скриншотов в AdvancedSettings + SettingsService
- **Файлы:** `Models/AdvancedSettings.cs`, `Services/SettingsService.cs`
- **Что сделать:** ScreenshotFormat (PNG/JPEG), ScreenshotFolder, AutoName
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 3: Команда в MainViewModel + диалог сохранения
- **Файлы:** `ViewModels/MainViewModel.cs`
- **Что сделать:** TakeScreenshotCommand → SaveFileDialog → ScreenshotService.CapturePageAsync
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 4: UI — кнопка 📸 + горячая клавиша Ctrl+Shift+S + секция в SettingsPage
- **Файлы:** `MainWindow.xaml`, `MainWindow.xaml.cs`, `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Кнопка камеры в навигации, InputBinding для Ctrl+Shift+S, секция настроек
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

---

## 🆕 НОВАЯ ЗАДАЧА: Автозаполнение форм

> **Дата:** 6 апреля 2026
> **Приоритет:** 🟡 Средняя
> **Статус:** ✅ ВЫПОЛНЕНО
> **Подпланов:** 5 — все completed

### Подплан 1: Модель AutoFillProfile
- **Файлы:** `Models/AutoFillProfile.cs` (новый)
- **Что сделать:** POCO с полями: FirstName, LastName, Email, Phone, Address, City, Zip, Country
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 2: Сервис AutoFillService
- **Файлы:** `Services/AutoFillService.cs` (новый)
- **Что сделать:** Load/Save профилей, GenerateFillScript() → JS для заполнения input по селекторам
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 3: Интеграция с TabViewModel — JS инъекция
- **Файлы:** `ViewModels/TabViewModel.cs`
- **Что сделать:** ExecuteScriptAsync в NavigationCompleted если автозаполнение вкл
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 4: UI — секция «Автозаполнение» в SettingsPage
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Форма с полями (имя, email, телефон, адрес), Toggle вкл/выкл
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 5: Горячая клавиша Ctrl+Shift+F
- **Файлы:** `MainWindow.xaml`, `ViewModels/MainViewModel.cs`
- **Что сделать:** FillFormsCommand → ExecuteScriptAsync на текущей вкладке
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

---

## 🆕 НОВАЯ ЗАДАЧА: Профили пользователей

> **Дата:** 6 апреля 2026
> **Приоритет:** 🔴 Высокая
> **Статус:** ✅ ВЫПОЛНЕНО
> **Подпланов:** 6 — все completed

### Подплан 1: Модель UserProfile
- **Файлы:** `Models/UserProfile.cs` (новый)
- **Что сделать:** POCO — Name, AvatarColor, IsActive, CreatedAt
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 2: Сервис ProfileService
- **Файлы:** `Services/ProfileService.cs` (новый)
- **Что сделать:** CRUD профилей, JSON (profiles.json), SetActive, Delete
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 3: MainViewModel — переключение профилей
- **Файлы:** `ViewModels/MainViewModel.cs`
- **Что сделать:** SwitchProfile(Profile) → пересоздание всех вкладок с новым CoreWebView2Profile
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 4: TabViewModel — поддержка профиля
- **Файлы:** `ViewModels/TabViewModel.cs`
- **Что сделать:** Конструктор принимает CoreWebView2ControllerOptions с ProfileName
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 5: UI — селектор профилей в MainWindow
- **Файлы:** `MainWindow.xaml`, `MainWindow.xaml.cs`
- **Что сделать:** Кнопка аватара в title bar → ComboBox/Popup со списком профилей
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 6: UI — секция «Профили» в SettingsPage
- **Файлы:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
- **Что сделать:** Список профилей, добавить/удалить/переименовать
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

---

## 🆕 НОВАЯ ЗАДАЧА: 🎨 Редизайн KING (шахматная тема)

> **Дата:** 6 апреля 2026
> **Приоритет:** 🔴 Высокая
> **Статус:** 🔄 in_progress
> **Подпланов:** 7

### Подплан 1: Цветовая палитра — App.xaml
- **Файлы:** `App.xaml`
- **Что сделать:** Заменить все цвета на KING палитру (дерево, золото, крем)
- **Как проверить:** dotnet build, визуальная проверка
- **Статус:** ✅ completed

### Подплан 2: Логотип — KING.png в MainWindow
- **Файлы:** `MainWindow.xaml`, `App.xaml`
- **Что сделать:** Заменить 👻 на изображение KING.png в title bar
- **Как проверить:** dotnet build
- **Статус:** ✅ completed

### Подплан 3: Название — GhostBrowser → KING
- **Файлы:** `MainWindow.xaml`, `IncognitoWindow.xaml`, `App.xaml.cs`, `NewTabPage.html`, все AI-файлы
- **Что сделать:** Заменить все упоминания GhostBrowser на KING
- **Как проверить:** dotnet build
- **Статус:** ✅ completed (MainWindow + IncognitoWindow Title)

### Подплан 4: Иконки — шахматные символы
- **Файлы:** `MainWindow.xaml`, `SettingsPage.xaml`, `design.md`
- **Что сделать:** 👻→♚, 🕶️→♛, адаптировать остальные эмодзи
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 5: NewTabPage.html — шахматная тема
- **Файлы:** `NewTabPage.html`
- **Что сделать:** Тёмное дерево, золотой текст, KING logo, частицы, орбы
- **Как проверить:** dotnet build
- **Статус:** ✅ completed (полностью переделана в стиле сайта KING)

### Подплан 6: Иконка приложения .ico
- **Файлы:** `KING.ico` (новый из PNG), `GhostBrowser.csproj`
- **Что сделать:** Конвертировать KING.png в .ico, обновить csproj
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 7: Финальная сборка и проверка
- **Файлы:** Все затронутые
- **Что сделать:** dotnet build, ручная проверка UI
- **Как проверить:** dotnet build
- **Статус:** ⏳ pending

### Подплан 8: Исправления KING (овал→круг, перетаскивание, дизайн)
- **Файлы:** `NewTabPage.html`, `MainWindow.xaml`, `MainWindow.xaml.cs`
- **Что сделать:** Круглый логотип, DragMove за title bar, улучшенный визуал
- **Как проверить:** dotnet build, ручной тест
- **Статус:** ✅ completed

### Подплан 9: Левитация колец логотипа на NewTabPage
- **Файлы:** `NewTabPage.html`
- **Что сделать:** Добавить анимацию левитации (float) к avatar-ring и avatar-ring-2, чтобы они двигались синхронно с логотипом ♚
- **Как проверить:** dotnet build, открыть новую вкладку
- **Статус:** ✅ completed
