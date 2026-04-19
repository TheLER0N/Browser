using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace GhostBrowser.Services
{
    public class GoodbyeDPIService
    {
        private const string DownloadUrl = "https://github.com/ValdikSS/GoodbyeDPI/releases/download/0.2.3rc3/goodbyedpi-0.2.3rc3.zip";
        private const string ArchiveName = "goodbyedpi-0.2.3rc3.zip";
        private const string ExtractFolderName = "goodbyedpi-0.2.3rc3";
        private const string ExePathRelative = "goodbyedpi-0.2.3rc3\\x86_64\\goodbyedpi.exe";
        
        private readonly string _baseDir;
        private readonly string _archivePath;
        private readonly string _extractPath;
        private readonly string _exePath;
        
        private Process? _process;

        public GoodbyeDPIService()
        {
            _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GhostBrowser", "GoodbyeDPI");
            _archivePath = Path.Combine(_baseDir, ArchiveName);
            _extractPath = Path.Combine(_baseDir, ExtractFolderName);
            _exePath = Path.Combine(_baseDir, ExePathRelative);
        }

        public async Task EnsureStartedAsync()
        {
            if (IsRunning()) return;

            await EnsureDownloadedAndExtractedAsync();

            StartProcess();
        }

        private async Task EnsureDownloadedAndExtractedAsync()
        {
            if (File.Exists(_exePath)) return;

            if (!Directory.Exists(_baseDir))
            {
                Directory.CreateDirectory(_baseDir);
            }

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
                ZipFile.ExtractToDirectory(_archivePath, _baseDir);
            }
        }

        private void StartProcess()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _exePath,
                    Arguments = "-9 --fake-gen 29 --fake-from-hex 160301FFFF01FFFFFF0303594F5552204144564552544953454D454E542048455245202D202431302F6D6F",
                    UseShellExecute = true,
                    Verb = "runas", // Запрашиваем права администратора (UAC)
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(_exePath)
                };

                _process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start GoodbyeDPI: {ex.Message}");
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
            
            var processes = Process.GetProcessesByName("goodbyedpi");
            return processes.Length > 0;
        }

        private void KillProcessByName()
        {
            try
            {
                var processes = Process.GetProcessesByName("goodbyedpi");
                foreach (var p in processes)
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }
    }
}