# 🔒 Безопасность — GhostBrowser

> **Версия:** 1.0
> **Дата:** Апрель 2026
> **Платформа:** .NET 7.0, WPF, WebView2 (Chromium)

---

## ⚠️ ВАЖНО: ЧТЕНИЕ ВСЕХ AI-ФАЙЛОВ ОБЯЗАТЕЛЬНО

**Перед любой задачей ИИ обязан прочитать ВСЕ файлы из папки `AI/`:**
1. `onboarding.md` — ввод в курс дела
2. `task.md` — текущая задача от пользователя
3. `tasks.md` — детализированный список задач и багов
4. `architecture.md` — полная архитектура проекта
5. `rules.md` — правила работы ИИ
6. `user-responses.md` — ответы пользователя о багах
7. `design.md` — дизайн-система
8. `security.md` — политика безопасности (этот файл)
9. `workflow.md` — рабочий процесс (разбиение задач на подпланы)
10. `roadmap.md` — дорожная карта и будущие задачи

**НЕ начинай работу, пока не прочитал все 10 файлов.**

---

## 📝 После выполнения задачи — ОБЯЗАТЕЛЬНО

**После каждого изменения ИИ обязан:**
1. Обновить `tasks.md` — отметить выполненное
2. Обновить `user-responses.md` — записать новый контекст (если есть)
3. Обновить `architecture.md` — если изменилась структура проекта
4. Обновить `security.md` — если изменились настройки безопасности
5. Обновить `rules.md` — если появились новые правила
6. Обновить `workflow.md` — если изменился рабочий процесс
7. Обновить `onboarding.md` — если изменились приоритеты
8. Обновить `roadmap.md` — если появились новые задачи или изменён план
9. Написать краткий отчёт пользователю: что сделано, какие файлы изменены

---

## 1. Обзор модели безопасности

### 1.1. Угрозы и защита

| Угроза | Механизм защиты | Статус |
|--------|----------------|--------|
| **Захват экрана (OBS, Discord, Zoom)** | `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` | ✅ Реализовано |
| **История посещений** | Сохранение в JSON, возможность очистки | ✅ Реализовано |
| **Cookies и трекеры** | Настройки блокировки в WebView2 Profile | ⏳ Частично |
| **Утечка DNS** | Кастомный DNS-сервер, тестирование | ✅ Реализовано |
| **Фишинг** | Нет встроенной защиты | ⏳ Планируется |
| **Вредоносные загрузки** | Менеджер загрузок с контролем | ✅ Реализовано |
| **Утечка данных форм** | Нет автозаполнения | ✅ По умолчанию безопасно |
| **Отслеживание через fingerprint** | Нет защиты | ⏳ Планируется |

### 1.2. Принципы безопасности

1. **Минимальное хранение данных** — только история и закладки, всё можно очистить
2. **Изоляция приватных сессий** — режим инкогнито использует временный профиль WebView2
3. **Прозрачность для пользователя** — индикаторы stealth mode, статус DNS
4. **Защита от захвата** — Win32 API скрывает окно от программ захвата экрана

---

## 2. Режим невидимости (Stealth Mode)

### 2.1. Технология

**API:** `SetWindowDisplayAffinity` (user32.dll)

**Константы:**
```csharp
private const int WDA_NONE = 0x00;              // Обычное поведение
private const int WDA_MONITOR = 0x01;           // Видно только на физическом мониторе
private const int WDA_EXCLUDEFROMCAPTURE = 0x11; // Полная невидимость для захвата
```

**Поддерживаемые ОС:**
| ОС | WDA_EXCLUDEFROMCAPTURE | WDA_MONITOR |
|----|------------------------|-------------|
| Windows 10 2004+ | ✅ | ✅ |
| Windows 10 1909 и старше | ❌ | ✅ |
| Windows 11 | ✅ | ✅ |

### 2.2. Что скрывается

| Программа | Тип захвата | Результат |
|-----------|------------|-----------|
| OBS Studio | Захват экрана/окна | ❌ GhostBrowser не виден |
| Discord (стрим) | Захват экрана/окна | ❌ GhostBrowser не виден |
| Zoom | Демонстрация экрана | ❌ GhostBrowser не виден |
| Teams | Демонстрация экрана | ❌ GhostBrowser не виден |
| Windows Snipping Tool | Скриншот | ❌ GhostBrowser не виден |
| PrintScreen | Системный скриншот | ❌ Чёрный квадрат |

