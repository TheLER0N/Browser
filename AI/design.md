# ♚ Дизайн — KING Browser

> **Версия:** 2.0 — KING Edition
> **Дата:** Апрель 2026
> **Платформа:** WPF (Windows Presentation Foundation)
> **Стиль:** Шахматная королевская тема, тёмное дерево, золото

---

## 🎨 1. Дизайн-принципы

### 1.1. Философия интерфейса

KING Browser следует трём ключевым принципам:

| Принцип | Описание | Пример |
|---------|----------|--------|
| **Величие** | Интерфейс подчёркивает статус пользователя — он «король» | Золотые акценты, шахматные элементы |
| **Стратегия** | Чистый, продуманный интерфейс без лишнего | Минимум элементов, максимум контроля |
| **Элегантность** | Тёмное дерево + золото = роскошь | Цветовая палитра дерева и золота |

### 1.2. Визуальный язык

- **Форма:** Скруглённые углы, элегантные линии
- **Глубина:** Тени и градиенты для ощущения «доски»
- **Контраст:** Тёмное дерево + золотой текст
- **Анимации:** Плавные, королевские (не спеша)

---

## 2. Цветовая палитра KING

### 2.1. Основные цвета

| Название | HEX | Назначение |
|----------|-----|-----------|
| **KingBg** | `#1a1208` | Основной фон (тёмное дерево) |
| **KingSurface** | `#2d1f0e` | Фон панелей (доска) |
| **KingSurfaceLight** | `#3d2b14` | Светлые панели |
| **KingGold** | `#d4a017` | Золотой акцент (королевский) |
| **KingGoldLight** | `#f0c040` | Светлое золото (hover) |
| **KingGoldDark** | `#a07818` | Тёмное золото |
| **KingText** | `#f5e6c8` | Основной текст (кремовый) |
| **KingTextMuted** | `#8b7355` | Вторичный текст |
| **KingPurple** | `#6b3fa0` | Королевский фиолетовый |
| **KingPurpleLight** | `#8b5fbf` | Светлый фиолетовый |
| **KingSuccess** | `#2d5a1e` | Зелёный (успех) |
| **KingDanger** | `#8b1a1a` | Красный (ошибка) |
| **KingBoard1** | `#d4a574` | Светлая клетка доски |
| **KingBoard2** | `#8b6914` | Тёмная клетка доски |
| **KingProgress** | `#d4a017` → `#f0c040` | Градиент прогресс-бара |

### 2.2. Ресурсы App.xaml (KING Edition)

```xml
<SolidColorBrush x:Key="BgDeepestBrush" Color="#1a1208" />
<SolidColorBrush x:Key="BgDeepBrush" Color="#2d1f0e" />
<SolidColorBrush x:Key="BgSurfaceBrush" Color="#3d2b14" />
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#f5e6c8" />
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#c4a882" />
<SolidColorBrush x:Key="TextTertiaryBrush" Color="#8b7355" />
<SolidColorBrush x:Key="AccentBrush" Color="#d4a017" />
<SolidColorBrush x:Key="AccentSoftBrush" Color="#f0c040" />
<SolidColorBrush x:Key="AccentDeepBrush" Color="#a07818" />
<SolidColorBrush x:Key="BorderSubtleBrush" Color="#5a4228" />
<SolidColorBrush x:Key="SuccessBrush" Color="#2d5a1e" />
<SolidColorBrush x:Key="DangerBrush" Color="#8b1a1a" />
<SolidColorBrush x:Key="PurpleBrush" Color="#6b3fa0" />
```

### 2.3. Типографика

| Элемент | Шрифт | Размер | Начертание |
|---------|-------|--------|-----------|
| Заголовок окна | Playfair Display / Georgia | 14px | Bold |
| Текст вкладок | Segoe UI | 12px | Regular |
| Адресная строка | Segoe UI | 13px | Regular |
| Кнопки навигации | Segoe UI Emoji | 16px | Regular |
| Статус-бар | Consolas | 11px | Regular |
| Часы | Consolas | 11px | Regular |
| Логотип KING | Playfair Display / Georgia | 16px | Bold |

---

## 3. Структура главного окна (MainWindow)

### 2.1. Общая схема

