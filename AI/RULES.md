# 📜 Правила и стандарты разработки KingBrowser

## Основные принципы
- **DRY** (Don't Repeat Yourself) — не повторяйся
- **KISS** (Keep It Simple, Stupid) — делай проще
- **SOLID** — принципы объектно-ориентированного проектирования
- **YAGNI** (You Aren't Gonna Need It) — не делай лишнего
- **Privacy First** — приватность пользователя в приоритете

## Конвенции именования
| Элемент | Стиль | Пример |
|---------|-------|--------|
| Переменные | camelCase | `webView`, `tabCount`, `isStealthMode` |
| Константы | PascalCase | `MaxHistoryEntries`, `AppDataPath` |
| Классы | PascalCase | `MainViewModel`, `StealthService` |
| Методы | PascalCase | `NavigateToUrl()`, `InitializeWebView()` |
| Файлы | PascalCase | `MainWindow.xaml.cs`, `BookmarkService.cs` |
| События | PascalCase + `Event` suffix | `DownloadCompletedEvent` |
| Команды | PascalCase + `Command` suffix | `NavigateCommand`, `CloseTabCommand` |

## Стандарты коммитов
- `feat:` — новая функциональность (`feat: add download manager`)
- `fix:` — исправление бага (`fix: memory leak in tab disposal`)
- `docs:` — изменение документации (`docs: update ARCHITECTURE.md`)
- `refactor:` — рефакторинг кода (`refactor: extract stealth service`)
- `test:` — добавление/изменение тестов (`test: add BookmarkService tests`)
- `chore:` — служебные изменения (`chore: update WebView2 version`)
- `perf:` — оптимизация производительности (`perf: reduce memory usage`)
- `style:` — стилизация UI (`style: update monochrome theme`)

## MVVM правила

### ViewModel
1. ViewModel НЕ должен содержать ссылки на UI элементы (никаких `TextBox`, `Button`)
2. Вся логика через `ICommand` (RelayCommand / AsyncRelayCommand)
3. Свойства UI обновляются через `INotifyPropertyChanged`
4. Асинхронные операции через `async/await` с обработкой ошибок

```csharp
// ✅ ПРАВИЛЬНО
private string _url;
public string Url 
{
    get => _url;
    set => Set(ref _url, value);
}

public ICommand NavigateCommand => new RelayCommand(Navigate);

private void Navigate() 
{
    // Логика навигации
}

// ❌ НЕПРАВИЛЬНО
public TextBox UrlBox; // Прямая ссылка на UI
```

### Models
1. Модели — чистые POCO объекты
2. Минимум логики, только данные
3. `INotifyPropertyChanged` только если модель биндится напрямую к UI

### Services
1. Один сервис — одна ответственность
2. Сервисы НЕ зависят от UI
3. Интерфейсы для тестируемости (будущее)
4. Логирование критических операций через `Debug.WriteLine`

## Требования к коду
1. Код должен быть читаемым и понятным
2. Обязательные комментарии к сложным участкам (Win32 API, WebView2 хуки)
3. Каждый метод — одна ответственность
4. Минимальная вложенность (не более 3 уровней)
5. Обработка всех ошибок и исключений
6. `async/await` для всех I/O операций
7. `Dispose` для WebView2 и ресурсов

## WebView2 специфика
```csharp
// ✅ ВСЕГДА: инициализация
await webView.EnsureCoreWebView2Async(environment);

// ✅ ВСЕГДА: подписка на события
webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

// ✅ ВСЕГДА: освобождение ресурсов
public void Dispose() 
{
    webView?.Dispose();
    webView = null;
}

// ❌ НИКОГДА: не забывайте Dispose
// webView = new WebView2(); // Утечка памяти!
```

## Безопасность
- ⛔ Никогда не коммить секреты, ключи, пароли
- 🔒 Использовать переменные окружения для чувствительных данных
- ✅ Валидировать все пользовательские данные (URL, search queries)
- ✅ Параметризованные запросы (если будет SQLite)
- 🔒 Не хранить пароли в открытом виде (будущий password manager)
- ✅ Отключать autofill для приватности
- ✅ Блокировать Canvas/WebGL/AudioContext фингерпринтинг

## Приватность (критично для KingBrowser)
1. Инкогнито режим: полное удаление данных при закрытии
2. Panic button F12: очистка cookies, cache, данных
3. Anti-fingerprinting: блокировка фингерпринтинг API
4. Кастомный User-Agent: маскировка под стандартный браузер
5. Отключен автозаполнение форм по умолчанию
6. Forced dark mode для всех сайтов

## Тестирование
- Покрытие тестами: минимум 80%
- Unit-тесты для каждой сервисной функции
- Integration тесты для WebView2 навигации
- Тесты граничных случаев (невалидный URL, пустые данные)
- Ручное тестирование stealth mode (OBS, Discord, ножницы)

## Сборка и запуск
```bash
# Быстрый запуск
run.bat

# Ручная сборка
dotnet restore
dotnet build --no-restore
dotnet run

# Release сборка
dotnet publish -c Release -o ./publish
```

## Структура данных (%APPDATA%\GhostBrowser\)
```
GhostBrowser/
├── bookmarks.json      # Закладки (не удалять)
├── history.json        # История (макс 1000 записей)
├── settings.json       # Настройки приложения
├── downloads.json      # История загрузок
└── Incognito/          # Временный профиль (удаляется при закрытии)
```

## Горячие клавиши (не изменять без обсуждения)
| Комбинация | Действие | Критичность |
|------------|----------|-------------|
| Ctrl+T | Новая вкладка | 🔴 |
| Ctrl+W | Закрыть вкладку | 🔴 |
| Ctrl+L | Фокус на URL бар | 🟡 |
| Ctrl+1-9 | Переключение вкладок | 🟡 |
| Ctrl+D | Добавить в закладки | 🟡 |
| Ctrl+R | Обновить | 🔴 |
| Alt+Left | Назад | 🟡 |
| Alt+Right | Вперед | 🟡 |
| F11 | Полноэкранный режим | 🟢 |
| F12 | Panic Button | 🔴 |
| PrintScreen | Блокируется | 🔴 |

## Известные технические долги
1. **Ручной DI** — рефакторинг к Microsoft.Extensions.DependencyInjection
2. **JSON вместо SQLite** — миграция при >10K записей
3. **Большой App.xaml** (555 строк) — разбить на ResourceDictionary
4. **Win32 API хардкод** — вынести в конфигурацию