### 2.3. Что НЕ скрывается

| Сценарий | Причина |
|----------|---------|
| Прямой доступ к монитору (камера на телефон) | Физический захват, защита невозможна |
| Удалённый рабочий стол (RDP) | Зависит от реализации RDP-клиента |
| Захват через драйвер видеокарты | Теоретически возможно, требует kernel-level защиты |

### 2.4. Жизненный цикл

```
MainWindow.ctor()
    → StealthService.Initialize(this)
        → Подписка на SourceInitialized (хэндл окна может быть не готов)
    
SourceInitialized
    → Получение hWnd через WindowInteropHelper
    → Если есть отложенный запрос → ApplyAffinity()

Пользователь нажимает 👻 / Ctrl+Shift+H
    → ToggleStealthCommand
        → StealthService.ToggleStealthMode()
            → ApplyAffinity(enabled)
                → SetWindowDisplayAffinity(hWnd, affinity)
                → Если WDA_EXCLUDEFROMCAPTURE не поддерживается → fallback на WDA_MONITOR
                → StealthModeChanged?.Invoke(this, true/false)

UI обновляется
    → StealthIndicatorBorder.Background = SuccessBrush / TextMutedBrush
    → StealthStatusText.Text = "Stealth: ON" / "Stealth: OFF"

Закрытие окна
    → StealthService.Dispose()
        → ApplyAffinity(false) → WDA_NONE
        → Отписка от событий
```

### 2.5. Обработка ошибок

```csharp
try
{
    SetWindowDisplayAffinity(hWnd, affinity);
}
catch (DllNotFoundException)
{
    // user32.dll не найден — крайне маловероятно на Windows
    Debug.WriteLine("SetWindowDisplayAffinity: DLL not found");
}
catch (Win32Exception ex) when (ex.NativeErrorCode == 127)
{
    // Функция не поддерживается (старая версия Windows)
    // Fallback на WDA_MONITOR
    SetWindowDisplayAffinity(hWnd, WDA_MONITOR);
    Debug.WriteLine($"WDA_EXCLUDEFROMCAPTURE не поддерживается, используем WDA_MONITOR: {ex.Message}");
}
```

### 2.6. Известные ограничения

- ⚠️ **Не защищает от физической камеры** — если кто-то снимает экран на телефон
- ⚠️ **Не защищает от RDP-клиентов** — зависит от реализации удалённого рабочего стола
- ⚠️ **Может конфликтовать с оверлеями** — Discord overlay, Steam overlay

---

## 3. Режим инкогнито (Incognito Mode)

### 3.1. Отличия от обычного режима

| Параметр | Обычный режим | Инкогнито |
|----------|--------------|-----------|
| **История** | Сохраняется в `history.json` | ❌ Не сохраняется |
| **Закладки** | Сохраняются, можно добавлять | Доступны только для чтения (чтение из `bookmarks.json`) |
| **Cookies** | Сохраняются в профиле WebView2 | ❌ Удаляются при закрытии |
| **Кэш** | Сохраняется в `UserDataFolder` | ❌ Удаляется при закрытии |
| **UserDataFolder** | `%APPDATA%\GhostBrowser` | Временная папка (`%TEMP%\GhostBrowser_Incognito_{GUID}`) |
| **Загрузки** | Сохраняются в `downloads.json` | ❌ Не сохраняются |

### 3.2. Изоляция профиля WebView2

**Создание временного профиля:**
```csharp
var tempProfilePath = Path.Combine(
    Path.GetTempPath(), 
    $"GhostBrowser_Incognito_{Guid.NewGuid():N}"
);

var options = new CoreWebView2EnvironmentOptions
{
    UserDataFolder = tempProfilePath
};

var environment = await CoreWebView2Environment.CreateAsync(null, tempProfilePath, options);
```

**Очистка при закрытии:**
```csharp
protected override void OnClosed(EventArgs e)
{
    base.OnClosed(e);
    
    try
    {
        // Очистка временной папки
        if (Directory.Exists(_tempProfilePath))
        {
            Directory.Delete(_tempProfilePath, true);
            Debug.WriteLine($"Incognito profile deleted: {_tempProfilePath}");
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Failed to delete incognito profile: {ex.Message}");
    }
}
```

### 3.3. Очистка cookies и кэша

**При закрытии окна инкогнито:**
```csharp
if (WebView?.CoreWebView2 != null)
{
    // Очистка всех данных браузера
    await WebView.CoreWebView2.Profile.ClearBrowsingDataAsync();
    
    // Принудительное освобождение ресурсов
    WebView.Dispose();
}
```