```
┌─────────────────────────────────────────────────────────┐ ← Row 0: Title Bar
│ 👻 GhostBrowser    ● ● ●                    [─] [□] [✕] │ 70px высота
├─────────────────────────────────────────────────────────┤ ← Row 1: Вкладки
│ [Вкладка 1] [Вкладка 2] [+]                             │ 35px высота
├─────────────────────────────────────────────────────────┤ ← Row 2: Навигация
│ [←] [→] [↻] [🏠] [地址栏____________] [☆] [G] [👻] [⋮] │ 45px высота
├─────────────────────────────────────────────────────────┤ ← Row 3: Закладки
│ [Google] [YouTube] [GitHub] ... →                       │ 35px высота (скролл)
├─────────────────────────────────────────────────────────┤ ← Row 4: Прогресс
│ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  3px высота
├─────────────────────────────────────────────────────────┤ ← Row 5: Content
│                                                         │
│                                                         │
│              WebView2  ИЛИ  SettingsPage                │
│                                                         │
│                                                         │
├─────────────────────────────────────────────────────────┤ ← Row 6: Status Bar
│ Готово                                    100%  14:32:45 │ 25px высота
└─────────────────────────────────────────────────────────┘
```

### 2.2. Row 0 — Title Bar

| Элемент | Расположение | Стиль | Действие |
|---------|-------------|-------|----------|
| Логотип 👻 + "GhostBrowser" | Left, margin 10px | Bold, 14px | — |
| Индикатор stealth mode | Right of logo | Зелёный кружок если ON, серый если OFF | — |
| Кнопки управления окном | Right edge | 3 кнопки: свернуть, развернуть, закрыть | Win32 через `WindowInteropHelper` |

**Особенности:**
- `WindowStyle="None"` — системный title bar скрыт
- `AllowsTransparency="True"` — прозрачность фона для кастомной рамки
- `WindowChrome` — позволяет менять размер окна мышью по краям
- `SnapsToDevicePixels="True"` + `UseLayoutRounding="True"` — предотвращает субпиксельное размытие

**Цветовая схема:**
```xml
<!-- Активный режим -->
StealthIndicatorBorder.Background = SuccessBrush (зелёный)
StealthStatusText.Text = "Stealth: ON"

<!-- Неактивный режим -->
StealthIndicatorBorder.Background = TextMutedBrush (серый)
StealthStatusText.Text = "Stealth: OFF"
```

### 2.3. Row 1 — Панель вкладок

| Элемент | Стиль | Действие |
|---------|-------|----------|
| TabItem (каждая вкладка) | Тёмный фон, белый текст, кнопка ✕ | Клик → SelectedTab, ✕ → CloseTabCommand |
| Кнопка "+" | Минимальная, справа от вкладок | AddTabCommand |

**DataTemplate для TabItem:**
- `TextBlock` с `Text="{Binding Title}"` — заголовок страницы
- `Button` с командой `CloseTabCommand` и `CommandParameter="{Binding}"` — кнопка закрытия

**Поведение:**
- При закрытии последней вкладки — окно закрывается
- При закрытии активной — выбирается соседняя (влево, если есть)

### 2.4. Row 2 — Навигационная панель

| Кнопка | Иконка | Команда | Горячая клавиша |
|--------|--------|---------|----------------|
| Назад | ← | GoBackCommand | Alt+← / BrowserBack |
| Вперёд | → | GoForwardCommand | Alt+→ / BrowserForward |
| Обновить | ↻ | RefreshCommand | F5 / Ctrl+R |
| Домой | 🏠 | GoHomeCommand | — |
| Адресная строка | TextBox | NavigateCommand (Enter) | Ctrl+L |
| Закладка | ☆ / ★ | ToggleBookmarkCommand | Ctrl+D |
| Поисковик | G / B / D / Я | CycleSearchEngineCommand (клик) | — |
| Stealth | 👻 | ToggleStealthCommand | Ctrl+Shift+H |
| Меню | ⋮ | Open/Close Settings | — |

**Адресная строка (UrlBox):**
- Placeholder: "Введите URL или поисковый запрос" (через визуальный триггер при пустом тексте)
- При фокусе — выделяет весь текст (`SelectAll`)
- При вводе — `KeyDown` → Enter → `NavigateCommand`

**Индикатор поисковика:**
- Google → "G"
- Bing → "B"
- DuckDuckGo → "D"
- Yandex → "Я"
- Клик по иконке → переключение на следующий поисковик

### 2.5. Row 3 — Панель закладок

