namespace FSLSettingsManager.App.Models;

public sealed class FslProfile
{
    public required string UserName { get; init; }

    public required string FslDirectory { get; init; }

    public required string SettingsFilePath { get; init; }

    public required string StatusText { get; init; }

    public required bool IsReady { get; init; }
}
