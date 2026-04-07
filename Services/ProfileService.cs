using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using GhostBrowser.Models;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис управления профилями пользователей.
    /// Хранит список профилей в profiles.json.
    /// Каждый профиль соответствует CoreWebView2Profile (через ProfileName).
    /// </summary>
    public class ProfileService
    {
        private readonly string _profilesFile;
        public ObservableCollection<UserProfile> Profiles { get; } = new();

        public ProfileService()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GhostBrowser");
            Directory.CreateDirectory(appData);
            _profilesFile = Path.Combine(appData, "profiles.json");
            LoadProfiles();

            // Если профилей нет — создаём дефолтный
            if (Profiles.Count == 0)
            {
                var defaultProfile = new UserProfile
                {
                    Name = "Основной",
                    AvatarColor = "#0078D4",
                    IsActive = true
                };
                Profiles.Add(defaultProfile);
                SaveProfiles();
            }
        }

        /// <summary>
        /// Возвращает активный профиль.
        /// </summary>
        public UserProfile? GetActiveProfile()
        {
            return Profiles.FirstOrDefault(p => p.IsActive);
        }

        /// <summary>
        /// Возвращает путь к UserDataFolder для активного профиля.
        /// Каждый профиль имеет отдельную папку для полной изоляции.
        /// </summary>
        public string GetActiveProfileUserDataFolder()
        {
            var profile = GetActiveProfile();
            if (profile == null)
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GhostBrowser");

            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GhostBrowser",
                "Profiles",
                SanitizeProfileName(profile.Name));

            Directory.CreateDirectory(appData);
            return appData;
        }

        /// <summary>
        /// Возвращает имя профиля для CoreWebView2 (валидное имя, макс. 64 символа).
        /// </summary>
        public string GetActiveProfileName()
        {
            var profile = GetActiveProfile();
            if (profile == null) return "Default";

            // Валидация имени для WebView2: только допустимые символы
            var name = SanitizeProfileName(profile.Name);
            return string.IsNullOrEmpty(name) ? "Default" : name;
        }

        /// <summary>
        /// Очищает имя профиля от недопустимых символов для WebView2.
        /// Допустимы: a-z, A-Z, 0-9, # @ $ ( ) + - _ ~ . (пробел)
        /// </summary>
        private static string SanitizeProfileName(string name)
        {
            var valid = new string(name.Where(c =>
                char.IsLetterOrDigit(c) ||
                "#@$()+-_~. ".Contains(c)
            ).ToArray()).Trim();

            // Не должен начинаться с пробела, не должен заканчиваться на . или пробел
            valid = valid.TrimStart(' ').TrimEnd('.', ' ');

            return valid.Length > 64 ? valid.Substring(0, 64) : valid;
        }

        public void AddProfile(UserProfile profile)
        {
            // Деактивируем все остальные
            foreach (var p in Profiles) p.IsActive = false;
            profile.IsActive = true;
            Profiles.Add(profile);
            SaveProfiles();
        }

        public void RemoveProfile(Guid id)
        {
            var profile = Profiles.FirstOrDefault(p => p.Id == id);
            if (profile != null && Profiles.Count > 1)
            {
                Profiles.Remove(profile);
                // Если удалили активный — активируем первый
                if (Profiles.Any() && !Profiles.Any(p => p.IsActive))
                    Profiles.First().IsActive = true;
                SaveProfiles();
            }
        }

        public void SetActiveProfile(Guid id)
        {
            foreach (var p in Profiles)
                p.IsActive = (p.Id == id);
            SaveProfiles();
        }

        public void RenameProfile(Guid id, string newName)
        {
            var profile = Profiles.FirstOrDefault(p => p.Id == id);
            if (profile != null && !string.IsNullOrWhiteSpace(newName))
            {
                profile.Name = newName;
                SaveProfiles();
            }
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(_profilesFile))
                {
                    var json = File.ReadAllText(_profilesFile);
                    var profiles = JsonSerializer.Deserialize<List<UserProfile>>(json);
                    if (profiles != null)
                    {
                        foreach (var profile in profiles)
                            Profiles.Add(profile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProfileService load error: {ex.Message}");
            }
        }

        public void SaveProfiles()
        {
            try
            {
                var json = JsonSerializer.Serialize(Profiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_profilesFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProfileService save error: {ex.Message}");
            }
        }
    }
}
