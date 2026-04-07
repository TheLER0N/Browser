using System;

namespace GhostBrowser.Models
{
    /// <summary>
    /// Профиль пользователя браузера.
    /// Каждый профиль имеет изолированные cookies, историю, закладки и настройки WebView2.
    /// Сериализуется в profiles.json.
    /// </summary>
    public class UserProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Отображаемое имя профиля (макс. 64 символа).</summary>
        public string Name { get; set; } = "Профиль";

        /// <summary>Цвет аватара для визуального различения профилей.</summary>
        public string AvatarColor { get; set; } = "#0078D4";

        /// <summary>Активный профиль (только один может быть true).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Дата создания профиля.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
