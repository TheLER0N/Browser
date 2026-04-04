using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Threading.Tasks;
using GhostBrowser.Services;

namespace GhostBrowser.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        private string _selectedSection = "DNS";

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
            SaveCommand = new RelayCommand(_ => Save());
            ResetCommand = new RelayCommand(_ => ResetToDefaults());
            TestDnsCommand = new RelayCommand(_ => TestDnsAsync(), _ => !string.IsNullOrEmpty(CustomDns));
            
            Sections = new List<string> { "DNS", "Общие", "Приватность" };
        }

        public List<string> Sections { get; }
        public List<DnsPreset> DnsPresets => SettingsService.DnsPresets;

        public string SelectedSection
        {
            get => _selectedSection;
            set => Set(ref _selectedSection, value);
        }

        // Delegate all properties to SettingsService
        public bool UseCustomDns
        {
            get => _settingsService.UseCustomDns;
            set => _settingsService.UseCustomDns = value;
        }

        public string CustomDns
        {
            get => _settingsService.CustomDns;
            set
            {
                if (_settingsService.CustomDns != value)
                {
                    _settingsService.CustomDns = value;
                    OnPropertyChanged();
                    if (TestDnsCommand is RelayCommand cmd) cmd.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedDnsPreset
        {
            get => _settingsService.SelectedDnsPreset;
            set => _settingsService.SelectedDnsPreset = value;
        }

        public bool DarkTheme
        {
            get => _settingsService.DarkTheme;
            set => _settingsService.DarkTheme = value;
        }

        public double FontSize
        {
            get => _settingsService.FontSize;
            set => _settingsService.FontSize = value;
        }

        public string HomePage
        {
            get => _settingsService.HomePage;
            set => _settingsService.HomePage = value;
        }

        public string DefaultSearchEngine
        {
            get => _settingsService.DefaultSearchEngine;
            set => _settingsService.DefaultSearchEngine = value;
        }

        public bool BlockTrackers
        {
            get => _settingsService.BlockTrackers;
            set => _settingsService.BlockTrackers = value;
        }

        public bool BlockThirdPartyCookies
        {
            get => _settingsService.BlockThirdPartyCookies;
            set => _settingsService.BlockThirdPartyCookies = value;
        }

        public bool IsTestingDns => _settingsService.IsTestingDns;
        public string DnsTestResult => _settingsService.DnsTestResult;
        public string SaveNotification => _settingsService.SaveNotification;

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand TestDnsCommand { get; }

        private void Save() => _settingsService.SaveSettings();

        private void ResetToDefaults() => _settingsService.ResetToDefaults();

        private async void TestDnsAsync()
        {
            await _settingsService.TestDnsAsync(CustomDns);
            OnPropertyChanged(nameof(DnsTestResult));
            OnPropertyChanged(nameof(IsTestingDns));
        }

        private SettingsService SettingsService => _settingsService;
    }
}