| Элемент | Стиль | Привязка |
|---------|-------|----------|
| ItemsControl | Horizontal StackPanel, ScrollViewer | `ItemsSource="{Binding BookmarkService.Bookmarks}"` |
| Button (каждая закладка) | Минимальная, текст = Title закладки | `Command="{Binding DataContext.NavigateToBookmarkCommand}"`, `CommandParameter="{Binding Url}"` |

**Особенности:**
- Автоматическое обновление через `ObservableCollection`
- Горизонтальный скролл если закладок много
- Клик → переход по URL закладки

### 2.6. Row 4 — Прогресс-бар загрузки

| Параметр | Значение |
|----------|----------|
| Высота | 3px |
| Цвет | Градиент: `#0078D4` → `#00BCF2` |
| Привязка | `Width="{Binding Progress, Converter={ProgressToWidthConverter}}"` |
| Видимость | `Visible` если `IsLoading == true`, иначе `Collapsed` |

**Поведение:**
- Навигация началась → Progress = 10, IsLoading = true
- ContentLoading → Progress = 50
- NavigationCompleted → Progress = 100, IsLoading = false
- Через 500мс → Progress = 0 (константа `ProgressResetDelayMs`)

### 2.7. Row 5 — ContentArea

**ContentControl** с `Content="{Binding DisplayedContent}"`:

| Сценарий | DisplayedContent | Что рендерится |
|----------|-----------------|----------------|
| Обычный режим | `SelectedTab.WebView` | WebView2 (Chromium) |
| Настройки открыты | `SettingsPage` | UserControl с 6 разделами |

**DataTemplate для TabViewModel:**
```xml
<DataTemplate DataType="{x:Type vm:TabViewModel}">
    <ContentControl Content="{Binding WebView}" />
</DataTemplate>
```

**Переключение:**
- Открытие настроек → `DisplayedContent = _settingsPage`
- Закрытие настроек → `DisplayedContent = SelectedTab?.WebView`

### 2.8. Row 6 — Status Bar

| Элемент | Расположение | Привязка |
|---------|-------------|----------|
| StatusText | Left | `StatusText` (навигация, ошибки) |
| Zoom level | Right of StatusText | Заглушка "100%" (пока не реализуется) |
| ClockTime | Right edge | `ClockTime` (обновляется каждую секунду) |

---

## 3. Страница настроек (SettingsPage)

### 3.1. Общая схема

```
┌─────────────────────────────────────────────────────────┐
│  Сайдбар (230px)  │  Контент (ScrollViewer)             │
│                   │                                     │
│  [⚙ DNS]         │  Заголовок раздела                   │
│  [🔧 Общие]      │  ─────────────────                   │
│  [🔒 Приватность]│                                     │
│  [📜 История]    │  Элементы управления разделом        │
│  [⭐ Закладки]   │                                     │
│  [ℹ️ О программе]│                                     │
│                   │                                     │
│                   │  [Кнопка "Тест"]  [Кнопка "Сохр."]   │
└─────────────────────────────────────────────────────────┘
```

### 3.2. Сайдбар

| Кнопка | Раздел | Иконка |
|--------|--------|--------|
| DNS | Настройки DNS | ⚙ |
| Общие | Общие настройки | 🔧 |
| Приватность | Блокировка трекеров | 🔒 |
| История | Список посещений | 📜 |
| Закладки | Список закладок | ⭐ |
| О программе | Информация | ℹ️ |

**Механизм переключения:**
- `ShowSection(sectionName)` → устанавливает `Visibility` каждой секции
- Активная кнопка подсвечивается (через стиль `IsEnabled` или `Background`)

### 3.3. Раздел: DNS

| Элемент | Тип | Описание |
|---------|-----|----------|
| ToggleSwitch | CheckBox/Toggle | Включить/выключить кастомный DNS |
| ComboBox | Выпадающий список | Пресеты DNS (14 серверов: Google, Cloudflare, OpenDNS, Quad9, AdGuard, Yandex и др.) |
| TextBox | Поле ввода | Ручной ввод DNS-сервера |
| Кнопка "Тест" | Button | Запуск `SettingsService.TestDnsAsync()` |
| Результаты | TextBlock / ListView | Вывод результатов теста (5 сайтов: Google, Gmail, Cloudflare, Gemini, YouTube) |

**Модальное окно результатов:**
- `DnsTestWindow` — открывается модально
- Принимает `List<string>` с результатами
- Отображает в `ScrollViewer`

