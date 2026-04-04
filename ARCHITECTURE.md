# 🏗️ Архитектура GhostBrowser

> **Версия:** 1.0  
> **Дата:** Апрель 2026  
> **Стек:** C# 12, WPF, .NET 10, WebView2 (Chromium)

---

## 📁 Структура проекта

```
GhostBrowser/
├── App.xaml / App.xaml.cs          # Точка входа, глобальные стили и ресурсы
├── MainWindow.xaml / .cs           # Главное окно: title bar, табы, навигация, контент
├── GhostBrowser.csproj             # Конфигурация проекта (.NET 10, WPF, WebView2)
├── NewTabPage.html                 # HTML-страница новой вкладки (копируется в output)
│
├── ViewModels/
│   ├── MainViewModel.cs            # Главный VM: управление вкладками, навигацией, сервисами
│   ├── TabViewModel.cs             # VM одной вкладки: WebView2, URL, заголовок, прогресс
│   ├── ViewModelBase.cs            # Базовый класс с INotifyPropertyChanged
│   ├── RelayCommand.cs             # Реализация ICommand для привязки команд
│   └── SettingsViewModel.cs        # (Не используется — удалить)
│
├── Models/
│   ├── Bookmark.cs                 # Модель закладки: Id, Title, Url, CreatedAt
│   └── HistoryEntry.cs             # Модель истории: Id, Title, Url, VisitedAt
│
├── Services/
│   ├── StealthService.cs           # Режим невидимости через Win32 SetWindowDisplayAffinity
│   ├── HistoryService.cs           # CRUD истории с сохранением в JSON
│   ├── BookmarkService.cs          # CRUD закладок с сохранением в JSON
│   ├── SearchService.cs            # Переключение поисковиков, нормализация URL
│   └── SettingsService.cs          # Настройки приложения (DNS, тема, шрифт)
│
└── Views/
    ├── SettingsPage.xaml / .cs     # Страница настроек (меню): DNS, история, закладки
    └── DnsTestWindow.xaml / .cs    # Модальное окно результатов теста DNS
```

---

## 🔄 Data Flow (Поток данных)

### 1. Инициализация приложения
```
App.xaml (StartupUri) → MainWindow constructor
  → создаётся MainViewModel
    → инициализируются сервисы (Stealth, History, Bookmark, Search, Settings)
    → создаётся первая вкладка (CreateTab)
      → TabViewModel создаёт WebView2
      → async InitializeWebViewAsync → EnsureCoreWebView2Async → ShowNewTabPage
  → DataContext = vm
  → StealthService.Initialize(this) — привязка к оконному хэндлу
```

### 2. Управление вкладками
```
User нажимает "+" → AddTabCommand → CreateTab()
  → GetEnvironmentAsync → новый TabViewModel(env, searchService)
  → Tabs.Add(tab) → SelectedTab = tab
  → UpdateFromSelectedTab() → DisplayedContent = SelectedTab.WebView
```

### 3. Навигация
```
User вводит URL → Enter → UrlBox_KeyDown
  → ViewModel.NavigateFromInput(url)
    → SelectedTab?.Navigate(url)
      → SearchService.NormalizeUrl(url) → URL или поисковый запрос
      → WebView.Source = new Uri(normalizedUrl)
  → WebView_NavigationStarting → IsLoading = true
  → WebView_ContentLoading → Progress = 50
  → WebView_NavigationCompleted → IsLoading = false, Title/Url обновлены
  → UpdateFromSelectedTab() → UrlInput, CanGoBack, CanGoForward синхронизированы
```

### 4. Открытие/закрытие меню (Settings)
```
User нажимает "⋮" → BtnMenu_Click
  → ViewModel.OpenSettings(settingsPage)
    → IsSettingsOpen = true
    → DisplayedContent = settingsPage
  → ContentControl (Grid.Row=5) автоматически обновляется через Binding

User нажимает "← Назад" → BackBtn_Click
  → ViewModel.CloseSettings()
    → IsSettingsOpen = false
    → DisplayedContent = SelectedTab?.WebView
```

### 5. Режим невидимости
```
User нажимает "👻" → ToggleStealthCommand
  → StealthService.ToggleStealthMode()
    → SetWindowDisplayAffinity(hWnd, WDA_EXCLUDEFROMCAPTURE)
    → Fallback: WDA_MONITOR если не поддерживается
  → StealthModeChanged event
  → OnStealthModeChanged → обновление индикатора в title bar
```

---

## 🔑 Ключевые компоненты

### MainViewModel
- **Ответственность:** Оркестрация всех сервисов и вкладок
- **Состояние:** Коллекция `Tabs`, `SelectedTab`, `DisplayedContent`
- **Команды:** AddTab, CloseTab, Navigation, Bookmark, Stealth, Search
- **Жизненный цикл:** Создаётся в MainWindow ctor, удаляется при OnClosed