### 3.4. Визуальные индикаторы

| Элемент | Описание |
|---------|----------|
| Заголовок окна | "GhostBrowser — Инкогнито" |
| Иконка | 🕶️ (вместо 👻) |
| Цвет прогресс-бара | Фиолетовый (`#8B5CF6`) |
| Баннер | "INCOGNITO — История не сохраняется" |

### 3.5. Горячая клавиша

- `Ctrl+Shift+N` — открыть окно инкогнито

---

## 4. Хранение данных

### 4.1. Расположение файлов

| Файл | Путь | Данные | Шифрование |
|------|------|--------|-----------|
| `settings.json` | `%APPDATA%\GhostBrowser\settings.json` | Настройки приложения | ❌ Нет |
| `history.json` | `%APPDATA%\GhostBrowser\history.json` | История посещений | ❌ Нет |
| `bookmarks.json` | `%APPDATA%\GhostBrowser\bookmarks.json` | Закладки | ❌ Нет |
| `downloads.json` | `%APPDATA%\GhostBrowser\downloads.json` | История загрузок | ❌ Нет |

### 4.2. Формат данных

**Сериализация:** `System.Text.Json`

**Пример history.json:**
```json
[
  {
    "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "Title": "Google",
    "Url": "https://www.google.com",
    "VisitedAt": "2026-04-05T14:32:00Z"
  }
]
```

### 4.3. Риски хранения

| Риск | Последствия | Митигация |
|------|------------|-----------|
| Файлы не зашифрованы | Любой пользователь может прочитать историю | Кнопка "Очистить историю" |
| Нет контроля доступа | Другие программы могут прочитать JSON | Режим инкогнито (нет записей) |
| Cookies WebView2 | Сохраняются сессии сайтовов | Очистка в инкогнито |

### 4.4. Рекомендации по улучшению

- [ ] Шифрование JSON-файлов через DPAPI (`ProtectedData.Protect`)
- [ ] Автоматическая очистка истории по таймеру (как в Chrome)
- [ ] Мастер-пароль для доступа к истории/закладкам

---

## 5. DNS-безопасность

### 5.1. Поддерживаемые DNS-пресеты

| Пресет | DNS-сервер | Особенности |
|--------|-----------|-------------|
| Google DNS | `8.8.8.8` | Быстрый, но логирует запросы |
| Cloudflare | `1.1.1.1` | Не логирует, быстрый |
| Cloudflare ( malware ) | `1.1.1.2` | Блокировка вредоносных доменов |
| Cloudflare (family) | `1.1.1.3` | Блокировка adult + malware |
| OpenDNS | `208.67.222.222` | Фильтрация, родительский контроль |
| Quad9 | `9.9.9.9` | Блокировка вредоносных доменов |
| AdGuard DNS | `94.140.14.14` | Блокировка рекламы и трекеров |
| UncensoredDNS | `94.140.14.15` | Обход цензуры |
| Yandex DNS | `77.88.8.8` | Российский, быстрая резолвинг |
| Yandex Safe | `77.88.8.88` | Блокировка вредоносных сайтов |
| Yandex Family | `77.88.8.7` | Родительский контроль |
| Control D | `76.76.2.0` | Кастомная фильтрация |
| NextDNS | Зависит от аккаунта | Кастомные правила |
| Custom | Ручной ввод | Любой DNS-сервер |

### 5.2. Тестирование DNS

**Метод:** `SettingsService.TestDnsAsync(dns)`

**Проверяемые сайты:**
1. `google.com`
2. `gmail.com`
3. `cloudflare.com`
4. `gemini.google.com`
5. `youtube.com`

**Процесс:**
1. Отправка UDP-запроса к DNS-серверу (порт 53)
2. Резолвинг тестовых сайтов через `HttpClient`
3. Вывод результатов: ✅ доступно / ❌ недоступно

**Ограничения:**
- ⚠️ Тест проверяет только доступность, не скорость
- ⚠️ Не проверяет DNSSEC (подпись записей)
- ⚠️ Не проверяет DoH/DoT (DNS over HTTPS/TLS)

### 5.3. Рекомендации по улучшению

- [ ] Поддержка DoH (DNS over HTTPS) — шифрование DNS-запросов
- [ ] Поддержка DoT (DNS over TLS)
- [ ] Тест скорости резолвинга
- [ ] Проверка на DNS leak

