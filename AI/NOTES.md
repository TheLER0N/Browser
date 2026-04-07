# 📝 Заметки и документация KingBrowser

## Важные решения
| Дата | Решение | Причина | Последствия |
|------|---------|---------|-------------|
| 08.04.2026 | WebView2 вместо CEF | Официальная поддержка Microsoft, тоньше дистрибутив | Требуется установка WebView2 Runtime у пользователя |
| 08.04.2026 | JSON вместо SQLite для данных | Простота, читаемость, нет зависимостей | Проблемы с производительностью при больших объемах (>10K записей) |
| 08.04.2026 | WDA_EXCLUDEFROMCAPTURE с fallback на WDA_MONITOR | Поддержка старых версий Windows | На Windows < 10 2004 только скрытие от fullscreen |
| 08.04.2026 | Черно-белая тема по умолчанию | Уникальный визуальный стиль, фокус на контенте | Может не понравиться пользователям цветных тем |
| 08.04.2026 | Ручной DI вместо контейнера | Простота проекта, нет оверхеда | Сложнее тестирование, нужно рефакторить при росте |

## Полезные ссылки
- [WebView2 документация](https://learn.microsoft.com/en-us/microsoft-edge/webview2/)
- [SetWindowDisplayAffinity (MSDN)](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity)
- [RegisterHotKey (MSDN)](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)
- [WPF Data Binding](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/)
- [MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [Canvas Fingerprinting](https://browserleaks.com/canvas)
- [WebRTC Leak](https://www.vpnmentor.com/blog/webRTC-leak-privacy/)

## Решения проблем

### Проблема #1: WebView2 не инициализируется
**Описание:** WebView2 требует установки WebView2 Runtime  
**Причина:** На некоторых Windows нет WebView2 Runtime  
**Решение:** Проверка при запуске, ссылка на установку  
**Код фикса:**
```csharp
// В App.xaml.cs или MainWindow
try 
{
    var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
    if (string.IsNullOrEmpty(version))
        MessageBox.Show("WebView2 Runtime не установлен. Скачайте с https://go.microsoft.com/fwlink/p/?LinkId=2124703");
}
catch (Exception ex)
{
    Debug.WriteLine($"WebView2 check failed: {ex.Message}");
}
```

### Проблема #2: Утечки памяти при закрытии вкладок
**Описание:** WebView2 не освобождает ресурсы  
**Причина:** WebView2 требует явного вызова Dispose  
**Решение:** Вызывать Dispose для каждой вкладки при закрытии  
**Код фикса:**
```csharp
// В TabViewModel или при закрытии вкладки
public void Dispose()
{
    webView?.Dispose();
    webView = null;
}
```

### Проблема #3: PrintScreen не блокируется на некоторых клавиатурах
**Описание:** Некоторые клавиатуры используют другие скан-коды  
**Причина:** RegisterHotKey не ловит все варианты PrintScreen  
**Решение:** Глобальный хук клавиатуры (низкоуровневый)  
**Статус:** Частично решено через WM_PRINTCLIENT блокировку

### Проблема #4: Стеки при максимизации окна
**Описание:** WindowChrome + AllowsTransparency вызывает артефакты  
**Причина:** Конфликт прозрачности и кастомного chrome  
**Решение:** Использовать Borderless окно с ручным управлением размерами  
**Код фикса:** Реализовано через ResizeMode="NoResize" + ручные хэндлеры

## Идеи и улучшения
- [ ] Облачная синхронизация закладок
- [ ] Менеджер паролей с шифрованием
- [ ] Расширения (uBlock Origin, Dark Reader)
- [ ] Вертикальные вкладки
- [ ] Группировка вкладок (Tab Groups)
- [ ] Режим чтения (Reader Mode)
- [ ] Жесты мыши
- [ ] Инсталлятор с автоустановкой WebView2
- [ ] Подпись кода для избежания ложных срабатываний антивируса
- [ ] Портативная версия (portable)
- [ ] Мобильная версия (MAUI?)

## Встречи и обсуждения

### 08.04.2026
- Проанализирован проект KingBrowser
- Заполнены файлы AI: ARCHITECTURE.md, PLAN.md, TASKS.md, NOTES.md, RULES.md
- Созданы: WALKTHROUGH.md, KNOWN_ISSUES.md, SPRINT.md, WORKFLOW.md, AUTO_WORKFLOW.md, QWEN.md
- Выявлены сильные стороны: stealth mode, MVVM, сервисы
- Выявлены технические долги: ручной DI, JSON вместо SQLite
- Определены следующие шаги: тестирование, оптимизация
- ✅ **Реализован обход блокировок**: ProxyService, DoH (Cloudflare/Google), SOCKS5/HTTP прокси
- Метод: `--proxy-server` аргумент через CoreWebView2EnvironmentOptions — НЕ требует админ прав
- ✅ **Автоприменение**: `ReinitializeEnvironmentAsync()` — пересоздаёт среду WebView2 без перезапуска браузера, URL вкладок сохраняются

## FAQ

**Q: Работает ли браузер на Windows 7/8?**  
**A:** Нет, требуется Windows 10 2004+ для полного функционала stealth mode. Базовый браузер может работать на Win10+.

**Q: Почему окно не скрывается в OBS?**  
**A:** Убедитесь, что используется Windows 10 2004 или новее. Проверьте, что stealth mode активен (индикатор в статус-баре).

**Q: Можно отключить блокировку PrintScreen?**  
**A:** Да, через настройки → Приватность → Блокировка скриншотов.

**Q: Где хранятся данные браузера?**  
**A:** `%APPDATA%\GhostBrowser\` — bookmarks.json, history.json, settings.json, downloads.json

**Q: Как работает инкогнито режим?**  
**A:** Создается изолированный WebView2 профиль в `%APPDATA%\GhostBrowser\Incognito\`. При закрытии окна папка полностью удаляется.

**Q: Почему черно-белая тема?**  
**A:** Это уникальный стиль проекта KING. В планах — добавить переключение на цветные темы.

**Q: Нужно ли перезапускать браузер после смены прокси/DoH?**  
**A:** Нет! Настройки применяются автоматически. Выбрал режим — сразу работает. Вкладки сохраняют свои URL.

**Q: Какой прокси использовать для обхода блокировок?**  
**A:** Для DNS-блокировок — DoH Cloudflare или Google. Для полной блокировки сайтов — SOCKS5/HTTP прокси (нужен внешний сервер).

## Разное
- Логотип: KING11.png (шахматная фигура ♚)
- Брендинг: "KING" отображается в заголовке окна
- Скрипт запуска: `run.bat` — dotnet restore → build → run
- Данные: `%APPDATA%\GhostBrowser\`
- DNS пресеты: Google, Cloudflare, OpenDNS, Quad9, AdGuard, Yandex, UncensoredDNS, Control D, NextDNS
