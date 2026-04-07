# Дизайн-система — KING Browser

---

## Цветовая палитра (BW Monochrome)

### Фон
| Имя | HEX |
|---|---|
| BgDeepest | `#000000` |
| BgDeep | `#0a0a0a` |
| BgPrimary | `#111111` |
| BgSurface | `#1a1a1a` |
| BgElevated | `#222222` |
| BgOverlay | `#2a2a2a` |

### Акцент
| Имя | HEX |
|---|---|
| Accent | `#ffffff` |
| AccentSoft | `#e0e0e0` |
| AccentDeep | `#cccccc` |
| AccentGlow | `#ffffff` |

### Текст
| Имя | HEX |
|---|---|
| TextPrimary | `#f0f0f0` |
| TextSecondary | `#999999` |
| TextTertiary | `#666666` |
| TextDisabled | `#444444` |

### Рамки
| Имя | HEX |
|---|---|
| BorderSubtle | `#1a1a1a` |
| BorderDefault | `#222222` |
| BorderStrong | `#333333` |

### Статус
| Имя | HEX |
|---|---|
| Danger | `#ff3333` |
| DangerDeep | `#cc0000` |
| Success | `#cccccc` |
| Warning | `#999999` |

**⛔ ЗАПРЕЩЕНО:** фиолетовый, бирюзовый, золотой и любые цвета кроме BW + Danger.

---

## Структура главного окна

```
┌─────────────────────────────────────┐ Row 0:44  ← Title Bar (KING + кнопки)
├─────────────────────────────────────┤ Row 1:Auto ← Вкладки
├─────────────────────────────────────┤ Row 2:Auto ← Навигация + URL
├─────────────────────────────────────┤ Row 3:Auto ← Закладки
├─────────────────────────────────────┤ Row 4:3   ← Прогресс-бар
├─────────────────────────────────────┤ Row 5:*   ← Контент (WebView2)
├─────────────────────────────────────┤ Row 6:28  ← Status Bar
└─────────────────────────────────────┘
```

---

## SVG иконки навигации

| Кнопка | SVG Path |
|---|---|
| Назад | `M10,6 L4,6 M6,3 L3,6 L6,9` |
| Вперёд | `M2,6 L8,6 M6,3 L9,6 L6,9` |
| Обновить | `M2,6 A4,4 0 1,1 6,10 M2,3 L2,7 L6,7` |
| Домой | `M2,6 L6,2 L10,6 M3,5 L3,10 L9,10 L9,5` |
| Плюс | `M7,2 L7,12 M2,7 L12,7` |
| Закрыть | `M2,2 L10,10 M10,2 L2,10` |
| Меню (3 точки) | `M2,2 A1.5,1.5 0 1,1 2,5 ...` |

---

## Анимации

| Эффект | Длительность | Easing |
|---|---|---|
| Hover | 200ms | EaseOut |
| Pressed | 100ms | — |
| Fade in | 300-400ms | PowerEase |
| Pulse (stealth) | 1.5-2s | SineEase |
| Progress shimmer | 1.5s | Linear |
| Logo float | 4.5s | ease-in-out |
| Ring float | 4.5s | ease-in-out |

---

## NewTabPage.html

### Логотип
- ♚ (шахматный король) — белый, 82px
- 2 кольца вокруг с float + pulse анимацией
- Container: 160×160px

### Элементы
- Часы (Consolas, правый верхний угол)
- Приветствие + дата
- Stealth индикатор
- Поиск/URL строка
- 4×2 карточки быстрых ссылок
- Chips (Wikipedia, Twitter, DuckDuckGo, Yandex)
- Footer с горячими клавишами

### Фон
- Ambient: белые blur-пятна (drift анимация)
- Grid: шахматная сетка 80px, opacity 0.025
- Particles: 20 белых точек, rise анимация

---

## Горячие клавиши

| Комбинация | Действие |
|---|---|
| Ctrl+T | Новая вкладка |
| Ctrl+W | Закрыть вкладку |
| Ctrl+L | Фокус на URL |
| Ctrl+R | Обновить |
| Ctrl+Shift+H | Stealth режим |
| Ctrl+Shift+N | Инкогнито |
| Ctrl+D | Закладка |
| Alt+← | Назад |
| Alt+→ | Вперёд |
