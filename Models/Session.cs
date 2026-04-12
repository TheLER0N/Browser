using System;
using System.Collections.Generic;

namespace GhostBrowser.Models
{
    /// <summary>
    /// Модель сохранённой сессии браузера.
    /// Содержит список URL открытых вкладок на момент сохранения.
    /// </summary>
    public class Session
    {
        /// <summary>Уникальный идентификатор сессии (GUID).</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Название сессии (задаётся пользователем или авто).</summary>
        public string Name { get; set; } = "";

        /// <summary>Дата и время создания сессии.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Количество вкладок в сессии.</summary>
        public int TabCount => Urls?.Count ?? 0;

        /// <summary>Список URL открытых вкладок.</summary>
        public List<string> Urls { get; set; } = new();
    }
}
