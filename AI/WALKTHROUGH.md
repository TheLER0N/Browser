# 🧭 Пошаговый гайд по кодовой базе KingBrowser

> Этот файл помогает AI и разработчикам быстро ориентироваться в проекте при выполнении задач.

---

## 🚀 Быстрый старт

### Точка входа приложения
```
App.xaml → App.xaml.cs → MainWindow.xaml → MainWindow.xaml.cs → MainViewModel
```

### Где что искать
| Задача | Куда смотреть |
|--------|---------------|
| Изменить UI главного окна | `MainWindow.xaml` + `MainWindow.xaml.cs` |
| Логика вкладок/навигации | `ViewModels/MainViewModel.cs` + `ViewModels/TabViewModel.cs` |
| Добавить сервис | Папка `Services/` → создать новый файл |
| Модель данных | Папка `Models/` |
| Настройки страницы | `Views/SettingsPage.xaml` |
| Стили/тема | `App.xaml` (ResourceDictionary, ~555 строк) |
| Новая вкладка (HTML) | `NewTabPage.html` |
| Инкогнито режим | `IncognitoWindow.xaml` + `IncognitoViewModel.cs` |

---

## 📁 Структура проекта (по порядку обхода)

### 1. Entry Point
```
App.xaml.cs
├── OnStartup() — инициализация
├── Global exception handlers (UI + background threads)
└── Запуск MainWindow
```

### 2. Главное окно
```
MainWindow.xaml
├── WindowChrome (AllowsTransparency=True) — кастомное окно без стандартной рамки
├── Title bar (перетаскивание, свернуть/развернуть/закрыть)
├── Tab bar (ObservableCollection tabs + SelectedTab)
├── Navigation bar (Back/Forward/Refresh/Home)
├── URL bar (TextBox с автодетектом поиск/URL)
├── Bookmarks bar (горизонтальный список)
├── Content area (TabControl с WebView2 в каждой вкладке)
├── Progress bar (загрузка страницы)
└── Status bar (статус, индикатор stealth mode)

MainWindow.xaml.cs
├── WindowChrome handlers (drag, resize)
├── F11 fullscreen toggle
├── Global hotkeys (Ctrl+T, Ctrl+W, Ctrl+L, etc.)
├── Stealth mode indicator
└── Launch incognito window
```

### 3. ViewModel слой
```
MainViewModel.cs (ядро)
├── ObservableCollection<TabViewModel> Tabs
├── SelectedTab
├ ├── Commands: NewTabCommand, CloseTabCommand, NavigateCommand, etc.
├ ├── BookmarkService, HistoryService, SearchService
├ ├── Panic button logic (F12)
└ └── Download manager integration

TabViewModel.cs (одна вкладка)
├── WebView2 instance
├── Url, Title, IsLoading, Progress
├ ├── Navigation: Back, Forward, Refresh
├ ├── DownloadStarting handler
├── Anti-fingerprint script injection
└ └── Dispose() — ВАЖНО! освобождение ресурсов

IncognitoViewModel.cs
├── Isolated WebView2 UserDataFolder
├── No bookmarks/history saving
└ └── Full cleanup on window close (delete folder)
```

### 4. Services (бизнес-логика)
```
StealthService.cs → SetWindowDisplayAffinity (WDA_EXCLUDEFROMCAPTURE)
GlobalHotkey.cs → RegisterHotKey (PrintScreen block, F12 panic)
ScreenshotBlocker.cs → JS injection (Canvas/WebGL/AudioContext block)
SnippingToolBlocker.cs → WndProc, WM_PRINTCLIENT intercept

BookmarkService.cs → bookmarks.json (load/save)
HistoryService.cs → history.json (max 1000, dedup)
SettingsService.cs → settings.json + DNS test (HTTP + UDP)
DownloadService.cs → HTTP Range, pause/resume, downloads.json
SearchService.cs → Google/Bing/DuckDuckGo/Yandex, URL normalization
AdBlockService.cs → regex filters
AutoFillService.cs → form autofill profiles
ProfileService.cs → multi-profile isolation
```

### 5. Models
```
Bookmark.cs → Id, Title, Url, Favicon, CreatedAt
HistoryEntry.cs → Id, Title, Url, VisitedAt
DownloadItem.cs → Full model with INotifyPropertyChanged, speed, status
AdvancedSettings.cs → ~100 properties (language, theme, DNS, proxy, etc.)
UserProfile.cs → Multi-profile support
AdBlockFilter.cs → Regex rules + resource type filtering
AutoFillProfile.cs → Name, contacts, address
SyncResult.cs → Bookmark import statistics
```

---

