# Задачи и баги — KING Browser

---

## 🐛 Баги

| # | Описание | Статус |
|---|---|---|
| 1 | Вылет при переключении вкладок | ✅ Исправлен |
| 2 | F11 зависание | ✅ Исправлен |
| 3 | Визуальные артефакты при ресайзе | ✅ Исправлен |
| 4 | DeferredBinaryDeserializerException — GradientStop StaticResource | ✅ Исправлен |

---

## 📋 Подпланы: Обновление дизайна v2.0

### Подплан 1: Title Bar
- **Файлы:** `MainWindow.xaml`
- **Что:** SVG Path иконки для кнопок окна, анимации hover/press
- **Статус:** ✅ completed

### Подплан 2: Навигация
- **Файлы:** `MainWindow.xaml`, `App.xaml`
- **Что:** SVG Path иконки (назад, вперёд, обновить, домой), адресная строка с glow
- **Статус:** ✅ completed

### Подплан 3: Вкладки
- **Файлы:** `App.xaml` (BrowserTabItemStyle)
- **Что:** SVG корона, hover-эффекты, кнопка закрытия
- **Статус:** ✅ completed

### Подплан 4: Status Bar
- **Файлы:** `MainWindow.xaml`
- **Что:** Индикатор загрузки, часы, зум
- **Статус:** ✅ completed

### Подплан 5: Progress Bar
- **Файлы:** `MainWindow.xaml`
- **Что:** Shimmer анимация, белый градиент
- **Статус:** ✅ completed

### Подплан 6: NewTabPage.html
- **Файлы:** `NewTabPage.html`
- **Что:** Логотип ♚, кольца с float анимацией, BW палитра, карточки, поиск
- **Статус:** ✅ completed

### Подплан 7: Bookmarks Bar
- **Файлы:** `MainWindow.xaml`, `App.xaml`
- **Что:** Glass-кнопки закладок, hover-эффекты
- **Статус:** ✅ completed

### Подплан 8: SettingsPage BW
- **Файлы:** `Views/SettingsPage.xaml`
- **Что:** BW тема сайдбара и элементов
- **Статус:** ✅ completed

---

## 🔧 Технические задачи

### Архитектурные улучшения
- ✅ AsyncRelayCommand создан
- ✅ Мёртвый код удалён
- ✅ Обработчики исключений добавлены
- ⏳ DPAPI для хранения паролей

### Инфраструктура
- ✅ .NET 7 → сборка работает
- ✅ KING11.png → Resource в сборке
- ✅ GitHub репозиторий подключён

---

## Среда

- **ОС:** Windows 11
- **.NET:** 7 SDK
- **Хранилище:** `%APPDATA%\KING\`
- **Палитра:** BW Monochrome (см. design.md)
