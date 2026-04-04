using System;

namespace GhostBrowser.Models
{
    public class HistoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTime VisitedAt { get; set; } = DateTime.Now;
    }
}