### 3.4. Раздел: Общие

| Элемент | Тип | Описание |
|---------|-----|----------|
| Тёмная тема | CheckBox | Вкл/выкл (по умолчанию true) |
| Размер шрифта | Slider | Диапазон: 10-24px (по умолчанию 14) |
| Домашняя страница | TextBox | URL или "ghost://newtab" |

### 3.5. Раздел: Приватность

| Элемент | Тип | Описание |
|---------|-----|----------|
| Блокировка трекеров | CheckBox | (по умолчанию true) |
| Блокировка cookies третьих сторон | CheckBox | (по умолчанию true) |

### 3.6. Раздел: История

| Элемент | Тип | Описание |
|---------|-----|----------|
| ListView | Список записей | `ItemsSource="{Binding HistoryService.History}"` |
| Колонки | Title, Url, VisitedAt | Форматирование даты |
| Кнопка "Очистить" | Button | `HistoryService.ClearHistory()` |

### 3.7. Раздел: Закладки

| Элемент | Тип | Описание |
|---------|-----|----------|
| ListView | Список закладок | `ItemsSource="{Binding BookmarkService.Bookmarks}"` |
| Колонки | Title, Url | Клик → переход |
| Кнопка "Удалить" | Button | Удаление выбранной закладки |

### 3.8. Раздел: О программе

| Элемент | Описание |
|---------|----------|
| Логотип | 👻 GhostBrowser |
| Версия | 1.0 |
| Список горячих клавиш | Таблица с описанием всех команд |

---

## 4. Окно инкогнито (IncognitoWindow)

### 4.1. Отличия от MainWindow

| Параметр | MainWindow | IncognitoWindow |
|----------|-----------|-----------------|
| Цвет прогресс-бара | Синий (`#0078D4`) | Фиолетовый (`#8B5CF6`) |
| Индикатор режима | 👻 Stealth | 🕶️ Incognito |
| История | Сохраняется | **Не сохраняется** |
| Закладки | Сохраняются | Доступны только для чтения |
| UserDataFolder | `%APPDATA%\GhostBrowser` | Временная папка (удаляется при закрытии) |
| Заголовок окна | "GhostBrowser" | "GhostBrowser — Инкогнито" |

### 4.2. Визуальный индикатор

- Фиолетовый баннер в верхней части окна
- Текст "INCOGNITO" жирным шрифтом
- Пояснение: "История не сохраняется"

---

## 5. Страница новой вкладки (NewTabPage.html)

### 5.1. Содержимое

HTML-страница, загружаемая через `NavigateToString()` при открытии пустой вкладки.

**Структура:**
- Логотип GhostBrowser (большой, по центру)
- Поисковая строка (по центру, с autofocus)
- Быстрые ссылки (избранные закладки)
- Часы (опционально)

**Стиль:**
- Тёмный фон (`#1a1a2e`)
- Белый текст
- Центрирование через Flexbox
- Встроенные CSS (data:URI, нет внешних зависимостей)

**Кодировка:** UTF-8 (обязательно для корректного отображения кириллицы)

---

## 6. Цветовая палитра

### 6.1. Основные цвета

| Название | HEX | Назначение |
|----------|-----|-----------|
| Background | `#1E1E1E` | Основной фон окна |
| Surface | `#2D2D2D` | Фон панелей (title bar, навигация) |
| Text | `#FFFFFF` | Основной текст |
| TextMuted | `#A0A0A0` | Вторичный текст (placeholder, stealth OFF) |
| Accent | `#0078D4` | Кнопки, ссылки, фокус |
| Success | `#107C10` | Индикатор stealth ON |
| Danger | `#E81123` | Ошибки, кнопка закрытия вкладки |
| Progress | `#0078D4` → `#00BCF2` | Градиент прогресс-бара |
| Incognito | `#8B5CF6` | Фиолетовый для режима инкогнито |

### 6.2. Ресурсы App.xaml

```xml
<SolidColorBrush x:Key="BackgroundBrush" Color="#1E1E1E" />
<SolidColorBrush x:Key="SurfaceBrush" Color="#2D2D2D" />
<SolidColorBrush x:Key="TextBrush" Color="#FFFFFF" />
<SolidColorBrush x:Key="TextMutedBrush" Color="#A0A0A0" />
<SolidColorBrush x:Key="AccentBrush" Color="#0078D4" />
<SolidColorBrush x:Key="SuccessBrush" Color="#107C10" />
<SolidColorBrush x:Key="DangerBrush" Color="#E81123" />
```

