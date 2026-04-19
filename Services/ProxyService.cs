using System;
using System.Net;

namespace GhostBrowser.Services
{
    /// <summary>
    /// Сервис сетевых режимов WebView2.
    /// Настройки применяются только к встроенному браузеру и не меняют системный прокси.
    /// </summary>
    public static class ProxyService
    {
        public const string ModeDirect = "none";
        public const string ModeDoHCloudflare = "doh_cloudflare";
        public const string ModeDoHGoogle = "doh_google";
        public const string ModeManualProxy = "proxy_manual";
        public const string ModeGoodbyeDPI = "goodbyedpi";
        public const string ModeVpnXray = "vpn_xray";
        public const string ModeExperimentalPublicProxy = "proxy_experimental_public";

        public static string NormalizeMode(string? mode)
        {
            return mode switch
            {
                null or "" => ModeDirect,
                "proxy" => ModeManualProxy,
                ModeDirect => ModeDirect,
                ModeGoodbyeDPI => ModeGoodbyeDPI,
                ModeVpnXray => ModeVpnXray,
                ModeDoHCloudflare => ModeDoHCloudflare,
                ModeDoHGoogle => ModeDoHGoogle,
                ModeManualProxy => ModeManualProxy,
                ModeExperimentalPublicProxy => ModeExperimentalPublicProxy,
                _ => ModeDirect
            };
        }

        public static bool IsProxyMode(string? mode)
        {
            var normalized = NormalizeMode(mode);
            return normalized == ModeManualProxy || normalized == ModeExperimentalPublicProxy;
        }

        public static bool IsExperimentalProxyMode(string? mode) =>
            NormalizeMode(mode) == ModeExperimentalPublicProxy;

        /// <summary>
        /// Формирует аргумент командной строки для ручного HTTP/SOCKS5 прокси.
        /// </summary>
        public static string BuildProxyArgument(string proxyType, string server, int port, string username = "", string password = "")
        {
            server = server?.Trim() ?? "";
            username = username?.Trim() ?? "";
            password = password?.Trim() ?? "";

            if (!IsValidProxyEndpoint(proxyType, server, port))
                return "";

            var scheme = NormalizeProxyType(proxyType);
            var arg = $"--proxy-server=\"{scheme}://{server}:{port}\"";

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                arg = $"--proxy-server=\"{scheme}://{Uri.EscapeDataString(username)}:{Uri.EscapeDataString(password)}@{server}:{port}\"";
            }

            return arg;
        }

        /// <summary>
        /// Проверяет корректность сетевого режима и ручных прокси-настроек.
        /// </summary>
        public static bool IsValidConfig(string mode, string proxyType, string server, int port)
        {
            var normalizedMode = NormalizeMode(mode);
            if (normalizedMode == ModeDirect || normalizedMode == ModeDoHCloudflare || normalizedMode == ModeDoHGoogle || normalizedMode == ModeGoodbyeDPI || normalizedMode == ModeVpnXray)
                return true;

            if (normalizedMode == ModeExperimentalPublicProxy)
                return true;

            if (normalizedMode == ModeManualProxy)
                return IsValidProxyEndpoint(proxyType, server, port);

            return false;
        }

        public static string GetBypassModeDescription(string mode)
        {
            return NormalizeMode(mode) switch
            {
                ModeDirect => "Прямое подключение",
                ModeGoodbyeDPI => "Обход блокировок (GoodbyeDPI)",
                ModeVpnXray => "VPN (Xray)",
                ModeDoHCloudflare => "DNS over HTTPS (Cloudflare)",
                ModeDoHGoogle => "DNS over HTTPS (Google)",
                ModeManualProxy => "Ручной прокси (SOCKS5/HTTP)",
                ModeExperimentalPublicProxy => "Экспериментальный публичный прокси",
                _ => "Неизвестный сетевой режим"
            };
        }

        private static string NormalizeProxyType(string proxyType) =>
            string.Equals(proxyType, "http", StringComparison.OrdinalIgnoreCase) ? "http" : "socks5";

        private static bool IsValidProxyEndpoint(string proxyType, string server, int port)
        {
            if (string.IsNullOrWhiteSpace(server))
                return false;
            if (port <= 0 || port > 65535)
                return false;

            if (!string.Equals(proxyType, "socks5", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(proxyType, "http", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var trimmed = server.Trim();
            if (Uri.CheckHostName(trimmed) != UriHostNameType.Unknown)
                return true;

            return IPAddress.TryParse(trimmed, out _);
        }
    }
}
