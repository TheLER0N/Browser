using System;

namespace GhostBrowser.Models
{
    /// <summary>
    /// Профиль автозаполнения форм.
    /// Хранит персональные данные для автоматического заполнения веб-форм.
    /// Сериализуется в autofill.json.
    /// </summary>
    public class AutoFillProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Мой профиль";

        // ═══ Личные данные ═══
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string MiddleName { get; set; } = "";

        // ═══ Контакты ═══
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        // ═══ Адрес ═══
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string Country { get; set; } = "";

        // ═══ Активный профиль ═══
        public bool IsActive { get; set; } = true;
    }
}