### TabViewModel
- **Ответственность:** Обёртка вокруг одного WebView2
- **Состояние:** WebView, Title, Url, IsLoading, Progress, CanGoBack/Forward
- **Жизненный цикл WebView:**
  1. Create → 2. EnsureCoreWebView2Async → 3. Navigate/ShowNewTabPage → 4. Dispose
- **Важно:** Отписка от событий выполняется ДО Dispose()

### StealthService
- **API:** Win32 `SetWindowDisplayAffinity` (user32.dll)
- **Константы:**
  - `WDA_NONE` (0x00) — обычное окно
  - `WDA_MONITOR` (0x01) — только на физическом мониторе
  - `WDA_EXCLUDEFROMCAPTURE` (0x11) — полностью скрыт от захвата
- **Fallback:** Если WDA_EXCLUDEFROMCAPTURE не поддерживается → WDA_MONITOR
- **Инициализация:** Требует хэндл окна (SourceInitialized event)

### SettingsPage
- **Структура:** Левый сайдбар с навигацией + правая панель с контентом
- **Разделы:** DNS, Общие, Приватность, История, Закладки, О программе
- **Состояние:** Сохраняется при переключении разделов (переиспользуется экземпляр)

---

## ⚠️ Выявленные архитектурные проблемы

### КРИТИЧЕСКИЕ (исправлены ✅)
1. **✅ Дублирование WebView2** — WebView рендерился дважды: через DataTemplate TabControl И через ручное добавление в ContentGrid.  
   **Исправление:** Убрано ручное управление ContentGrid. Используется ContentControl с Binding к DisplayedContent.

2. **✅ Краш при открытии меню** — SettingsPage и WebView конкурировали за одно место в Grid, вызывая конфликт рендеринга.  
   **Исправление:** Единый ContentControl переключается между WebView и SettingsPage через DisplayedContent.

3. **✅ Утечка памяти в WebView2** — События WebView не отписывались перед Dispose().  
   **Исправление:** Отписка выполняется ДО вызова Dispose().

4. **✅ Пустые catch-блоки** — Сервисы глотали исключения без логирования.  
   **Исправление:** Все catch блоки теперь логируют через Debug.WriteLine.

### СРЕДНИЕ (требуют исправления)
5. **SettingsService implements INotifyPropertyChanged** — Сервис не должен реализовывать INPC, это ответственность ViewModel.  
   **Рекомендация:** Вынести настройки в SettingsViewModel.

6. **Code-behind вызывает конструктор View** — MainWindow создаёт SettingsPage напрямую, нарушая MVVM.  
   **Рекомендация:** Использовать DataTemplate + DataTrigger для автоматического отображения View по типу ViewModel.

7. **Нет валидации URL** — Navigate() не проверяет корректность URL перед созданием Uri.  
   **Рекомендация:** Добавить валидацию с пользовательским уведомлением.

8. **RelayCommand не поддерживает async** — Все команды синхронные, CreateTab — async void.  
   **Рекомендация:** Создать AsyncRelayCommand для асинхронных операций.

### НИЗКИЕ (улучшения)
9. **SettingsViewModel.cs не используется** — Файл мёртвый код.
10. **Нет unit-тестов** — Сервисы (SearchService, BookmarkService) не покрыты тестами.
11. **Жёсткая привязка к UI** — StealthService зависит от Window, что усложняет тестирование.

---

## 📐 План улучшения архитектуры

### Фаза 1: Стабилизация (выполнено ✅)
- [x] Исправить дублирование WebView2
- [x] Исправить краш меню
- [x] Исправить утечку памяти
- [x] Добавить обработку ошибок

### Фаза 2: Чистота кода (в процессе)
- [ ] Удалить SettingsViewModel.cs (мёртвый код)
- [ ] Добавить валидацию URL в Navigate()
- [ ] Создать AsyncRelayCommand для CreateTab
- [ ] Покрыть SearchService unit-тестами

### Фаза 3: Архитектурный рефакторинг
- [ ] Вынести настройки из SettingsService в SettingsViewModel
- [ ] Создать ViewLocator для автоматического отображения View
- [ ] Добавить DI-контейнер для управления зависимостями
- [ ] Разделить слой сервисов и слой UI

---

## 🔐 Безопасность

- **UserDataFolder WebView2:** `%APPDATA%\GhostBrowser` — изолированное хранилище
- **JSON файлы:** `%APPDATA%\GhostBrowser\{settings,bookmarks,history}.json`
- **Stealth mode:** Не шифрует данные, только скрывает окно от захвата
- **DNS:** Тесты используют HttpClient без системного изменения DNS (не влияют на ОС)

---

## 📊 Производительность

- **WebView2 на вкладку:** Каждая вкладка создаёт отдельный WebView2 → высокое потребление RAM
- **Оптимизация:** При закрытии вкладки WebView.Dispose() освобождает ресурсы
- **Лимит истории:** 1000 записей для предотвращения разрастания JSON
- **ClockTimer:** DispatcherTimer с интервалом 1 сек — минимальное влияние на CPU

---

*Документ создан автоматически при рефакторинге v1.0*
