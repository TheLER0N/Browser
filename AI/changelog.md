# История изменений — KING Browser

---

## 2026-04-08

### UI: Редизайн логотипа ♚
- **Файлы:** `NewTabPage.html`
- **Что:** SVG → текстовый символ ♚, контейнер 160×160px, кольца синхронизированы с логотипом (float + scale)
- **Результат:** ✅ Работает

### Rename: KING11 → KING
- **Файлы:** `MainWindow.xaml`, `NewTabPage.html`, `MainWindow.xaml.cs`
- **Что:** Все упоминания KING11 заменены на KING. Title окна = "KING"
- **Результат:** ✅ Работает

---

## 2026-04-07

### AI-система: Рефакторинг документации
- **Файлы:** Все файлы в `AI/`
- **Что:** Удалены дубликаты (onboarding, rules, workflow, bug-handling). Создан README.md — единая точка входа. Остальные файлы переписаны кратко и по делу.
- **Результат:** ✅ 10 файлов вместо 13, без дублей

### Fix: GradientStop StaticResource → HEX
- **Файлы:** `App.xaml`, `MainWindow.xaml`, `IncognitoWindow.xaml`, `Views/SettingsPage.xaml`, `Views/DnsTestWindow.xaml`
- **Что:** 45 мест `Color="{StaticResource ...}"` → `Color="#...."`
- **Результат:** ✅ DeferredBinaryDeserializerException исправлен

### NewTabPage.html: KING дизайн
- **Файлы:** `NewTabPage.html`
- **Что:** BW Monochrome, ambient фон, particles, grid, stealth badge, search bar, quick links, chips
- **Результат:** ✅ Работает

### BW Monochrome тема
- **Файлы:** `App.xaml`
- **Что:** 20+ цветов, 15+ brush-ей, 8 стилей кнопок/контролов, SVG иконки
- **Результат:** ✅ Завершено

### Stealth 2.0
- **Файлы:** `StealthService.cs`, `SettingsPage.xaml`
- **Что:** Блокировка PrintScreen, скриншотов WebView2, Snipping Tool, авто-stealth, anti-fingerprint
- **Результат:** ✅ Завершено

---

## Критические правила изменений

1. GradientStop Color = только HEX, никогда StaticResource
2. Перед изменением — `dotnet clean && dotnet build`
3. После изменения — проверка запуска
4. Каждый коммит — push в main
