namespace GhostBrowser.Models
{
    /// <summary>
    /// Статус загрузки файла.
    /// </summary>
    public enum DownloadItemStatus
    {
        /// <summary>Загрузка активна.</summary>
        Downloading,
        /// <summary>Загрузка приостановлена пользователем.</summary>
        Paused,
        /// <summary>Загрузка завершена успешно.</summary>
        Completed,
        /// <summary>Загрузка отменена пользователем.</summary>
        Cancelled,
        /// <summary>Ошибка загрузки (сетевая ошибка, недоступность сервера и т.д.).</summary>
        Failed
    }
}
