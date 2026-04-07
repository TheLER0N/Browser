using System;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис управления прокси для обхода блокировок.
    /// НЕ требует прав администратора — прокси применяется только к WebView2.
    /// </summary>
    public class ProxyService
    {
        /// <summary>
        /// Формирует аргумент командной строки для WebView2.
        /// Формат: --proxy-server="socks5://127.0.0.1:1080" или --proxy-server="http://proxy.example.com:8080"
        /// </summary>
        public static string BuildProxyArgument(string proxyType, string server, int port, string username = "", string password = "")
        {
            if (string.IsNullOrWhiteSpace(server) || port <= 0)
                return "";

            var scheme = proxyType.ToLower() == "http" ? "http" : "socks5";
            var arg = $"--proxy-server=\"{scheme}://{server}:{port}\"";

            // WebView2/Chromium поддерживает аутентификацию через URL:
            // --proxy-server="socks5://user:pass@host:port"
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                arg = $"--proxy-server=\"{scheme}://{Uri.EscapeDataString(username)}:{Uri.EscapeDataString(password)}@{server}:{port}\"";
            }

            return arg;
        }

        /// <summary>
        /// Проверяет корректность настроек прокси.
        /// </summary>
        public static bool IsValidConfig(string bypassMode, string proxyType, string server, int port)
        {
            if (bypassMode == "none" || bypassMode == "doh_cloudflare" || bypassMode == "doh_google")
                return true; // DoH режимы валидны

            if (bypassMode == "proxy")
            {
                if (string.IsNullOrWhiteSpace(server)) return false;
                if (port <= 0 || port > 65535) return false;
                if (proxyType != "socks5" && proxyType != "http") return false;
            }

            return true;
        }

        /// <summary>
        /// Получает описание режима обхода.
        /// </summary>
        public static string GetBypassModeDescription(string mode)
        {
            return mode switch
            {
                "none" => "Без обхода (прямое подключение)",
                "doh_cloudflare" => "DNS over HTTPS (Cloudflare)",
                "doh_google" => "DNS over HTTPS (Google)",
                "proxy" => "Прокси-сервер (SOCKS5/HTTP)",
                _ => "Неизвестный режим"
            };
        }
    }
}
