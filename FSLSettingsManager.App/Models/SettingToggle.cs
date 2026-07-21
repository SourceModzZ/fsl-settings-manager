using FSLSettingsManager.App.ViewModels;

namespace FSLSettingsManager.App.Models;

public sealed class SettingToggle : ViewModelBase
{
    private bool _value;

    public required string Section { get; init; }

    public required string Key { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required bool DefaultValue { get; init; }

    public bool Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
