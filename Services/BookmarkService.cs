using System;
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
