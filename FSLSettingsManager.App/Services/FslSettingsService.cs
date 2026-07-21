using System.IO;
using System.Linq;
using System.Text;
using FSLSettingsManager.App.Models;

namespace FSLSettingsManager.App.Services;

public sealed class FslSettingsService
{
    private static readonly string[] IgnoredUserDirectories =
    [
        "All Users",
        "Default",
        "Default User",
        "defaultuser0",
        "Public"
    ];

    public IReadOnlyList<FslProfile> FindProfiles(out int checkedUserCount, out int incompleteProfileCount)
    {
        var profiles = new List<FslProfile>();
        checkedUserCount = 0;
        incompleteProfileCount = 0;

        const string usersRoot = @"C:\Users";
        if (!Directory.Exists(usersRoot))
        {
            return profiles;
        }

        foreach (var userDirectory in Directory.GetDirectories(usersRoot))
        {
            try
            {
                var userName = Path.GetFileName(userDirectory);
                if (string.IsNullOrWhiteSpace(userName) ||
                    IgnoredUserDirectories.Contains(userName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                checkedUserCount++;

                var fslDirectory = Path.Combine(userDirectory, "AppData", "Roaming", "FSL");
                var settingsPath = Path.Combine(fslDirectory, "settings.ini");
                var hasFslDirectory = Directory.Exists(fslDirectory);
                var hasSettingsFile = File.Exists(settingsPath);

                if (!hasFslDirectory && !hasSettingsFile)
                {
                    continue;
                }

                if (!hasFslDirectory || !hasSettingsFile)
                {
                    incompleteProfileCount++;
                    continue;
                }

                profiles.Add(new FslProfile
                {
                    UserName = userName,
                    FslDirectory = fslDirectory,
                    SettingsFilePath = settingsPath,
                    StatusText = "Ready to edit",
                    IsReady = true
                });
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
        }

        return profiles
            .OrderBy(profile => profile.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void LoadSettings(string settingsFilePath, IEnumerable<SettingToggle> toggles)
    {
        var values = new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (var rawLine in File.ReadAllLines(settingsFilePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                if (!values.ContainsKey(currentSection))
                {
                    values[currentSection] = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                }

                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || currentSection is null)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var rawValue = line[(separatorIndex + 1)..].Trim();

            if (bool.TryParse(rawValue, out var parsedValue))
            {
                values[currentSection][key] = parsedValue;
            }
        }

        foreach (var toggle in toggles)
        {
            toggle.Value = values.TryGetValue(toggle.Section, out var sectionValues) &&
                           sectionValues.TryGetValue(toggle.Key, out var storedValue)
                ? storedValue
                : toggle.DefaultValue;
        }
    }

    public void SaveSettings(string settingsFilePath, IEnumerable<SettingToggle> toggles)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);

        if (File.Exists(settingsFilePath))
        {
            File.Copy(settingsFilePath, $"{settingsFilePath}.bak", overwrite: true);
        }

        var groupedToggles = toggles
            .GroupBy(toggle => toggle.Section)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var builder = new StringBuilder();
        AppendSection(builder, "Global", groupedToggles);
        builder.AppendLine();
        AppendSection(builder, "GTA", groupedToggles);

        File.WriteAllText(settingsFilePath, builder.ToString().TrimEnd() + Environment.NewLine, Encoding.UTF8);
    }

    private static void AppendSection(
        StringBuilder builder,
        string sectionName,
        IReadOnlyDictionary<string, List<SettingToggle>> groupedToggles)
    {
        builder.AppendLine($"[{sectionName}]");

        if (!groupedToggles.TryGetValue(sectionName, out var sectionToggles))
        {
            return;
        }

        foreach (var toggle in sectionToggles)
        {
            builder.AppendLine($"{toggle.Key} = {toggle.Value.ToString().ToLowerInvariant()}");
        }
    }
}
