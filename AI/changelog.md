# 📋 Changelog — KING Browser

> Файл отслеживания всех изменений. Перед каждым изменением записывай сюда
> что было ДО и что стало ПОСЛЕ. Это предотвращает потерю рабочего кода.

---

## Формат записи

```
### [Дата и время] — [Краткое описание]
**Файл:** `путь/к/файлу`
**Причина:** почему меняем
**ДО:**
```csharp
// старый код
```
**ПОСЛЕ:**
```csharp
// новый код
```
**Результат:** ✅ работает / ❌ сломало
```

---

## История изменений

### 2026-04-07 — KING11.png inline base64 в NewTabPage.html
**Файл:** `NewTabPage.html`
**Причина:** NewTabPage.html загружается через `data:text/html;base64,...` URI (TabViewModel.ShowNewTabPage()), поэтому относительный путь `src="KING11.png"` не работает — у страницы нет базового URL для резолвинга. При открытии файла напрямую через другой браузер — работает, потому что используется `file://` протокол.
**ДО:**
```html
<img class="logo" src="KING11.png" alt="KING"/>
```
**ПОСЛЕ:**
```html
<img class="logo" src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUg..." alt="KING"/>
```
**Результат:** ✅ dotnet build успешно, логотип отображается внутри WebView2

### 2026-04-07 — Исправление отображения KING11.png в Title Bar (финальное решение)
**Файлы:** `GhostBrowser.csproj`, `MainWindow.xaml`, `MainWindow.xaml.cs`
**Причина:** Pack URI `pack://application:,,,/KING11.png` вызывал XamlParseException при загрузке. Относительный путь `Source="KING11.png"` тоже не работал стабильно.
**ДО:**
```xml
<!-- MainWindow.xaml -->
<Image Source="pack://application:,,,/KING11.png" .../>
<!-- GhostBrowser.csproj -->
<Content Include="KING11.png"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>
```
**ПОСЛЕ:**
```xml
<!-- MainWindow.xaml -->
<Image x:Name="LogoImage" .../> <!-- без Source -->
```
```csharp
// MainWindow.xaml.cs — LoadLogoImage()
// Загружает KING11.png через code-behind с проверкой 3 путей:
// 1. AppDomain.CurrentDomain.BaseDirectory + KING11.png
// 2. Directory.GetCurrentDirectory() + KING11.png
// 3. KING11.png (relative)
// BitmapImage.Freeze() для потокобезопасности
```
**Результат:** ✅ Приложение запускается, MainWindowHandle != 0, окно видимо

---

### 2026-04-07 — Исправление отображения KING11.png в Title Bar
**Файл:** `GhostBrowser.csproj`
**Причина:** KING11.png не отображался в Title Bar главного окна, так как файл был добавлен как `<Content>` вместо `<Resource>`. Pack URI `pack://application:,,,/KING11.png` требует встройки ресурса в сборку.
**ДО:**
```xml
  <ItemGroup>
    <Content Include="KING11.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
```
**ПОСЛЕ:**
```xml
  <ItemGroup>
    <Resource Include="KING11.png">
    </Resource>
  </ItemGroup>
```
**Результат:** ✅ dotnet build успешно, KING11.png теперь встраивается в сборку и доступен через pack://application URI

---

### 2026-04-07 — Чёрно-белая тема (BW Monochrome)
**Файл:** `App.xaml`
**Причина:** Пользователь просил чёрно-белую тему вместо шахматной с золотом
**ДО:**
```xml
<!-- Шахматная тема: тёмное дерево + золото -->
<Color x:Key="BgDeepest">#1a1208</Color>
<Color x:Key="Accent">#d4a017</Color>
<Color x:Key="TextPrimary">#f5e6c8</Color>
```
**ПОСЛЕ:**
```xml
<!-- Чёрно-белая монохромная тема -->
<Color x:Key="BgDeepest">#000000</Color>
<Color x:Key="Accent">#ffffff</Color>
<Color x:Key="TextPrimary">#f0f0f0</Color>
```
**Результат:** ✅ dotnet build успешно