---

## 6. Блокировка трекеров и cookies

### 6.1. Настройки приватности

| Настройка | По умолчанию | Описание |
|-----------|-------------|----------|
| Блокировка трекеров | ✅ true | Предотвращает загрузку известных трекеров |
| Блокировка cookies третьих сторон | ✅ true | Запрещает cookies с доменов, отличных от текущего |

### 6.2. Реализация через WebView2

**CoreWebView2.Profile.PreferredColorScheme** — уже используется для тёмной темы.

**Для блокировки трекеров:**
```csharp
// Planned: CoreWebView2.SetVirtualHostNameToFolderMapping
// Planned: CoreWebView2.Profile.ClearBrowsingDataAsync
```

**Для cookies третьих сторон:**
```csharp
// Planned: CoreWebView2.CookieManager
// Iterate cookies, delete if Domain != current
```

### 6.3. Статус реализации

| Функция | Статус | Примечание |
|---------|--------|-----------|
| Блокировка трекеров | ⏳ Запланировано | Нет реализации в текущей версии |
| Блокировка cookies | ⏳ Запланировано | Нет реализации в текущей версии |
| Тёмная тема WebView2 | ✅ Реализовано | `PreferredColorScheme = Dark` |

---

## 7. Менеджер загрузок — безопасность

### 7.1. Перехват загрузок

**Событие WebView2:** `DownloadStarting`

```csharp
WebView.CoreWebView2.DownloadStarting += (sender, e) =>
{
    // Отменяем стандартную загрузку WebView2
    e.Cancel = true;
    
    // Передаём в DownloadService
    _downloadService.StartDownload(e.DownloadOperation);
};
```

### 7.2. Контроль загружаемых файлов

| Параметр | Описание |
|----------|----------|
| Путь сохранения | Настраивается в настройках (по умолчанию: `%USERPROFILE%\Downloads`) |
| Проверка типа файла | ⏳ Планируется (предупреждение о `.exe`, `.bat`, `.js`) |
| Проверка размера | ⏳ Планируется (предупреждение о файлах > 1GB) |
| Сканирование антивирусом | ❌ Не реализуется (зависит от ОС) |

### 7.3. Безопасность загрузок

- ✅ Загрузки сохраняются в выбранную пользователем папку
- ✅ История загрузок → `downloads.json` (можно очистить)
- ⏳ Нет предупреждений о подозрительных файлах
- ❌ Нет интеграции с Windows Defender

### 7.4. Рекомендации

- [ ] Предупреждение при загрузке `.exe`, `.bat`, `.msi`, `.js`
- [ ] Предупреждение при загрузке файлов > 500MB
- [ ] Проверка хэша файла (SHA-256)

---

## 8. Защита от фишинга

### 8.1. Текущий статус

| Механизм | Статус |
|----------|--------|
| Проверка URL по чёрным спискам | ❌ Не реализовано |
| Предупреждение о подозрительных сайтах | ❌ Не реализовано |
| Проверка SSL-сертификатов | ✅ WebView2 автоматически |
| Индикатор HTTPS | ❌ Не реализовано |

### 8.2. Рекомендации

- [ ] Индикатор HTTPS/HTTP в адресной строке (замок 🔒 / ⚠️)
- [ ] Проверка SSL-сертификата (срок действия, issuer)
- [ ] Интеграция с Google Safe Browsing API
- [ ] Предупреждение о сайтах с похожими доменами (typosquatting)

---

## 9. Защита от fingerprinting

### 9.1. Текущий статус

| Метод fingerprinting | Защита |
|---------------------|--------|
| User-Agent | ❌ WebView2 отдаёт стандартный |
| Canvas fingerprinting | ❌ Не защищено |
| WebGL fingerprinting | ❌ Не защищено |
| Audio fingerprinting | ❌ Не защищено |
| Screen resolution | ❌ WebView2 отдаёт реальное |
| Timezone | ❌ WebView2 отдаёт системное |
| Language | ❌ WebView2 отдаёт системный |

### 9.2. Рекомендации

- [ ] Кастомный User-Agent (без точной версии Chromium)
- [ ] Блокировка Canvas API (или добавление шума)
- [ ] Блокировка WebGL (настраиваемая)
- [ ] Защита от AudioContext fingerprinting
- [ ] Маскировка реального разрешения экрана

---

## 10. Валидация URL

### 10.1. Текущий статус

**Метод:** `SearchService.NormalizeUrl(input)`

