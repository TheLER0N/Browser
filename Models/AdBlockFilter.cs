using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GhostBrowser.Models
{
    /// <summary>
    /// Отдельное правило фильтрации (одна строка из EasyList).
    /// Поддерживает простые паттерны и regex-правила.
    /// </summary>
    public class AdBlockRule
    {
        public string Pattern { get; set; } = "";
        public bool IsRegex { get; set; }
        public AdBlockResourceType ResourceType { get; set; } = AdBlockResourceType.All;

        [NonSerialized]
        private Regex? _compiledRegex;

        /// <summary>
        /// Проверяет, совпадает ли URL с этим правилом.
        /// </summary>
        public bool Matches(string url)
        {
            if (IsRegex)
            {
                try
                {
                    _compiledRegex ??= new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    return _compiledRegex.IsMatch(url);
                }
                catch
                {
                    return false;
                }
            }

            // Простое contains-совпадение (для /ads/, /banner/, doubleclick.net и т.д.)
            return url.IndexOf(Pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    /// <summary>
    /// Тип ресурса для фильтрации.
    /// </summary>
    public enum AdBlockResourceType
    {
        All,
        Script,
        Image,
        Stylesheet,
        XMLHttpRequest,
        Font,
        Media,
        WebSocket
    }

    /// <summary>
    /// Список фильтров (например EasyList).
    /// </summary>
    public class AdBlockFilterList
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public string SourceUrl { get; set; } = "";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public List<AdBlockRule> Rules { get; set; } = new();
        public int BlockedCount { get; set; } = 0;

        /// <summary>
        /// Проверяет URL по всем правилам списка.
        /// </summary>
        public bool ShouldBlock(string url)
        {
            if (!IsEnabled) return false;
            foreach (var rule in Rules)
            {
                if (rule.Matches(url))
                {
                    BlockedCount++;
                    return true;
                }
            }
            return false;
        }
    }
}
