namespace GhostBrowser.Models
{
    /// <summary>
    /// Результат импорта/синхронизации закладок.
    /// Используется для отображения статистики после импорта.
    /// </summary>
    public class SyncResult
    {
        /// <summary>Количество новых закладок, добавленных из файла.</summary>
        public int Added { get; set; }

        /// <summary>Количество закладок, пропущенных как дубликаты.</summary>
        public int Skipped { get; set; }

        /// <summary>Количество ошибок при импорте (невалидный JSON и т.д.).</summary>
        public int Errors { get; set; }

        /// <summary>Общее количество закладок в импортируемом файле.</summary>
        public int TotalImported { get; set; }

        /// <summary>Сообщение об ошибке (если возникла).</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Импорт прошёл без ошибок.</summary>
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
}
