using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using GhostBrowser.Models;

namespace GhostBrowser.Services
{
    public class HistoryService
    {
        private readonly string _historyFile;
        public ObservableCollection<HistoryEntry> History { get; } = new();

        public HistoryService()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser");
            Directory.CreateDirectory(appData);
            _historyFile = Path.Combine(appData, "history.json");
            LoadHistory();
        }

        public void AddEntry(string title, string url)
        {
            if (url.StartsWith("ghost://")) return;

            var entry = new HistoryEntry
            {
                Title = title,
                Url = url,
                VisitedAt = DateTime.UtcNow
            };

            History.Insert(0, entry);

            // Limit to 1000 entries
            while (History.Count > 1000)
            {
                History.RemoveAt(History.Count - 1);
            }

            SaveHistory();
        }

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFile))
                {
                    var json = File.ReadAllText(_historyFile);
                    var entries = JsonSerializer.Deserialize<HistoryEntry[]>(json);
                    if (entries != null)
                    {
                        // Дедупликация: убираем записи с одинаковым URL (оставляем первую)
                        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var entry in entries)
                        {
                            if (seen.Add(entry.Url))
                            {
                                History.Add(entry);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем работу приложения
                System.Diagnostics.Debug.WriteLine($"History load error: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            try
            {
                var json = JsonSerializer.Serialize(History, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"History save error: {ex.Message}");
            }
        }

        public void ClearHistory()
        {
            History.Clear();
            SaveHistory();
        }
    }
}
