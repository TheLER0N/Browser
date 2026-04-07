# Контекст пользователя — KING Browser

---

## Ответы пользователя

| Вопрос | Ответ |
|---|---|
| Как тестировать? | Пользователь тестирует сам |
| .NET версия? | .NET 7 SDK |
| Сколько вкладок обычно? | 1-3 вкладки |
| Какая тема дизайна? | Чёрно-белая монохромная (BW) |
| Имя бренда | KING (не KING11) |

---

## Текущее состояние проекта

### Выполнено
- ✅ BW Monochrome тема во всех XAML файлах
- ✅ Stealth 2.0 (блокировка скриншотов, анти-fingerprint)
- ✅ Логотип ♚ на NewTabPage.html
- ✅ Критические баги (вылет, зависание, артефакты) исправлены
- ✅ Мёртвый код удалён, обработчики исключений добавлены
- ✅ NewTabPage.html полностью переписан
- ✅ GradientStop StaticResource → HEX (45 мест)
- ✅ GitHub: https://github.com/TheLER0N/Browser.git (main)

### Структура проекта (актуальная)
```
KingBrowser/
├── AI/                          ← 10 файлов документации
├── MainWindow.xaml(.cs)         ← Главное окно
├── IncognitoWindow.xaml(.cs)    ← Окно инкогнито
├── App.xaml(.cs)                ← Точка входа, стили
├── NewTabPage.html              ← Новая вкладка
├── ViewModel/                   ← MainViewModel, TabViewModel...
├── Services/                    ← Stealth, Search, History...
├── Models/                      ← Bookmark, HistoryEntry
└── Views/                       ← SettingsPage, DnsTestWindow
```

### Удалённые файлы
- `ViewModels/AsyncRelayCommand.cs` — удалён, используется встроенный
- `Services/GlobalHotkeyService.cs` — удалён
- `Services/PanicService.cs` — удалён
- `IncognitoWindow.xaml.cs` — удалён
- `Views/DownloadsPage.xaml` — удалён