---

## 7. Типографика

| Элемент | Шрифт | Размер | Начертание |
|---------|-------|--------|-----------|
| Заголовок окна | Segoe UI | 14px | Bold |
| Текст вкладок | Segoe UI | 12px | Regular |
| Адресная строка | Segoe UI | 13px | Regular |
| Кнопки навигации | Segoe UI Emoji | 16px | Regular |
| Статус-бар | Consolas | 11px | Regular |
| Часы | Consolas | 11px | Regular |

---

## 8. Горячие клавиши

| Комбинация | Действие | Команда |
|------------|----------|---------|
| `Ctrl+T` | Новая вкладка | AddTabCommand |
| `Ctrl+W` | Закрыть вкладку | CloseTabCommand |
| `Ctrl+L` | Фокус на адресную строку | FocusUrlCommand |
| `Ctrl+D` | Добавить/удалить закладку | ToggleBookmarkCommand |
| `Ctrl+R` / `F5` | Обновить страницу | RefreshCommand |
| `Alt+←` | Назад | GoBackCommand |
| `Alt+→` | Вперёд | GoForwardCommand |
| `Ctrl+Shift+H` | Переключить stealth | ToggleStealthCommand |
| `Ctrl+Shift+N` | Режим инкогнито | OpenIncognitoCommand |
| `F11` | Полноэкранный режим | ToggleFullScreen |
| `Esc` | Выход из полноэкранного режима | ToggleFullScreen |

---

## 9. Адаптивность и ресайз

### 9.1. Поведение при изменении размера

- `Window_SizeChanged` → `InvalidateVisual()` + `UpdateLayout()` через Dispatcher
- `SnapsToDevicePixels="True"` + `UseLayoutRounding="True"` — предотвращает размытие
- WebView2 автоматически масштабируется через Chromium (DPI-aware)

### 9.2. Минимальные размеры окна

| Параметр | Значение |
|----------|----------|
| MinWidth | 800px |
| MinHeight | 600px |

### 9.3. Полноэкранный режим (F11)

**Последовательность входа:**
1. Сохранить: `WindowState`, `Width`, `Height`, `Left`, `Top`, `WindowStyle`, `ResizeMode`
2. `WindowStyle = None`, `ResizeMode = NoResize`
3. `WindowState = Normal`
4. `Topmost = true`, `Left = 0`, `Top = 0`
5. `Width = SystemParameters.PrimaryScreenWidth`
6. `Height = SystemParameters.PrimaryScreenHeight`

**Последовательность выхода:**
1. Восстановить все сохранённые параметры
2. `Topmost = false`
3. `InvalidateVisual()` + `UpdateLayout()` через `Dispatcher.InvokeAsync(Background)`

---

## 10. Правила изменения UI

### 10.1. Перед изменением XAML

1. Прочитать весь `<Grid>` целиком
2. Посчитать `ColumnDefinitions` / `RowDefinitions`
3. Проверить каждый `Grid.Column` / `Grid.Row` элемент
4. Убедиться, что элементы не перекрываются

### 10.2. После изменения XAML

1. Перечитать весь XAML файл
2. Проверить что все элементы на своих местах
3. Убедиться, что `Margin`/`Padding` корректны
4. Скомпилировать проект (`dotnet build`) для проверки

### 10.3. Запреты

- ❌ НЕ использовать фиксированные размеры для контейнеров (только `Auto` или `*`)
- ❌ НЕ добавлять элементы в Grid без проверки колонок/строк
- ❌ НЕ создавать UI-элементы вне UI-потока
- ❌ НЕ менять стили без согласования с пользователем

---

## 11. Ссылки на файлы

| Компонент | Файл |
|-----------|------|
| Главное окно | `MainWindow.xaml`, `MainWindow.xaml.cs` |
| Глобальные стили | `App.xaml` |
| Страница настроек | `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs` |
| Окно DNS-теста | `Views/DnsTestWindow.xaml`, `Views/DnsTestWindow.xaml.cs` |
| Окно инкогнито | `IncognitoWindow.xaml`, `IncognitoWindow.xaml.cs` |
| Новая вкладка | `NewTabPage.html` |

---

*Файл обновляется при изменении UI-компонентов или стилей.*
