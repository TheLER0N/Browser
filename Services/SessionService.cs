using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GhostBrowser.Models;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис управления сессиями браузера.
    /// Сохраняет и восстанавливает наборы открытых вкладок.
    /// Хранение: JSON файлы в %APPDATA%\GhostBrowser\sessions\
    /// </summary>
    public class SessionService
    {
        private readonly string _sessionsFolder;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Коллекция сохранённых сессий для биндинга к UI.
        /// Отсортирована по дате создания (новые первые).
        /// </summary>
        public ObservableCollection<Session> Sessions { get; } = new();

        public SessionService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            _sessionsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GhostBrowser", "sessions");

            Directory.CreateDirectory(_sessionsFolder);
            LoadSessions();
        }

        /// <summary>
        /// Сохраняет текущую сессию (список URL вкладок).
        /// </summary>
        /// <param name="urls">Список URL открытых вкладок.</param>
        /// <param name="name">Название сессии (авто если пустое).</param>
        /// <returns>Созданная сессия.</returns>
        public Session SaveSession(IEnumerable<string> urls, string name = "")
        {
            var session = new Session
            {
                Name = string.IsNullOrEmpty(name)
                    ? $"Сессия — {DateTime.Now:dd.MM.yyyy HH:mm}"
                    : name,
                CreatedAt = DateTime.Now,
                Urls = urls.Where(u => !string.IsNullOrEmpty(u) && !u.StartsWith("ghost://")).ToList()
            };

            if (session.Urls.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[SessionService] No URLs to save");
                return session;
            }

            // Сохраняем в отдельный JSON файл
            var filePath = GetSessionFilePath(session.Id);
            var json = JsonSerializer.Serialize(session, _jsonOptions);
            File.WriteAllText(filePath, json);

            // Добавляем в коллекцию
            Sessions.Insert(0, session);

            System.Diagnostics.Debug.WriteLine($"[SessionService] Saved session '{session.Name}' with {session.TabCount} tabs");
            return session;
        }

        /// <summary>
        /// Восстанавливает сессию по ID.
        /// </summary>
        /// <param name="sessionId">ID сессии.</param>
        /// <returns>Список URL или null если сессия не найдена.</returns>
        public List<string>? RestoreSession(string sessionId)
        {
            var session = Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null)
            {
                // Пробуем загрузить из файла
                var filePath = GetSessionFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        session = JsonSerializer.Deserialize<Session>(json, _jsonOptions);
                        if (session != null)
                        {
                            Sessions.Add(session);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SessionService] Restore error: {ex.Message}");
                        return null;
                    }
                }
            }

            if (session == null || session.Urls.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionService] Session '{sessionId}' not found or empty");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[SessionService] Restored session '{session.Name}' with {session.TabCount} tabs");
            return session.Urls;
        }

        /// <summary>
        /// Удаляет сессию по ID.
        /// </summary>
        public bool DeleteSession(string sessionId)
        {
            var session = Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session == null) return false;

            try
            {
                var filePath = GetSessionFilePath(sessionId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                Sessions.Remove(session);
                System.Diagnostics.Debug.WriteLine($"[SessionService] Deleted session '{session.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionService] Delete error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получает последнюю сохранённую сессию (по дате).
        /// </summary>
        public Session? GetLastSession()
        {
            return Sessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
        }

        /// <summary>
        /// Загружает все сохранённые сессии из файлов.
        /// </summary>
        private void LoadSessions()
        {
            try
            {
                var files = Directory.GetFiles(_sessionsFolder, "*.json");
                var sessions = new List<Session>();

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var session = JsonSerializer.Deserialize<Session>(json, _jsonOptions);
                        if (session != null && session.Urls.Count > 0)
                        {
                            sessions.Add(session);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SessionService] Failed to load {file}: {ex.Message}");
                    }
                }

                Sessions.Clear();
                foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
                {
                    Sessions.Add(session);
                }

                System.Diagnostics.Debug.WriteLine($"[SessionService] Loaded {Sessions.Count} sessions");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionService] LoadSessions error: {ex.Message}");
            }
        }

        /// <summary>
        /// Возвращает путь к файлу сессии.
        /// </summary>
        private string GetSessionFilePath(string sessionId)
        {
            return Path.Combine(_sessionsFolder, $"session_{sessionId}.json");
        }

        /// <summary>
        /// Очищает ВСЕ сохранённые сессии.
        /// </summary>
        public void ClearAllSessions()
        {
            try
            {
                foreach (var file in Directory.GetFiles(_sessionsFolder, "*.json"))
                {
                    File.Delete(file);
                }

                Sessions.Clear();
                System.Diagnostics.Debug.WriteLine("[SessionService] All sessions cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionService] ClearAllSessions error: {ex.Message}");
            }
        }
    }
}
