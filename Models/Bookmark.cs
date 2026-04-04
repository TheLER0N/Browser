using System;

namespace GhostBrowser.Models
{
    public class Bookmark
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Favicon { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
