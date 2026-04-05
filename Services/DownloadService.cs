using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GhostBrowser.Models;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис управления загрузками файлов.
    /// 
    /// Отвечает за:
    /// - Запуск, приостановку, возобновление и отмену загрузок
    /// - Расчёт скорости загрузки (байт/сек, 1-секундный интервал)
    /// - Сохранение/загрузку истории загрузок (downloads.json)
    /// - Управление папкой загрузок по умолчанию
    /// 
    /// Для pause/resume используется HTTP Range header.
    /// При паузе: CancellationTokenSource отменяется, FileStream закрывается.
    /// При возобновлении: создаётся новый запрос с Range: bytes={receivedBytes}-.
    /// </summary>
    public class DownloadService : INotifyPropertyChanged
    {
        /// <summary>Период обновления скорости и сохранения (мс).</summary>
        private const int SpeedCalcIntervalMs = 1000;

        /// <summary>Размер буфера для чтения из HTTP stream (8 КБ).</summary>
        private const int BufferSize = 8192;

        private readonly HttpClient _httpClient;
        private readonly string _downloadsFile;
        private readonly DispatcherTimer _speedTimer;
        private readonly object _lock = new();

        /// <summary>Словарь активных загрузок: DownloadItem.Id -> CancellationTokenSource.</summary>
        private readonly Dictionary<Guid, CancellationTokenSource> _activeTokens = new();

        /// <summary>Словарь предыдущих значений receivedBytes для расчёта скорости.</summary>
        private readonly Dictionary<Guid, long> _previousBytes = new();

        /// <summary>Словарь FileStream'ов для активных записей на диск.</summary>
        private readonly Dictionary<Guid, FileStream> _activeStreams = new();

        public DownloadService()
        {
            _httpClient = new HttpClient();

            // Файл downloads.json хранится в %APPDATA%\GhostBrowser\
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser");
            Directory.CreateDirectory(appData);
            _downloadsFile = Path.Combine(appData, "downloads.json");

            // Загружаем историю загрузок из файла
            LoadDownloads();

            // Таймер скорости — обновляется каждую секунду
            _speedTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SpeedCalcIntervalMs)
            };
            _speedTimer.Tick += SpeedTimer_Tick;
            _speedTimer.Start();
        }

        // ==================== Collections ====================

        /// <summary>Активные загрузки (Downloading, Paused).</summary>
        public ObservableCollection<DownloadItem> ActiveDownloads { get; } = new();

        /// <summary>Завершённые загрузки (Completed, Cancelled, Failed).</summary>
        public ObservableCollection<DownloadItem> CompletedDownloads { get; } = new();

        // ==================== Download Folder ====================

        /// <summary>
        /// Папка загрузок по умолчанию.
        /// По умолчанию: %USERPROFILE%\Downloads
        /// </summary>
        public string DownloadFolder
        {
            get
            {
                // Читаем из SettingsService если доступен, иначе дефолт
                return _downloadFolder ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            set
            {
                if (_downloadFolder != value)
                {
                    _downloadFolder = value;
                    OnPropertyChanged();
                    // Сохраняем в настройки если SettingsService доступен
                    SaveDownloadFolder(value);
                }
            }
        }
        private string? _downloadFolder;

        /// <summary>Загружает сохранённую папку загрузок из настроек.</summary>
        public void LoadDownloadFolder(SettingsService settingsService)
        {
            _downloadFolder = settingsService.DownloadFolder;
            OnPropertyChanged(nameof(DownloadFolder));
        }

        /// <summary>Сохраняет папку загрузок в SettingsService.</summary>
        public void SaveDownloadFolder(string path, SettingsService? settingsService = null)
        {
            _downloadFolder = path;
            OnPropertyChanged(nameof(DownloadFolder));

            // Сохраняем напрямую в settings.json через SettingsService
            if (settingsService != null)
            {
                settingsService.DownloadFolder = path;
            }
            else
            {
                // Fallback: сохраняем в отдельный файл
                var prefsFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GhostBrowser", "download_prefs.json");
                try
                {
                    var json = JsonSerializer.Serialize(new { DownloadFolder = path });
                    File.WriteAllText(prefsFile, json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SaveDownloadFolder error: {ex.Message}");
                }
            }
        }

        // ==================== Download Start ====================

        /// <summary>
        /// Запускает загрузку файла по URL.
        /// Вызывается из TabViewModel при событии DownloadStarting.
        /// </summary>
        /// <param name="url">URL файла для загрузки.</param>
        /// <param name="suggestedFilename">Предлагаемое имя файла из WebView2 (может быть null).</param>
        public async void StartDownload(string url, string? suggestedFilename = null)
        {
            try
            {
                // Определяем имя файла: из suggestedFilename или извлекаем из URL
                var fileName = CleanFileName(suggestedFilename ?? ExtractFileNameFromUrl(url));
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                // Создаём папку загрузок если не существует
                Directory.CreateDirectory(DownloadFolder);

                // Формируем полный путь
                var filePath = Path.Combine(DownloadFolder, fileName);

                // Если файл уже существует, добавляем суффикс
                filePath = GetUniqueFilePath(filePath);

                // Создаём элемент загрузки
                var item = new DownloadItem
                {
                    Url = url,
                    FileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    Status = DownloadItemStatus.Downloading,
                    StartedAt = DateTime.Now
                };

                // Регистрируем callbacks для Pause/Resume/Cancel
                item.OnPauseRequested = () => CancelTokenForItem(item.Id);
                item.OnResumeRequested = () => _ = ResumeDownloadAsync(item);
                item.OnCancelRequested = () => CancelTokenForItem(item.Id);

                // Добавляем в коллекцию активных загрузок (UI thread)
                Application.Current.Dispatcher.Invoke(() => ActiveDownloads.Add(item));

                // Запускаем асинхронную загрузку
                _ = ExecuteDownloadAsync(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartDownload error: {ex.Message}");
            }
        }

        // ==================== Internal Download Execution ====================

        /// <summary>
        /// Выполняет загрузку файла с поддержкой Range (для resume).
        /// 
        /// Алгоритм:
        /// 1. Если receivedBytes > 0 (resume), добавляем Range header
        /// 2. Открываем FileStream (Append если resume)
        /// 3. Читаем response stream блоками по 8KB
        /// 4. Обновляем ReceivedBytes и Speed каждую секунду через таймер
        /// 5. При отмене (CancellationToken) — выходим из цикла
        /// </summary>
        private async Task ExecuteDownloadAsync(DownloadItem item, long startByte = 0)
        {
            CancellationTokenSource cts = new();

            lock (_lock)
            {
                _activeTokens[item.Id] = cts;
            }

            FileStream? fs = null;
            try
            {
                // Создаём HTTP запрос с поддержкой Range
                using var request = new HttpRequestMessage(HttpMethod.Get, item.Url);

                // Если это resume — добавляем Range header
                if (startByte > 0)
                {
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(
                        startByte, null); // bytes=startByte-
                }

                // Отправляем запрос
                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cts.Token);

                // Проверяем статус ответа
                if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable && startByte > 0)
                {
                    // Сервер не поддерживает Range — пробуем загрузить заново
                    Debug.WriteLine($"Server doesn't support Range, restarting download: {item.FileName}");
                    item.ReceivedBytes = 0;
                    lock (_lock) _activeTokens.Remove(item.Id);
                    await ExecuteDownloadAsync(item, 0);
                    return;
                }

                response.EnsureSuccessStatusCode();

                // Определяем общий размер файла
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue)
                {
                    // Если resume — contentLength это оставшийся размер
                    item.TotalBytes = startByte + contentLength.Value;
                }

                // Открываем FileStream для записи
                // Если resume — открываем в режиме Append
                var mode = startByte > 0 ? FileMode.Append : FileMode.Create;
                fs = new FileStream(item.FilePath, mode, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

                lock (_lock)
                {
                    _activeStreams[item.Id] = fs;
                    _previousBytes[item.Id] = item.ReceivedBytes;
                }

                // Читаем данные блоками
                using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                var buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                {
                    // Проверяем не была ли загрузка отменена
                    if (cts.Token.IsCancellationRequested)
                        break;

                    // Проверяем не была ли загрузка приостановлена
                    if (item.Status == DownloadItemStatus.Paused)
                        break;

                    // Записываем блок на диск
                    await fs.WriteAsync(buffer, 0, bytesRead, cts.Token);

                    // Обновляем счётчик полученных байт
                    item.ReceivedBytes += bytesRead;
                }

                // Если загрузка не была отменена или приостановлена — она завершена
                if (item.Status == DownloadItemStatus.Downloading && !cts.Token.IsCancellationRequested)
                {
                    item.Status = DownloadItemStatus.Completed;
                    item.Progress = 100;
                    item.CompletedAt = DateTime.Now;
                    item.Speed = 0;

                    // Перемещаем из ActiveDownloads в CompletedDownloads
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActiveDownloads.Remove(item);
                        CompletedDownloads.Insert(0, item);
                    });

                    // Сохраняем историю
                    SaveDownloads();
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная ситуация при Pause/Cancel — не логируем как ошибку
                Debug.WriteLine($"Download cancelled: {item.FileName}");
            }
            catch (Exception ex)
            {
                // Сетевая ошибка, таймаут и т.д.
                Debug.WriteLine($"Download error for {item.FileName}: {ex.Message}");

                // Если загрузка не была явно отменена — помечаем как Failed
                if (item.Status == DownloadItemStatus.Downloading)
                {
                    item.Status = DownloadItemStatus.Failed;
                    item.ErrorMessage = ex.Message;
                    item.Speed = 0;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ActiveDownloads.Remove(item);
                        CompletedDownloads.Insert(0, item);
                    });

                    SaveDownloads();
                }
            }
            finally
            {
                // Освобождаем ресурсы
                lock (_lock)
                {
                    _activeTokens.Remove(item.Id);
                    _activeStreams.Remove(item.Id);
                }

                fs?.Dispose();
            }
        }

        /// <summary>
        /// Возобновляет загрузку с места остановки.
        /// Отправляет HTTP запрос с Range header: bytes={receivedBytes}-
        /// </summary>
        private async Task ResumeDownloadAsync(DownloadItem item)
        {
            try
            {
                Debug.WriteLine($"Resuming download: {item.FileName} from byte {item.ReceivedBytes}");
                await ExecuteDownloadAsync(item, item.ReceivedBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResumeDownload error: {ex.Message}");
                item.Status = DownloadItemStatus.Failed;
                item.ErrorMessage = ex.Message;
            }
        }

        // ==================== Speed Calculation ====================

        /// <summary>
        /// Таймер расчёта скорости.
        /// Каждую секунду вычисляет разницу между текущим и предыдущим значением ReceivedBytes.
        /// </summary>
        private void SpeedTimer_Tick(object? sender, EventArgs e)
        {
            lock (_lock)
            {
                foreach (var item in ActiveDownloads)
                {
                    if (item.Status != DownloadItemStatus.Downloading)
                    {
                        item.Speed = 0;
                        continue;
                    }

                    // Рассчитываем скорость: (текущие байты - предыдущие байты) / 1 секунда
                    if (_previousBytes.TryGetValue(item.Id, out var prevBytes))
                    {
                        item.Speed = Math.Max(0, item.ReceivedBytes - prevBytes);
                    }

                    // Сохраняем текущее значение как предыдущее для следующего тика
                    _previousBytes[item.Id] = item.ReceivedBytes;
                }
            }

            // Сохраняем историю каждые 10 секунд (каждый 10-й тик)
            _saveCounter++;
            if (_saveCounter >= 10)
            {
                _saveCounter = 0;
                SaveDownloads();
            }
        }
        private int _saveCounter;

        // ==================== Token Management ====================

        /// <summary>Отменяет CancellationToken для указанной загрузки.</summary>
        private void CancelTokenForItem(Guid id)
        {
            lock (_lock)
            {
                if (_activeTokens.TryGetValue(id, out var cts))
                {
                    cts.Cancel();
                }

                // Закрываем FileStream
                if (_activeStreams.TryGetValue(id, out var fs))
                {
                    try { fs.Dispose(); } catch { /* игнорируем */ }
                    _activeStreams.Remove(id);
                }
            }
        }

        // ==================== Persistence ====================

        /// <summary>
        /// Сохраняет историю загрузок в downloads.json.
        /// Сохраняются только Completed, Cancelled, Failed загрузки.
        /// Активные загрузки не сохраняются (при перезапуске они потеряются).
        /// </summary>
        public void SaveDownloads()
        {
            try
            {
                // Собираем завершённые загрузки для сохранения
                var itemsToSave = new List<DownloadDto>();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in CompletedDownloads)
                    {
                        itemsToSave.Add(new DownloadDto
                        {
                            Id = item.Id,
                            Url = item.Url,
                            FileName = item.FileName,
                            FilePath = item.FilePath,
                            TotalBytes = item.TotalBytes,
                            ReceivedBytes = item.ReceivedBytes,
                            Status = item.Status,
                            StartedAt = item.StartedAt,
                            CompletedAt = item.CompletedAt
                        });
                    }
                });

                var json = JsonSerializer.Serialize(itemsToSave, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.WriteAllText(_downloadsFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveDownloads error: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает историю загрузок из downloads.json.
        /// Все загрузки восстанавливаются в CompletedDownloads (неактивные).
        /// </summary>
        private void LoadDownloads()
        {
            try
            {
                if (!File.Exists(_downloadsFile))
                    return;

                var json = File.ReadAllText(_downloadsFile);
                var items = JsonSerializer.Deserialize<List<DownloadDto>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (items == null) return;

                foreach (var dto in items)
                {
                    var item = new DownloadItem
                    {
                        Id = dto.Id,
                        Url = dto.Url,
                        FileName = dto.FileName,
                        FilePath = dto.FilePath,
                        TotalBytes = dto.TotalBytes,
                        ReceivedBytes = dto.ReceivedBytes,
                        Status = dto.Status,
                        StartedAt = dto.StartedAt,
                        CompletedAt = dto.CompletedAt
                    };

                    // Вычисляем прогресс из загруженных данных
                    if (item.TotalBytes > 0)
                        item.Progress = Math.Min(100.0, (double)item.ReceivedBytes / item.TotalBytes * 100.0);

                    Application.Current.Dispatcher.Invoke(() =>
                        CompletedDownloads.Add(item));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadDownloads error: {ex.Message}");
            }
        }

        // ==================== Public Management Methods ====================

        /// <summary>Удаляет завершённую загрузку из списка и удаляет файл с диска.</summary>
        public void DeleteDownload(DownloadItem item)
        {
            try
            {
                // Удаляем файл если он существует
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                }

                // Удаляем из коллекций
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveDownloads.Remove(item);
                    CompletedDownloads.Remove(item);
                });

                SaveDownloads();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteDownload error: {ex.Message}");
            }
        }

        /// <summary>Удаляет только запись из истории (файл остаётся на диске).</summary>
        public void RemoveFromHistory(DownloadItem item)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CompletedDownloads.Remove(item);
            });
            SaveDownloads();
        }

        /// <summary>Очищает все завершённые загрузки из списка.</summary>
        public void ClearCompleted()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toRemove = new List<DownloadItem>(CompletedDownloads);
                foreach (var item in toRemove)
                {
                    CompletedDownloads.Remove(item);
                }
            });
            SaveDownloads();
        }

        /// <summary>Открывает папку содержащую файл в проводнике.</summary>
        public void OpenFileLocation(DownloadItem item)
        {
            try
            {
                if (File.Exists(item.FilePath))
                {
                    // Открываем проводник с выделенным файлом
                    Process.Start("explorer.exe", $"/select,\"{item.FilePath}\"");
                }
                else if (Directory.Exists(DownloadFolder))
                {
                    // Если файл не найден, открываем папку загрузок
                    Process.Start("explorer.exe", $"\"{DownloadFolder}\"");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenFileLocation error: {ex.Message}");
            }
        }

        /// <summary>Открывает папку загрузок в проводнике.</summary>
        public void OpenDownloadFolder()
        {
            try
            {
                Directory.CreateDirectory(DownloadFolder);
                Process.Start("explorer.exe", $"\"{DownloadFolder}\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenDownloadFolder error: {ex.Message}");
            }
        }

        /// <summary>Открывает файл программой по умолчанию.</summary>
        public void OpenFile(DownloadItem item)
        {
            try
            {
                if (File.Exists(item.FilePath))
                {
                    Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenFile error: {ex.Message}");
            }
        }

        // ==================== Helpers ====================

        /// <summary>Извлекает имя файла из URL (последний сегмент пути).</summary>
        private static string ExtractFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var fileName = Path.GetFileName(path);
                return string.IsNullOrEmpty(fileName) ? "" : fileName;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Очищает имя файла от недопустимых символов.
        /// Windows запрещает: < > : " / \ | ? *
        /// </summary>
        private static string CleanFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return cleaned.Trim();
        }

        /// <summary>
        /// Генерирует уникальное имя файла, добавляя суффикс (1), (2) и т.д.
        /// если файл уже существует.
        /// </summary>
        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path)) return path;

            var dir = Path.GetDirectoryName(path) ?? "";
            var nameWithoutExt = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            int counter = 1;
            string newPath;
            do
            {
                newPath = Path.Combine(dir, $"{nameWithoutExt} ({counter}){ext}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        // ==================== DTO для сериализации ====================

        /// <summary>
        /// DTO (Data Transfer Object) для сериализации DownloadItem в JSON.
        /// Содержит только необходимые поля без transient-состояний.
        /// </summary>
        private class DownloadDto
        {
            public Guid Id { get; set; }
            public string Url { get; set; } = "";
            public string FileName { get; set; } = "";
            public string FilePath { get; set; } = "";
            public long TotalBytes { get; set; }
            public long ReceivedBytes { get; set; }
            public DownloadItemStatus Status { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        // ==================== INotifyPropertyChanged ====================

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>Освобождает ресурсы (таймер, HttpClient).</summary>
        public void Dispose()
        {
            _speedTimer.Stop();
            _httpClient?.Dispose();

            // Отменяем все активные загрузки
            lock (_lock)
            {
                foreach (var cts in _activeTokens.Values)
                {
                    try { cts.Cancel(); } catch { /* игнорируем */ }
                    cts.Dispose();
                }
                _activeTokens.Clear();

                foreach (var fs in _activeStreams.Values)
                {
                    try { fs.Dispose(); } catch { /* игнорируем */ }
                }
                _activeStreams.Clear();
            }
        }
    }
}