**Логика:**
1. Если ввод похож на поисковый запрос (нет точки, нет `http://`, есть пробелы) → URL поиска
2. Если нет `http://` или `https://` → добавляет `https://`
3. Возвращает нормализованный URL

**Проблема:** Нет валидации корректности URL перед `new Uri()`

### 10.2. План исправления

```csharp
public string NormalizeUrl(string input)
{
    if (IsSearchQuery(input))
    {
        return GetSearchUrl(input);
    }
    
    var url = input.StartsWith("http://") || input.StartsWith("https://") 
        ? input 
        : $"https://{input}";
    
    // Валидация перед созданием Uri
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        // Вернуть поисковый запрос вместо краха
        return GetSearchUrl(input);
    }
    
    return uri.ToString();
}
```

### 10.3. Edge cases

| Ввод | Ожидаемое поведение |
|------|-------------------|
| `google.com` | `https://google.com` |
| `google` | Поиск Google |
| `http://[invalid` | Поиск или ошибка |
| `localhost:8080` | `http://localhost:8080` |
| `file:///C:/test.html` | `file:///C:/test.html` |
| `ghost://newtab` | Специальный протокол → NewTabPage |

---

## 11. Глобальные обработчики исключений

### 11.1. App.xaml.cs

```csharp
// Глобальный обработчик UI-потока
DispatcherUnhandledException += (sender, e) =>
{
    Debug.WriteLine($"Unhandled exception: {e.Exception}");
    e.Handled = true; // Предотвращаем краш
};

// Глобальный обработчик фоновых потоков
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var ex = e.ExceptionObject as Exception;
    Debug.WriteLine($"Unhandled exception (background): {ex}");
};

// Глобальный обработчик Task
TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    Debug.WriteLine($"Unobserved task exception: {e.Exception}");
    e.SetObserved(); // Предотвращаем краш
};
```

### 11.2. Локальные try-catch

**Все методы с риском падения обёрнуты:**
- `CreateTab()` — создание WebView2
- `CloseTab()` — закрытие вкладки
- `UpdateFromSelectedTab()` — обновление состояния
- `LoadSettings()` — загрузка настроек из JSON
- `TestDns()` — сетевые запросы

**Правило:** НИКОГДА не оставлять пустые `catch {}`

---

## 12. Рекомендации по улучшению безопасности

### 12.1. Критические (высокий приоритет)

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| Шифрование JSON-файлов | DPAPI (`ProtectedData.Protect`) для history.json, bookmarks.json | 🔴 Высокий |
| Валидация URL | Предотвращение краха при некорректном URL | 🔴 Высокий |
| Предупреждение о `.exe` | Диалог перед загрузкой исполняемых файлов | 🔴 Высокий |

### 12.2. Важные (средний приоритет)

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| Индикатор HTTPS | 🔒 / ⚠️ в адресной строке | 🟡 Средний |
| DoH/DoT поддержка | Шифрование DNS-запросов | 🟡 Средний |
| Автоочистка истории | Таймер автоочистки (1 день / 7 дней / 30 дней) | 🟡 Средний |
| Мастер-пароль | Защита доступа к истории/закладкам | 🟡 Средний |

### 12.3. Желательные (низкий приоритет)

| Задача | Описание | Приоритет |
|--------|----------|-----------|
| Anti-fingerprint | Кастомный User-Agent, блокировка Canvas/WebGL | 🟢 Низкий |
| Safe Browsing | Интеграция с Google Safe Browsing API | 🟢 Низкий |
| Проверка SSL | Предупреждение о просроченных сертификатах | 🟢 Низкий |
| Блокировка трекеров | Реализация через WebView2 API | 🟢 Низкий |

---

## 13. Ссылки на файлы

| Компонент | Файл |
|-----------|------|
| Режим невидимости | `Services/StealthService.cs` |
| История | `Services/HistoryService.cs` |
| Закладки | `Services/BookmarkService.cs` |
| Настройки | `Services/SettingsService.cs` |
| Загрузки | `Services/DownloadService.cs` |
| Менеджер загрузок | `Models/DownloadItem.cs`, `Models/DownloadItemStatus.cs` |
| Глобальные обработчики | `App.xaml.cs` |
| Режим инкогнито | `IncognitoWindow.xaml.cs`, `ViewModels/IncognitoViewModel.cs` |

---

*Файл обновляется при изменении настроек безопасности или добавлении новых механизмов защиты.*
