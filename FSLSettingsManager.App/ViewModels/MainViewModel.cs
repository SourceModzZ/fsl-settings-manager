using System.Collections.ObjectModel;
using System.Diagnostics;
using FSLSettingsManager.App.Models;
using FSLSettingsManager.App.Services;

namespace FSLSettingsManager.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private const string DownloadUrl = "https://www.unknowncheats.me/forum/grand-theft-auto-v/616977-fsl-local-gtao-saves.html";

    private readonly FslSettingsService _settingsService = new();
    private FslProfile? _selectedProfile;
    private string _statusTitle = "Checking FSL profiles...";
    private string _statusMessage = "Scan the local user profiles to find ready-to-edit settings.ini files.";
    private string _selectedProfileName = "No ready profile selected";
    private string _selectedProfilePath = "Select a detected FSL profile to edit its settings.";
    private string _footerMessage = "Restart GTA V after saving so FSL can reload the updated settings.";
    private bool _showNotice = true;
    private bool _isEditorEnabled;

    public MainViewModel()
    {
        Profiles = new ObservableCollection<FslProfile>();
        Settings = new ObservableCollection<SettingToggle>(CreateDefaultSettings());

        RefreshCommand = new RelayCommand(RefreshProfiles);
        ApplyCommand = new RelayCommand(ApplyChanges, () => IsEditorEnabled);
        OpenSelectedFolderCommand = new RelayCommand(OpenSelectedFolder, () => SelectedProfile is not null);
        DownloadFslCommand = new RelayCommand(OpenDownloadPage);
    }

    public ObservableCollection<FslProfile> Profiles { get; }

    public ObservableCollection<SettingToggle> Settings { get; }

    public RelayCommand RefreshCommand { get; }

    public RelayCommand ApplyCommand { get; }

    public RelayCommand OpenSelectedFolderCommand { get; }

    public RelayCommand DownloadFslCommand { get; }

    public FslProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (!SetProperty(ref _selectedProfile, value))
            {
                return;
            }

            LoadSelectedProfile();
            OpenSelectedFolderCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusTitle
    {
        get => _statusTitle;
        private set => SetProperty(ref _statusTitle, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedProfileName
    {
        get => _selectedProfileName;
        private set => SetProperty(ref _selectedProfileName, value);
    }

    public string SelectedProfilePath
    {
        get => _selectedProfilePath;
        private set => SetProperty(ref _selectedProfilePath, value);
    }

    public string FooterMessage
    {
        get => _footerMessage;
        private set => SetProperty(ref _footerMessage, value);
    }

    public bool ShowNotice
    {
        get => _showNotice;
        private set => SetProperty(ref _showNotice, value);
    }

    public bool IsEditorEnabled
    {
        get => _isEditorEnabled;
        private set
        {
            if (!SetProperty(ref _isEditorEnabled, value))
            {
                return;
            }

            ApplyCommand.RaiseCanExecuteChanged();
        }
    }

    public string NoticeMessage =>
        "FSL was not found in any user AppData folder with a ready settings.ini file. Please download FSL, start it once, and then refresh this manager.";

    public void RefreshProfiles()
    {
        try
        {
            var profiles = _settingsService.FindProfiles(out var checkedUserCount, out var incompleteProfileCount);

            Profiles.Clear();
            foreach (var profile in profiles)
            {
                Profiles.Add(profile);
            }

            if (Profiles.Count == 0)
            {
                SelectedProfile = null;
                StatusTitle = "No ready FSL profile found";
                StatusMessage = $"Checked {checkedUserCount} user profile(s). Incomplete FSL folders skipped: {incompleteProfileCount}.";
                SelectedProfileName = "No ready profile selected";
                SelectedProfilePath = "Download FSL, launch it once, and then click Refresh.";
                FooterMessage = "The editor becomes available after a valid %APPDATA%\\FSL\\settings.ini is detected.";
                ShowNotice = true;
                IsEditorEnabled = false;
                ResetSettingsToDefaults();
                return;
            }

            StatusTitle = $"Ready profiles found: {Profiles.Count}";
            StatusMessage = $"Checked {checkedUserCount} user profile(s). Incomplete FSL folders skipped: {incompleteProfileCount}.";
            FooterMessage = "Restart GTA V after saving so FSL can reload the updated settings.";
            ShowNotice = false;

            SelectedProfile = Profiles[0];
        }
        catch (Exception ex)
        {
            SelectedProfile = null;
            StatusTitle = "Profile scan failed";
            StatusMessage = $"Windows returned an error while checking the user folders: {ex.Message}";
            SelectedProfileName = "No ready profile selected";
            SelectedProfilePath = "Fix the access problem and click Refresh again.";
            FooterMessage = "The editor is disabled until the scan succeeds.";
            ShowNotice = true;
            IsEditorEnabled = false;
            ResetSettingsToDefaults();
        }
    }

    private void LoadSelectedProfile()
    {
        if (SelectedProfile is null)
        {
            IsEditorEnabled = false;
            SelectedProfileName = "No ready profile selected";
            SelectedProfilePath = "Select a detected FSL profile to edit its settings.";
            return;
        }

        try
        {
            _settingsService.LoadSettings(SelectedProfile.SettingsFilePath, Settings);
            IsEditorEnabled = true;
            SelectedProfileName = SelectedProfile.UserName;
            SelectedProfilePath = SelectedProfile.SettingsFilePath;
            FooterMessage = $"Loaded settings.ini for {SelectedProfile.UserName}. Click Apply to save changes.";
        }
        catch (Exception ex)
        {
            IsEditorEnabled = false;
            SelectedProfileName = SelectedProfile.UserName;
            SelectedProfilePath = SelectedProfile.SettingsFilePath;
            FooterMessage = $"Could not read settings.ini: {ex.Message}";
        }
    }

    private void ApplyChanges()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        try
        {
            _settingsService.SaveSettings(SelectedProfile.SettingsFilePath, Settings);
            FooterMessage = $"Saved settings.ini for {SelectedProfile.UserName}. Restart GTA V to apply the new FSL settings.";
        }
        catch (Exception ex)
        {
            FooterMessage = $"Saving failed: {ex.Message}";
        }
    }

    private void OpenSelectedFolder()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{SelectedProfile.FslDirectory}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            FooterMessage = $"Could not open the folder: {ex.Message}";
        }
    }

    private void OpenDownloadPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DownloadUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            FooterMessage = $"Could not open the download page: {ex.Message}";
        }
    }

    private void ResetSettingsToDefaults()
    {
        foreach (var toggle in Settings)
        {
            toggle.Value = toggle.DefaultValue;
        }
    }

    private static IEnumerable<SettingToggle> CreateDefaultSettings()
    {
        return
        [
            new SettingToggle
            {
                Section = "Global",
                Key = "BlockTelemetry",
                Title = "Block Telemetry",
                Description = "Controls the global telemetry-blocking option.",
                DefaultValue = true,
                Value = true
            },
            new SettingToggle
            {
                Section = "GTA",
                Key = "UseLocalSaves",
                Title = "Use Local Saves",
                Description = "Loads and stores GTA Online save data locally through FSL.",
                DefaultValue = true,
                Value = true
            },
            new SettingToggle
            {
                Section = "GTA",
                Key = "EnableMoneyCheatCodes",
                Title = "Enable Money Cheat Codes",
                Description = "Allows the integrated FSL money code console inside GTA Online.",
                DefaultValue = true,
                Value = true
            },
            new SettingToggle
            {
                Section = "GTA",
                Key = "UnlockGTAPlus",
                Title = "Unlock GTA+",
                Description = "Toggles the FSL setting for GTA+ unlocking.",
                DefaultValue = true,
                Value = true
            },
            new SettingToggle
            {
                Section = "GTA",
                Key = "BypassBattlEye",
                Title = "Bypass BattlEye",
                Description = "Controls the BattlEye bypass flag stored in the configuration file.",
                DefaultValue = false,
                Value = false
            },
            new SettingToggle
            {
                Section = "GTA",
                Key = "UnlockCESP",
                Title = "Unlock CESP",
                Description = "Toggles the Criminal Enterprise Starter Pack unlock setting.",
                DefaultValue = true,
                Value = true
            },
            new SettingToggle
            {
                Section = "GTA",
                Key = "EnableBackups",
                Title = "Enable Backups",
                Description = "Keeps the regular FSL backup behavior enabled.",
                DefaultValue = true,
                Value = true
            }
        ];
    }
}
