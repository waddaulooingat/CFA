namespace GeoTutor.Tests.Services;

using System;
using System.Collections.Generic;
using GeoTutor.Core.Models;
using GeoTutor.Core.Services;
using Xunit;

public class MasteryServiceTests
{
    [Theory]
    [InlineData(true,  0, 0.15)]   // correct, no hints → +0.15
    [InlineData(true,  1, 0.1125)] // correct, 1 hint  → 0.15 * 0.75
    [InlineData(true,  2, 0.075)]  // correct, 2 hints → 0.15 * 0.50
    [InlineData(true,  3, 0.0375)] // correct, 3 hints → 0.15 * 0.25
    [InlineData(false, 0, -0.12)]  // wrong, no hints  → -0.12
    public void ComputeMasteryDelta_MatchesExpected(bool correct, int hints, double expected)
    {
        double delta = MasteryService.ComputeMasteryDelta(correct, hints);
        Assert.Equal(expected, delta, precision: 4);
    }

    [Fact]
    public void ApplyDecay_ZeroElapsed_NoChange()
    {
        double result = MasteryService.ApplyDecay(0.8, TimeSpan.Zero);
        Assert.Equal(0.8, result, precision: 6);
    }

    [Fact]
    public void ApplyDecay_OneWeek_Reduces3Percent()
    {
        double result = MasteryService.ApplyDecay(1.0, TimeSpan.FromDays(7));
        Assert.Equal(0.97, result, precision: 4);
    }

    [Fact]
    public void ApplyDecay_TwoWeeks_CompoundsCorrectly()
    {
        double result = MasteryService.ApplyDecay(1.0, TimeSpan.FromDays(14));
        double expected = 1.0 * 0.97 * 0.97;
        Assert.Equal(expected, result, precision: 4);
    }

    [Fact]
    public void Clamp_AboveOne_ReturnsOne()
    {
        Assert.Equal(1.0, MasteryService.Clamp(1.5));
    }

    [Fact]
    public void Clamp_BelowZero_ReturnsZero()
    {
        Assert.Equal(0.0, MasteryService.Clamp(-0.3));
    }

    [Fact]
    public void ShouldPromote_FiveCorrectFast_ReturnsTrue()
    {
        var skill = new Skill
        {
            DifficultyBand = 2,
            CurrentMastery = 0.85,
            RecentAttempts = [1.0, 1.0, 1.0, 1.0, 1.0],
        };

        bool result = MasteryService.ShouldPromote(skill, medianTimeMs: 20_000, thresholdMs: 45_000);
        Assert.True(result);
    }

    [Fact]
    public void ShouldPromote_LowMastery_ReturnsFalse()
    {
        var skill = new Skill
        {
            DifficultyBand = 1,
            CurrentMastery = 0.60,
            RecentAttempts = [1.0, 0.0, 1.0, 0.0, 1.0],
        };

        bool result = MasteryService.ShouldPromote(skill, medianTimeMs: 20_000, thresholdMs: 45_000);
        Assert.False(result);
    }

    [Fact]
    public void ShouldDemote_TwoConsecutiveWrong_ReturnsTrue()
    {
        var skill = new Skill
        {
            DifficultyBand = 3,
            RecentAttempts = [1.0, 1.0, 0.0, 0.0],
        };

        Assert.True(MasteryService.ShouldDemote(skill));
    }

    [Fact]
    public void ShouldDemote_OneWrong_ReturnsFalse()
    {
        var skill = new Skill
        {
            DifficultyBand = 3,
            RecentAttempts = [1.0, 0.0, 1.0],
        };

        Assert.False(MasteryService.ShouldDemote(skill));
    }

    [Fact]
    public void ArePrereqsMet_AllAbove70_ReturnsTrue()
    {
        var parent = new Skill { Id = "p1", CurrentMastery = 0.75 };
        var child  = new Skill { Id = "c1", PrereqIds = ["p1"] };
        var all    = new Dictionary<string, Skill> { ["p1"] = parent };

        Assert.True(MasteryService.ArePrereqsMet(child, all));
    }

    [Fact]
    public void ArePrereqsMet_OneBelow70_ReturnsFalse()
    {
        var parent = new Skill { Id = "p1", CurrentMastery = 0.65 };
        var child  = new Skill { Id = "c1", PrereqIds = ["p1"] };
        var all    = new Dictionary<string, Skill> { ["p1"] = parent };

        Assert.False(MasteryService.ArePrereqsMet(child, all));
    }
}
