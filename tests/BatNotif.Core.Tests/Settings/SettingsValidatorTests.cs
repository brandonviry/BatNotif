using BatNotif.Core.Settings;

namespace BatNotif.Core.Tests.Settings;

/// <summary>FR-018 : réglages incohérents refusés avec un message explicite.</summary>
public class SettingsValidatorTests
{
    [Fact]
    public void Defaults_are_valid()
    {
        var result = SettingsValidator.Validate(AppSettings.Default);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(96)]
    [InlineData(-1)]
    public void Low_threshold_outside_5_95_is_rejected(int low)
    {
        var result = SettingsValidator.Validate(AppSettings.Default with { LowThreshold = low });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("seuil bas") && e.Contains("5 %") && e.Contains("95 %"));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(101)]
    public void High_threshold_outside_5_95_is_rejected(int high)
    {
        var result = SettingsValidator.Validate(AppSettings.Default with { LowThreshold = 5, HighThreshold = high });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("seuil haut") && e.Contains("5 %") && e.Contains("95 %"));
    }

    [Theory]
    [InlineData(78, 80)]
    [InlineData(76, 80)]
    [InlineData(80, 80)]
    [InlineData(85, 80)]
    public void Gap_below_5_is_rejected_with_explicit_message(int low, int high)
    {
        var result = SettingsValidator.Validate(AppSettings.Default with { LowThreshold = low, HighThreshold = high });

        Assert.False(result.IsValid);
        Assert.Contains("Le seuil haut doit dépasser le seuil bas d'au moins 5 %.", result.Errors);
    }

    [Fact]
    public void Gap_of_exactly_5_is_accepted()
    {
        var result = SettingsValidator.Validate(AppSettings.Default with { LowThreshold = 75, HighThreshold = 80 });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Volume_outside_0_1_is_rejected(double volume)
    {
        var result = SettingsValidator.Validate(AppSettings.Default with { Volume = volume });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("volume"));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void Volume_bounds_are_accepted(double volume)
    {
        Assert.True(SettingsValidator.Validate(AppSettings.Default with { Volume = volume }).IsValid);
    }
}
