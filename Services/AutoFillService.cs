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
    /// Сервис автозаполнения форм.
    /// Хранит профили данных пользователей и генерирует JavaScript
    /// для автоматического заполнения input/select/textarea полей на странице.
    /// </summary>
    public class AutoFillService
    {
        private readonly string _autofillFile;
        public ObservableCollection<AutoFillProfile> Profiles { get; } = new();
        public bool IsEnabled { get; set; } = false;

        public AutoFillService()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GhostBrowser");
            Directory.CreateDirectory(appData);
            _autofillFile = Path.Combine(appData, "autofill.json");
            LoadProfiles();
        }

        /// <summary>
        /// Возвращает активный профиль (первый с IsActive == true).
        /// </summary>
        public AutoFillProfile? GetActiveProfile()
        {
            return Profiles.FirstOrDefault(p => p.IsActive);
        }

        /// <summary>
        /// Генерирует JavaScript для заполнения форм на странице.
        /// Использует name, id, autocomplete-атрибуты и placeholder для маппинга полей.
        /// </summary>
        public string GenerateFillScript()
        {
            var profile = GetActiveProfile();
            if (profile == null)
                return "console.log('[AutoFill] No active profile');";

            // Маппинг: имя JS-переменной → массив CSS-селекторов/имён полей
            var fieldMappings = new Dictionary<string, string[]>
            {
                { "firstName", new[] { "firstName", "fname", "given-name" } },
                { "lastName", new[] { "lastName", "lname", "family-name" } },
                { "middleName", new[] { "middleName", "mname", "additional-name" } },
                { "email", new[] { "email" } },
                { "phone", new[] { "phone", "tel" } },
                { "street", new[] { "street", "addressLine1", "address-line1" } },
                { "city", new[] { "city", "locality" } },
                { "state", new[] { "state", "region" } },
                { "zipCode", new[] { "zip", "postalCode", "zipCode", "postal-code" } },
                { "country", new[] { "country", "countryName" } },
            };

            var scriptParts = new List<string>();
            scriptParts.Add("(function() {");
            scriptParts.Add("var filled = 0;");
            scriptParts.Add("var profile = " + JsonSerializer.Serialize(profile) + ";");

            foreach (var (propName, selectors) in fieldMappings)
            {
                // Для каждого поля генерируем JS, который ищет input по name/id/autocomplete/placeholder
                scriptParts.Add($"var val = profile.{propName} || '';");
                scriptParts.Add($"if (val) {{");
                foreach (var sel in selectors)
                {
                    // Поиск по name
                    scriptParts.Add($"var el = document.querySelector('input[name=\"{sel}\"], input[id*=\"{sel}\"], input[autocomplete=\"{sel}\"]');");
                    scriptParts.Add("if (el && !el.value) { el.value = val; el.dispatchEvent(new Event('input', {bubbles:true})); filled++; }");
                }
                scriptParts.Add("}");
            }

            scriptParts.Add("console.log('[AutoFill] Filled ' + filled + ' fields');");
            scriptParts.Add("})();");

            return string.Join("\n", scriptParts);
        }

        public void AddProfile(AutoFillProfile profile)
        {
            Profiles.Add(profile);
            SaveProfiles();
        }

        public void RemoveProfile(Guid id)
        {
            var profile = Profiles.FirstOrDefault(p => p.Id == id);
            if (profile != null)
            {
                Profiles.Remove(profile);
                SaveProfiles();
            }
        }

        public void SetActiveProfile(Guid id)
        {
            foreach (var p in Profiles)
                p.IsActive = (p.Id == id);
            SaveProfiles();
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(_autofillFile))
                {
                    var json = File.ReadAllText(_autofillFile);
                    var profiles = JsonSerializer.Deserialize<List<AutoFillProfile>>(json);
                    if (profiles != null)
                    {
                        foreach (var profile in profiles)
                            Profiles.Add(profile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoFill load error: {ex.Message}");
            }
        }

        public void SaveProfiles()
        {
            try
            {
                var json = JsonSerializer.Serialize(Profiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_autofillFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoFill save error: {ex.Message}");
            }
        }
    }
}
