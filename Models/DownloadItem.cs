using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using GhostBrowser.Models;

namespace GhostBrowser.Models
{
    /// <summary>
    /// Модель элемента загрузки.
    /// Реализует INotifyPropertyChanged для привязки данных в WPF.
    /// Отслеживает прогресс, скорость, статус и предоставляет методы управления (Pause/Resume/Cancel).
    /// </summary>
    public class DownloadItem : INotifyPropertyChanged
    {
        private DownloadItemStatus _status;
        private long _receivedBytes;
        private long _totalBytes;
        private double _progress;
        private double _speed; // bytes per second
        private string _fileName = "";
        private string _filePath = "";
        private string _url = "";
        private string _errorMessage = "";

        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        /// <summary>URL источника файла.</summary>
        public string Url
        {
            get => _url;
            set => Set(ref _url, value);
        }

        /// <summary>Имя файла (без пути).</summary>
        public string FileName
        {
            get => _fileName;
            set => Set(ref _fileName, value);
        }

        /// <summary>Полный путь к сохранённому файлу.</summary>
        public string FilePath
        {
            get => _filePath;
            set => Set(ref _filePath, value);
        }

        /// <summary>Общий размер файла в байтах (0 если неизвестен).</summary>
        public long TotalBytes
        {
            get => _totalBytes;
            set => Set(ref _totalBytes, value);
        }

        /// <summary>Количество полученных байт.</summary>
        public long ReceivedBytes
        {
            get => _receivedBytes;
            set
            {
                if (Set(ref _receivedBytes, value))
                {
                    // Обновляем прогресс при изменении received bytes
                    UpdateProgress();
                }
            }
        }

        /// <summary>Скорость загрузки в байтах/сек.</summary>
        public double Speed
        {
            get => _speed;
            set => Set(ref _speed, value);
        }

        /// <summary>Прогресс загрузки в процентах (0-100).</summary>
        public double Progress
        {
            get => _progress;
            set => Set(ref _progress, value);
        }

        /// <summary>Текущий статус загрузки.</summary>
        public DownloadItemStatus Status
        {
            get => _status;
            set
            {
                if (Set(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(IsPaused));
                    OnPropertyChanged(nameof(IsDownloading));
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(IsFailed));
                    OnPropertyChanged(nameof(IsCancellable));
                }
            }
        }

        /// <summary>Сообщение об ошибке (для Failed статуса).</summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        // ==================== Computed Properties ====================

        /// <summary>Форматированная скорость (например "1.5 МБ/с").</summary>
        public string SpeedFormatted => FormatSpeed(Speed);

        /// <summary>Форматированный общий размер файла.</summary>
        public string TotalSizeFormatted => FormatSize(TotalBytes);

        /// <summary>Форматированный полученный размер.</summary>
        public string ReceivedSizeFormatted => FormatSize(ReceivedBytes);

        /// <summary>Текстовое представление статуса для UI.</summary>
        public string StatusText => Status switch
        {
            DownloadItemStatus.Downloading => "Загрузка...",
            DownloadItemStatus.Paused => "Приостановлено",
            DownloadItemStatus.Completed => "Завершено",
            DownloadItemStatus.Cancelled => "Отменено",
            DownloadItemStatus.Failed => $"Ошибка: {ErrorMessage}",
            _ => ""
        };

        /// <summary>Иконка статуса для UI.</summary>
        public string StatusIcon => Status switch
        {
            DownloadItemStatus.Downloading => "⬇",
            DownloadItemStatus.Paused => "⏸",
            DownloadItemStatus.Completed => "✅",
            DownloadItemStatus.Cancelled => "❌",
            DownloadItemStatus.Failed => "⚠",
            _ => "❓"
        };

        public bool IsPaused => Status == DownloadItemStatus.Paused;
        public bool IsDownloading => Status == DownloadItemStatus.Downloading;
        public bool IsCompleted => Status == DownloadItemStatus.Completed;
        public bool IsFailed => Status == DownloadItemStatus.Failed;
        public bool IsCancellable => Status == DownloadItemStatus.Downloading || Status == DownloadItemStatus.Paused;

        // ==================== Internal Callbacks ====================

        /// <summary>
        /// Действие приостановки. Устанавливается из DownloadService.
        /// Вызывается при вызове Pause().
        /// </summary>
        internal Action? OnPauseRequested { get; set; }

        /// <summary>
        /// Действие возобновления. Устанавливается из DownloadService.
        /// Вызывается при вызании Resume().
        /// </summary>
        internal Action? OnResumeRequested { get; set; }

        /// <summary>
        /// Действие отмены. Устанавливается из DownloadService.
        /// Вызывается при вызове Cancel().
        /// </summary>
        internal Action? OnCancelRequested { get; set; }

        // ==================== Public Methods ====================

        /// <summary>Приостановить загрузку.</summary>
        public void Pause()
        {
            if (Status != DownloadItemStatus.Downloading) return;
            Status = DownloadItemStatus.Paused;
            Speed = 0;
            OnPauseRequested?.Invoke();
        }

        /// <summary>Возобновить загрузку.</summary>
        public void Resume()
        {
            if (Status != DownloadItemStatus.Paused) return;
            Status = DownloadItemStatus.Downloading;
            OnResumeRequested?.Invoke();
        }

        /// <summary>Отменить загрузку.</summary>
        public void Cancel()
        {
            if (!IsCancellable) return;
            Status = DownloadItemStatus.Cancelled;
            Speed = 0;
            CompletedAt = DateTime.Now;
            OnCancelRequested?.Invoke();
        }

        // ==================== Helpers ====================

        /// <summary>Обновляет прогресс в процентах на основе received/total.</summary>
        private void UpdateProgress()
        {
            if (TotalBytes > 0)
                Progress = Math.Min(100.0, (double)ReceivedBytes / TotalBytes * 100.0);
            else
                Progress = 0;

            OnPropertyChanged(nameof(ReceivedSizeFormatted));
        }

        /// <summary>Форматирует размер в байтах в читаемый вид (Б, КБ, МБ, ГБ).</summary>
        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 Б";
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024L * 1024) return $"{bytes / 1024.0:F1} КБ";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} МБ";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} ГБ";
        }

        /// <summary>Форматирует скорость в байтах/сек в читаемый вид.</summary>
        private static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec <= 0) return "0 Б/с";
            if (bytesPerSec < 1024) return $"{bytesPerSec:F0} Б/с";
            if (bytesPerSec < 1024 * 1024) return $"{bytesPerSec / 1024:F1} КБ/с";
            if (bytesPerSec < 1024 * 1024 * 1024) return $"{bytesPerSec / (1024 * 1024):F1} МБ/с";
            return $"{bytesPerSec / (1024 * 1024 * 1024):F2} ГБ/с";
        }

        // ==================== INotifyPropertyChanged ====================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
