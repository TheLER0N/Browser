using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;

namespace GhostBrowser.Services
{
    public class XrayService
    {
        private const string DownloadUrl = "https://github.com/XTLS/Xray-core/releases/download/v1.8.24/Xray-windows-64.zip";
        private const string ArchiveName = "Xray-windows-64.zip";
        private const string ExtractFolderName = "xray-core";
        private const string ExePathRelative = "xray.exe";
        private const string ConfigFileName = "config.json";
        
        private readonly string _baseDir;
        private readonly string _archivePath;
        private readonly string _extractPath;
        private readonly string _exePath;
        private readonly string _configPath;
        
        private Process? _process;

        public XrayService()
        {
            _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser", "Xray");
            _archivePath = Path.Combine(_baseDir, ArchiveName);
            _extractPath = Path.Combine(_baseDir, ExtractFolderName);
            _exePath = Path.Combine(_extractPath, ExePathRelative);
            _configPath = Path.Combine(_extractPath, ConfigFileName);
        }

        public async Task EnsureStartedAsync(string vpnKey)
        {
            if (IsRunning()) Stop();

            await EnsureDownloadedAndExtractedAsync();

            GenerateConfig(vpnKey);
            StartProcess();
        }

        private async Task EnsureDownloadedAndExtractedAsync()
        {
            if (File.Exists(_exePath)) return;

            if (!Directory.Exists(_baseDir)) Directory.CreateDirectory(_baseDir);

            if (!File.Exists(_archivePath))
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(DownloadUrl);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(_archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);
            }

            if (!Directory.Exists(_extractPath))
            {
                Directory.CreateDirectory(_extractPath);
                ZipFile.ExtractToDirectory(_archivePath, _extractPath);
            }
        }

        private void GenerateConfig(string vpnKey)
        {
            try
            {
                string configJson = "";
                if (vpnKey.StartsWith("vless://"))
                {
                    configJson = ParseVlessToConfig(vpnKey);
                }
                else
                {
                    throw new Exception("Поддерживаются только VLESS ссылки.");
                }

                File.WriteAllText(_configPath, configJson, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate Xray config: {ex.Message}");
                throw;
            }
        }

        private string ParseVlessToConfig(string vlessUri)
        {
            // vless://uuid@host:port?param=value#name
            var uri = new Uri(vlessUri);
            string uuid = uri.UserInfo;
            string address = uri.Host;
            int port = uri.Port;
            
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            string security = queryParams["security"] ?? "none";
            string type = queryParams["type"] ?? "tcp";
            string flow = queryParams["flow"] ?? "";
            
            // TLS/Reality params
            string sni = queryParams["sni"] ?? "";
            string fp = queryParams["fp"] ?? "chrome";
            string pbk = queryParams["pbk"] ?? "";
            string sid = queryParams["sid"] ?? "";
            string alpn = queryParams["alpn"] ?? "";

            // Transport params
            string serviceName = queryParams["serviceName"] ?? "";
            string host = queryParams["host"] ?? "";
            string path = queryParams["path"] ?? "/";

            // SOCKS5 in port 10808
            string inbounds = @"{ ""port"": 10808, ""listen"": ""127.0.0.1"", ""protocol"": ""socks"", ""settings"": { ""udp"": true } }";

            // Outbounds stream settings
            string streamSettings = $@"
                ""network"": ""{type}"",
                ""security"": ""{security}""
            ";

            if (security == "reality")
            {
                streamSettings += $@",
                ""realitySettings"": {{
                    ""serverName"": ""{sni}"",
                    ""publicKey"": ""{pbk}"",
                    ""shortId"": ""{sid}"",
                    ""fingerprint"": ""{fp}"",
                    ""spiderX"": """"
                }}";
            }
            else if (security == "tls")
            {
                streamSettings += $@",
                ""tlsSettings"": {{
                    ""serverName"": ""{(string.IsNullOrEmpty(sni) ? address : sni)}"",
                    ""fingerprint"": ""{fp}""
                }}";
            }

            if (type == "grpc")
            {
                streamSettings += $@",
                ""grpcSettings"": {{
                    ""serviceName"": ""{serviceName}"",
                    ""multiMode"": true
                }}";
            }
            else if (type == "ws")
            {
                streamSettings += $@",
                ""wsSettings"": {{
                    ""path"": ""{path}"",
                    ""headers"": {{ ""Host"": ""{(string.IsNullOrEmpty(host) ? sni : host)}"" }}
                }}";
            }
            else if (type == "tcp")
            {
                // TCP has HTTP header obfuscation optionally
                if (!string.IsNullOrEmpty(queryParams["headerType"]) && queryParams["headerType"] == "http")
                {
                    streamSettings += $@",
                    ""tcpSettings"": {{
                        ""header"": {{
                            ""type"": ""http"",
                            ""request"": {{
                                ""path"": [""{path}""],
                                ""headers"": {{ ""Host"": [""{(string.IsNullOrEmpty(host) ? sni : host)}""] }}
                            }}
                        }}
                    }}";
                }
            }

            string outbounds = $@"{{
                ""protocol"": ""vless"",
                ""settings"": {{
                    ""vnext"": [
                        {{
                            ""address"": ""{address}"",
                            ""port"": {port},
                            ""users"": [
                                {{
                                    ""id"": ""{uuid}"",
                                    ""encryption"": ""none"",
                                    ""flow"": ""{flow}""
                                }}
                            ]
                        }}
                    ]
                }},
                ""streamSettings"": {{
                    {streamSettings}
                }}
            }}";

            return $@"{{
                ""log"": {{ ""loglevel"": ""warning"" }},
                ""inbounds"": [{inbounds}],
                ""outbounds"": [{outbounds}]
            }}";
        }

        private void StartProcess()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _exePath,
                    Arguments = $"run -c \"{_configPath}\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = _extractPath
                };

                _process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start Xray: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                    }
                }
                catch { }
                _process.Dispose();
                _process = null;
            }
            
            KillProcessByName();
        }

        private bool IsRunning()
        {
            if (_process != null && !_process.HasExited) return true;
            
            var processes = Process.GetProcessesByName("xray");
            return processes.Length > 0;
        }

        private void KillProcessByName()
        {
            try
            {
                var processes = Process.GetProcessesByName("xray");
                foreach (var p in processes)
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }
    }
}