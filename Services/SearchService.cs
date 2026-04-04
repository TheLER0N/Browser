using System;

namespace GhostBrowser.Services
{
    public class SearchService
    {
        public enum SearchEngine
        {
            Google,
            Bing,
            DuckDuckGo,
            Yandex
        }

        private SearchEngine _currentEngine = SearchEngine.Google;

        public SearchEngine CurrentEngine
        {
            get => _currentEngine;
            set
            {
                _currentEngine = value;
                EngineChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<SearchEngine>? EngineChanged;

        public string GetSearchUrl(string query)
        {
            var encodedQuery = Uri.EscapeDataString(query);
            
            return _currentEngine switch
            {
                SearchEngine.Google => $"https://www.google.com/search?q={encodedQuery}",
                SearchEngine.Bing => $"https://www.bing.com/search?q={encodedQuery}",
                SearchEngine.DuckDuckGo => $"https://duckduckgo.com/?q={encodedQuery}",
                SearchEngine.Yandex => $"https://yandex.ru/search/?text={encodedQuery}",
                _ => $"https://www.google.com/search?q={encodedQuery}"
            };
        }

        public bool IsSearchQuery(string input)
        {
            // If it doesn't look like a URL, treat as search query
            if (!input.Contains(".") && !input.StartsWith("http"))
            {
                return true;
            }

            // Check for common URL patterns
            if (input.StartsWith("http://") || input.StartsWith("https://") || 
                input.StartsWith("www.") || input.StartsWith("ghost://"))
            {
                return false;
            }

            // If it has spaces, it's probably a search query
            if (input.Contains(" "))
            {
                return true;
            }

            return false;
        }

        public string NormalizeUrl(string input)
        {
            if (IsSearchQuery(input))
            {
                return GetSearchUrl(input);
            }

            if (!input.StartsWith("http://") && !input.StartsWith("https://") && !input.StartsWith("ghost://"))
            {
                return "https://" + input;
            }

            return input;
        }

        public string GetEngineIcon(SearchEngine engine)
        {
            return engine switch
            {
                SearchEngine.Google => "G",
                SearchEngine.Bing => "B",
                SearchEngine.DuckDuckGo => "D",
                SearchEngine.Yandex => "Я",
                _ => "?"
            };
        }
    }
}