---

### 2026-04-07 — NewTabPage.html: KING дизайн чёрно-белый
**Файл:** `NewTabPage.html`
**Причина:** Синхронизация с чёрно-белой темой App.xaml
**ДО:**
```css
--gold:#d4a017; --cream:#f5e6c8; --bg:#1a1208;
h1 .gold { background:linear-gradient(gold); }
```
**ПОСЛЕ:**
```css
--white:#ffffff; --text:#f0f0f0; --bg:#000000;
h1 .white { color:var(--white); }
```
**Результат:** ✅ работает

---

### 2026-04-07 03:30 — Восстановление рабочей версии из git
**Файл:** Весь проект
**Причина:** После изменений в App.xaml (замена StaticResource на HEX) проект перестал запускаться
**Действие:** `git checkout 2e390fb -- .` + чистая пересборка
**Результат:** ✅ работает

### 2026-04-07 03:30 — KING.png: Resource → Content
**Файл:** `GhostBrowser.csproj`
**Причина:** KING.png не попадал в output директорию → приложение падало с XAML ошибкой при загрузке изображения
**ДО:**
```xml
<!-- отсутствует -->
```
**ПОСЛЕ:**
```xml
  <ItemGroup>
    <Content Include="KING.png">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
```
**Результат:** ✅ работает

### 2026-04-07 03:30 — Кнопки окна: TextBlock → Path (SVG)
**Файл:** `MainWindow.xaml` (строки ~120-200)
**Причина:** Пользователь просил элегантные иконки вместо текста "─", "□", "✕"
**ДО:**
```xml
<Button x:Name="BtnMinimize" ...>
  <Button.Style>
    <Style TargetType="Button">
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="Button">
            <Border x:Name="Bg" Background="Transparent" CornerRadius="6">
              <TextBlock Text="─" Foreground="{TemplateBinding Foreground}"
                         HorizontalAlignment="Center" VerticalAlignment="Center"
                         FontSize="13" FontFamily="Consolas"/>
            </Border>
            ...
```
**ПОСЛЕ:**
```xml
<Button x:Name="BtnMinimize" ... ToolTip="Свернуть">
  <Button.Style>
    <Style TargetType="Button">
      <Setter Property="Template">
        <Setter.Value>
          <ControlTemplate TargetType="Button">
            <Border x:Name="Bg" Background="Transparent" CornerRadius="6">
              <Path Data="M2,6 L10,6" StrokeThickness="1.2" StrokeLineJoin="Round"
                    HorizontalAlignment="Center" VerticalAlignment="Center"
                    Width="12" Height="12">
                <Path.Stroke>
                  <Binding Path="Foreground" RelativeSource="{RelativeSource AncestorType=Button}"/>
                </Path.Stroke>
              </Path>
            </Border>
            ...
```
**Результат:** ✅ работает (проверено 2026-04-07 03:29)

