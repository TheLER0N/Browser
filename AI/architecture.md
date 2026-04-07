# Архитектура — KING Browser

**Стек:** C# 12, .NET 7, WPF, WebView2 v1.0.2739.15, MVVM

---

## Структура проекта

```
KingBrowser/
├── App.xaml(.cs)              ← Точка входа, глобальные стили
├── MainWindow.xaml(.cs)       ← Главное окно
├── IncognitoWindow.xaml(.cs)  ← Окно инкогнито
├── NewTabPage.html            ← Страница новой вкладки
│
├── ViewModels/
│   ├── MainViewModel.cs       ← Оркестрация: вкладки, навигация, команды
│   ├── TabViewModel.cs        ← VM вкладки: WebView2, URL, заголовок, прогресс
│   ├── ViewModelBase.cs       ← INotifyPropertyChanged
│   ├── RelayCommand.cs        ← ICommand
│   └── AsyncRelayCommand.cs   ← Async ICommand
│
├── Services/
│   ├── StealthService.cs      ← SetWindowDisplayAffinity (невидимость)
│   ├── SearchService.cs       ← Поисковики, нормализация URL
│   ├── HistoryService.cs      ← История → JSON
│   ├── BookmarkService.cs     ← Закладки → JSON
│   └── SettingsService.cs     ← Настройки, DNS
│
├── Models/
│   ├── Bookmark.cs            ← Модель закладки
│   └── HistoryEntry.cs        ← Модель записи истории
│
└── Views/
    ├── SettingsPage.xaml(.cs) ← Страница настроек
    └── DnsTestWindow.xaml(.cs)← Окно теста DNS
```

---

## Ключевые классы

| Класс | Файл | Ответственность |
|---|---|---|
| MainWindow | MainWindow.xaml.cs | UI события, hotkeys, lifecycle окна |
| MainViewModel | MainViewModel.cs | Вкладки, навигация, команды, оркестрация |
| TabViewModel | TabViewModel.cs | WebView2, URL, заголовок, прогресс вкладки |
| StealthService | StealthService.cs | WDA_EXCLUDEFROMCAPTURE, блокировка скриншотов |
| SearchService | SearchService.cs | Google/Yandex/DuckDuckGo, нормализация URL |
| SettingsService | SettingsService.cs | Настройки → JSON, INPC |

---

## Поток данных

### Инициализация
```
App.xaml.cs → MainWindow → DataContext = MainViewModel
MainViewModel инициализирует сервисы → 1я вкладка → NewTabPage
```

### Навигация
```
User вводит URL → UrlBox_KeyDown → MainViewModel.NavigateAsync()
→ SearchService.NormalizeUrl() → TabViewModel.WebView.Source
```

### Управление вкладками
```
AddTab → Tabs.Add(new TabViewModel) → SelectedTab = tab
→ UpdateFromSelectedTab → ContentArea показывает WebView
CloseTab → Tabs.Remove(tab) → SelectedTab = Tabs.LastOrDefault()
```

### Stealth режим
```
ToggleStealthCommand → StealthService.Toggle()
→ SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)
→ UI обновляется: индикатор, текст, пульсация
```

---

## MVVM паттерн

- ViewModel **не знает** о View
- Commands через `ICommand` (RelayCommand / AsyncRelayCommand)
- `ViewModelBase` → `INotifyPropertyChanged` через `Set()`
- DataTemplate в XAML → авто-отображение VM

---

## Хранение данных

`%APPDATA%\KING\` — закладки, история, настройки в JSON.

---

## Известные архитектурные решения

### GradientStop в XAML
**НЕЛЬЗЯ** `Color="{StaticResource ...}"` в GradientStop. WPF парсер выдаёт `DeferredBinaryDeserializerException`. Только HEX: `Color="#ffffff"`.

### WebView2
Тяжёлый объект. Одна WebView2 на вкладку. Не создавай больше нужного. Инициализация асинхронная.

### Dispatcher
Все UI-операции через `Dispatcher.Invoke` или `Dispatcher.InvokeAsync`.

### WindowChrome
`CaptionHeight="0"` — кастомный title bar. Resize через `ResizeBorderThickness="8"`.