## 🔍 Типичные задачи и куда идти

### Добавить новую вкладку в навигацию
```
1. MainViewModel.cs → добавить команду (ICommand)
2. MainWindow.xaml → добавить кнопку/элемент UI
3. MainWindow.xaml.cs → забиндить на команду (если нужно)
```

### Добавить настройку
```
1. Models/AdvancedSettings.cs → добавить свойство
2. Views/SettingsPage.xaml → добавить UI элемент
3. Services/SettingsService.cs → добавить сохранение/загрузку
4. App.xaml → добавить стили (если нужно)
```

### Изменить stealth mode поведение
```
1. Services/StealthService.cs → основной API
2. Services/ScreenshotBlocker.cs → JS injection
3. Services/SnippingToolBlocker.cs → WM_PRINTCLIENT
4. MainWindow.xaml.cs → интеграция с окном
```

### Добавить горячую клавишу
```
1. MainWindow.xaml.cs → KeyDown handler или GlobalHotkey.cs → RegisterHotKey
2. MainViewModel.cs → команда для обработки
```

### Изменить тему/стили
```
1. App.xaml → ResourceDictionary (все стили тут)
2. Искать по ключевым словам: "Style", "ControlTemplate", "Brush"
```

---

## ⚠️ Критичные места (трогать осторожно)

| Файл/Место | Проблема | Почему осторожно |
|-----------|---------|-----------------|
| `TabViewModel.Dispose()` | Утечки памяти | WebView2 требует явного Dispose |
| `App.xaml` (555 строк) | Большой файл стилей | Легко сломать зависимости стилей |
| `GlobalHotkey.cs` | Конфликты с другими программами | PrintScreen перехват глобальный |
| `MainWindow.xaml.cs` (WndProc) | Перехват сообщений Windows | Неправильный hook = краш |
| `IncognitoViewModel` → cleanup | Удаление папки | Неправильный path = удаление не того |
| `SettingsService` DNS test | UDP сокеты | Требуют прав администратора иногда |

---

## 🧪 Диагностика и логи

### Где искать логи
```csharp
// Все сервисы используют Debug.WriteLine
Debug.WriteLine($"[ServiceName] Message: {detail}");
```

### Включить подробное логирование
1. В `AdvancedSettings` → включить debug mode (если есть)
2. Проверить Output в Visual Studio / терминал

### Частые ошибки
| Ошибка | Причина | Решение |
|--------|---------|---------|
| WebView2 не инициализируется | Нет WebView2 Runtime | Установить с microsoft.com |
| Утечка памяти при закрытии вкладок | Не вызван Dispose | Проверить TabViewModel.Dispose() |
| PrintScreen не блокируется | Другая программа перехватила | Проверить RegisterHotKey return value |
| Окно не скрывается в OBS | Windows < 10 2004 | Fallback на WDA_MONITOR |

---

## 📐 Паттерны, используемые в проекте

| Паттерн | Где используется |
|---------|-----------------|
| MVVM | Вся архитектура (View → ViewModel → Model) |
| Command | RelayCommand, AsyncRelayCommand для ICommand |
| Observer | INotifyPropertyChanged через ViewModelBase |
| Singleton | SettingsService.Instance |
| Repository | BookmarkService, HistoryService (JSON хранилище) |
| Strategy | SearchService (разные поисковики) |
| Dispose | WebView2 cleanup в TabViewModel и IncognitoViewModel |

---

## 🔗 Зависимости компонентов

```
MainWindow
├── MainViewModel
│   ├── TabViewModel[] 
│   │   └── WebView2
│   ├── BookmarkService → bookmarks.json
│   ├── HistoryService → history.json
│   ├── SearchService
│   ├── DownloadService → downloads.json
│   └── SettingsService → settings.json
├── StealthService → Win32 SetWindowDisplayAffinity
├── GlobalHotkey → Win32 RegisterHotKey
├── ScreenshotBlocker → WebView2 JS injection
└── SnippingToolBlocker → WndProc override

IncognitoWindow
└── IncognitoViewModel
    └── WebView2 (isolated UserDataFolder)
```

---

## 💡 Советы для AI

1. **При изменении XAML** — всегда проверяй биндинги `{Binding}`, они зависят от DataContext
2. **При добавлении команд** — используй `RelayCommand` или `AsyncRelayCommand`
3. **При работе с WebView2** — всегда `Dispose()` при закрытии
4. **При изменении настроек** — обновляй `AdvancedSettings` модель и `SettingsService`
5. **При stealth mode** — тестируй вручную (автотестов нет)
6. **При горячих клавишах** — проверяй конфликты с Windows shortcuts