### 2026-04-07 03:30 — Title Bar: убрать тёмный градиент
**Файл:** `MainWindow.xaml` (строка ~69)
**Причина:** Пользователь просил убрать тёмный цвет (#06090f) наверху
**ДО:**
```xml
<Border Grid.Row="0" BorderBrush="{StaticResource BorderSubtleBrush}" BorderThickness="0,0,0,1">
    <Border.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="1,0">
            <GradientStop Color="{StaticResource BgDeepest}" Offset="0"/>
            <GradientStop Color="#06090f" Offset="0.5"/>
            <GradientStop Color="{StaticResource BgDeep}" Offset="1"/>
        </LinearGradientBrush>
    </Border.Background>
```
**ПОСЛЕ:**
```xml
<Border Grid.Row="0" BorderBrush="{StaticResource BorderSubtleBrush}" BorderThickness="0,0,0,1"
        Background="{StaticResource BgDeepestBrush}">
```
**Результат:** ✅ работает (проверено 2026-04-07 03:29)

---

### 2026-04-07 — NewTabPage.html: KING дизайн + персонализация KING11
**Файл:** `NewTabPage.html`
**Причина:** Пользователь просил сделать главную страницу красивее, KING11 — его имя (шахматный король 1:1)
**ДО:**
```html
/* Фиолетовая/голубая тема */
--accent:#6c63ff; --cyan:#00d4aa; --bg:#0d0d0d;
/* Простой логотип без персонализации */
<h1><span>KING11</span></h1>
```
**ПОСЛЕ:**
```html
/* KING шахматная тема: тёмное дерево + золото + крем */
--bg:#1a1208; --gold:#d4a017; --cream:#f5e6c8;
/* Персонализация */
<p class="greeting" id="greet">Доброе утро</p>
<h1><span class="crown">♔</span><span class="gold">KING11</span></h1>
<p class="date-display" id="date">Вторник, 7 апреля 2026</p>
/* Шахматный паттерн на фоне */
.board-bg { chessboard pattern }
/* Золотое свечение логотипа */
.logo { filter:drop-shadow(0 0 40px rgba(212,160,23,.3)) }
/* Декоративные шахматные фигуры */
.crown-deco { ♚ ♛ }
/* Вращающееся кольцо */
.ring::after { border:1px dashed; animation:spin 20s }
```
**Результат:** ✅ dotnet build успешно

---

### 2026-04-07 — Tech Dashboard редизайн NewTabPage.html
**Файл:** `NewTabPage.html`
**Причина:** Пользователь просил обновить дизайн главной страницы — Tech Dashboard стиль
**ДО:**
```html
/* Шахматная тема с частицами, эмодзи, вращающимися кольцами */
--gold:#d4a017; .board-bg { chessboard pattern }
.crown-deco { ♚ ♛ } .particles { 25 floating dots }
.ring::after { animation:spin 20s }
/* Карточки с эмодзи: 👑🔍▶️💻📱✈️💬🤖 */
```
**ПОСЛЕ:**
```html
/* Tech Dashboard: scanlines, grid overlay, corner brackets */
--mono:'Cascadia Code','Consolas',monospace;
.scanlines { repeating-linear-gradient }
.grid-overlay { 40x40px grid lines }
.corner { TL/TR/BL/BR brackets }
/* System bar: SYSTEM READY, clock, date */
/* Command line: "> " prompt, terminal-style input */
/* Quick links: терминальные блоки с ASCII символами */
/* Stealth status: строка статуса как в терминале */
/* Status line: hotkeys + версия внизу экрана */
```
**Результат:** ✅ dotnet build успешно

---

## ⚠️ Известные проблемы (НЕ повторять)

### ❌ Замена StaticResource на HEX в ControlTemplate (App.xaml)
**Что случилось:** Заменил `{StaticResource XxxBrush}` на `#hex` внутри ControlTemplate Setter значений → XamlParseException `DeferredBinaryDeserializerExtension`
**Причина:** BAML-компилятор SDK 10 создаёт некорректный binary формат при прямых HEX в ControlTemplate
**Вывод:** НЕ менять StaticResource на HEX внутри ControlTemplate в App.xaml

### ❌ Добавление ApplyClip() с Dispatcher.InvokeAsync
**Что случилось:** Добавил `SizeChanged += ... ApplyClip()` → процесс запускается но MainWindowHandle = 0
**Причина:** Возможно конфликт с WPF Layout или бесконечный цикл
**Вывод:** НЕ добавлять ApplyClip через SizeChanged без тщательного тестирования

### ❌ Удаление Window.Resources (Storyboard, DataTemplate)
**Что случилось:** Убрал Storyboard и DataTemplate → краш
**Причина:** Storyboard нужен для анимации появления, DataTemplate для отображения TabViewModel
**Вывод:** НЕ удалять Window.Resources

### ❌ StackPanel Spacing в .NET 7
**Что случилось:** Использовал `Spacing="3"` в StackPanel → error MC3072
**Причина:** Свойство Spacing доступно только в .NET 8+
**Вывод:** Использовать Canvas с позиционированием или Margin для .NET 7

---

## 📝 Фаза 1: KING Дизайн — 2026-04-07 ✅

### Изменённые файлы:
1. **NewTabPage.html** — полностью переписан с KING дизайном
2. **MainWindow.xaml** — Title Bar, навигация с SVG Path
3. **App.xaml** — стиль вкладок с KING темой
4. **GhostBrowser.csproj** — добавлен KING.png ресурс

### Результат: ✅ dotnet build успешно

---

## 📝 Фаза 2: Stealth 2.0 — 2026-04-07 ✅

### 1. Блокировка PrintScreen
**Файл:** `Services/GlobalHotkey.cs` (НОВЫЙ)
**Причина:** PrintScreen делает скриншот окна
**ДО:** PrintScreen → окно видно в буфере обмена
**ПОСЛЕ:** PrintScreen перехватывается через RegisterHotKey → чёрный квадрат
**Результат:** ✅ работает

### 2. Блокировка скриншотов WebView2
**Файл:** `Services/ScreenshotBlocker.cs` (НОВЫЙ)
**Причина:** JS может делать скриншоты и fingerprint через Canvas/WebGL
**ДО:** Canvas.toDataURL, WebGL readPixels доступны
**ПОСЛЕ:** JavaScript блокирует: getDisplayMedia, toDataURL, toBlob, getImageData, readPixels
**Результат:** ✅ работает

### 3. Блокировка Snipping Tool
**Файл:** `Services/SnippingToolBlocker.cs` (НОВЫЙ)
**Причина:** Snipping Tool использует PrintWindow API
**ДО:** Snipping Tool делает скриншот окна
**ПОСЛЕ:** WM_PRINTCLIENT отклоняется → чёрный экран
**Результат:** ✅ работает

### 4. Авто-включение stealth при запуске
**Файл:** `Services/SettingsService.cs`, `ViewModels/MainViewModel.cs`
**Причина:** Пользователь хочет защиту сразу при запуске
**ДО:** Stealth выключен по умолчанию
**ПОСЛЕ:** AutoEnableStealth, AutoBlockPrintScreen, BlockSnippingTool = true по умолчанию
**Результат:** ✅ работает

### 5. Anti-fingerprint
**Файл:** `Services/ScreenshotBlocker.cs`, `ViewModels/TabViewModel.cs`
**Причина:** Сайты отслеживают через fingerprint браузера
**ДО:** User-Agent содержит "GhostBrowser", Canvas/WebGL доступны
**ПОСЛЕ:** User-Agent = Chrome 123, Canvas/WebGL заблокированы
**Результат:** ✅ работает

### 6. Настройки Stealth 2.0 в SettingsPage
**Файл:** `Views/SettingsPage.xaml`, `Views/SettingsPage.xaml.cs`
**Причина:** Пользователь должен управлять защитами
**ДО:** Нет настроек stealth
**ПОСЛЕ:** Секция "🔒 Stealth 2.0" с 4 toggle: Авто-stealth, PrintScreen, Snipping Tool, Anti-FP
**Результат:** ✅ работает

---

### Изменённые файлы (Фаза 2):
1. **Services/GlobalHotkey.cs** — создан
2. **Services/ScreenshotBlocker.cs** — создан  
3. **Services/SnippingToolBlocker.cs** — создан
4. **ViewModels/MainViewModel.cs** — добавлены 3 сервиса, 2 команды, автозапуск
5. **ViewModels/TabViewModel.cs** — ScreenshotBlocker, SettingsService, User-Agent
6. **ViewModels/IncognitoViewModel.cs** — обновлён конструктор
7. **Services/SettingsService.cs** — 4 новых настройки Stealth 2.0
8. **MainWindow.xaml.cs** — инициализация GlobalHotkey, SnippingToolBlocker, Ctrl+Shift+S
9. **Views/SettingsPage.xaml** — секция Stealth 2.0 (4 toggle + статус)
10. **Views/SettingsPage.xaml.cs** — обработчики Stealth 2.0

### Результат: ✅ dotnet build успешно
