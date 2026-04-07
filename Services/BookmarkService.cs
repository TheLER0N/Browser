using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using GhostBrowser.Models;

namespace GhostBrowser.Services
{
    public class BookmarkService
    {
        private readonly string _bookmarksFile;
        public ObservableCollection<Bookmark> Bookmarks { get; } = new();

        public BookmarkService()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser");
            Directory.CreateDirectory(appData);
            _bookmarksFile = Path.Combine(appData, "bookmarks.json");
            LoadBookmarks();
        }

        public void AddBookmark(string title, string url)
        {
            // Check if already exists — case-insensitive URL comparison
            if (Bookmarks.Any(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase))) return;

            var bookmark = new Bookmark
            {
                Title = title,
                Url = url,
                CreatedAt = DateTime.UtcNow
            };

            Bookmarks.Add(bookmark);
            SaveBookmarks();
        }

        public void RemoveBookmark(Guid id)
        {
            var bookmark = Bookmarks.FirstOrDefault(b => b.Id == id);
            if (bookmark != null)
            {
                Bookmarks.Remove(bookmark);
                SaveBookmarks();
            }
        }

        public bool IsBookmarked(string url)
        {
            return Bookmarks.Any(b => b.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
        }

        public void ClearBookmarks()
        {
            Bookmarks.Clear();
            SaveBookmarks();
        }

        /// <summary>
        /// Экспорт закладок в указанный файл (копия bookmarks.json).
        /// </summary>
        public bool ExportBookmarks(string destinationPath)
        {
            try
            {
                var json = JsonSerializer.Serialize(Bookmarks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(destinationPath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bookmark export error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Импорт закладок из файла с умным слиянием (merge).
        /// Дедупликация по URL (case-insensitive). Возвращает SyncResult со статистикой.
        /// </summary>
        public SyncResult ImportAndMergeBookmarks(string sourcePath)
        {
            var result = new SyncResult();

            try
            {
                var json = File.ReadAllText(sourcePath);
                var importedBookmarks = JsonSerializer.Deserialize<List<Bookmark>>(json);

                if (importedBookmarks == null || importedBookmarks.Count == 0)
                {
                    result.ErrorMessage = "Файл не содержит закладок";
                    return result;
                }

                result.TotalImported = importedBookmarks.Count;

                // Коллекция существующих URL для быстрой проверки (case-insensitive)
                var existingUrls = new HashSet<string>(
                    Bookmarks.Select(b => b.Url.ToLowerInvariant())
                );

                foreach (var imported in importedBookmarks)
                {
                    // Пропускаем закладки с пустыми полями
                    if (string.IsNullOrWhiteSpace(imported.Url))
                    {
                        result.Errors++;
                        continue;
                    }

                    // Дедупликация по URL (case-insensitive)
                    if (existingUrls.Contains(imported.Url.ToLowerInvariant()))
                    {
                        result.Skipped++;
                        continue;
                    }

                    // Добавляем новую закладку
                    var bookmark = new Bookmark
                    {
                        Id = imported.Id != Guid.Empty ? imported.Id : Guid.NewGuid(),
                        Title = imported.Title ?? "",
                        Url = imported.Url,
                        Favicon = imported.Favicon ?? "",
                        CreatedAt = imported.CreatedAt != default ? imported.CreatedAt : DateTime.UtcNow
                    };

                    Bookmarks.Add(bookmark);
                    existingUrls.Add(imported.Url.ToLowerInvariant());
                    result.Added++;
                }

                // Сохраняем обновлённый список
                if (result.Added > 0)
                {
                    SaveBookmarks();
                }
            }
            catch (JsonException ex)
            {
                result.ErrorMessage = $"Невалидный JSON: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Bookmark import JSON error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Ошибка импорта: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Bookmark import error: {ex.Message}");
            }

            return result;
        }

        private void LoadBookmarks()
        {
            try
            {
                if (File.Exists(_bookmarksFile))
                {
                    var json = File.ReadAllText(_bookmarksFile);
                    var bookmarks = JsonSerializer.Deserialize<Bookmark[]>(json);
                    if (bookmarks != null)
                    {
                        foreach (var bookmark in bookmarks)
                        {
                            Bookmarks.Add(bookmark);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bookmark load error: {ex.Message}");
            }
        }

        private void SaveBookmarks()
        {
            try
            {
                var json = JsonSerializer.Serialize(Bookmarks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_bookmarksFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bookmark save error: {ex.Message}");
            }
        }
    }
}
