using MHPatcher;

namespace MHPatcher.Tests;

public class PatcherTests
{
    static Patch P(string name, string offset, string data, bool enabled = true, string? group = null) =>
        new() { Name = name, Offset = offset, Data = data, IsEnabled = enabled, Group = group };

    [Fact]
    public void Apply_WritesBytesAtOffset()
    {
        byte[] file = new byte[8];
        var patches = new[] { P("a", "0x2", "AABB"), P("off", "0x0", "FF", enabled: false) };

        Assert.Empty(Patcher.Validate(patches, file.Length));
        Patcher.Apply(file, patches);

        Assert.Equal(new byte[] { 0, 0, 0xAA, 0xBB, 0, 0, 0, 0 }, file);
    }

    [Fact]
    public void Validate_PatchEndingExactlyAtEndOfFile_IsAllowed()
    {
        Assert.Empty(Patcher.Validate(new[] { P("a", "0x6", "AABB") }, 8));
    }

    [Fact]
    public void Validate_PatchRunningPastEndOfFile_IsRejected()
    {
        // Old code only checked the start offset, so this crashed with IndexOutOfRangeException.
        var errors = Patcher.Validate(new[] { P("a", "0x7", "AABB") }, 8);
        Assert.Single(errors);
        Assert.Contains("outside the file", errors[0]);
    }

    [Fact]
    public void Validate_OverlappingPatches_AreRejected()
    {
        var errors = Patcher.Validate(new[] { P("a", "0x0", "AABBCC"), P("b", "0x2", "DD") }, 8);
        Assert.Single(errors);
        Assert.Contains("same bytes", errors[0]);
    }

    [Fact]
    public void Validate_AdjacentPatches_AreAllowed()
    {
        Assert.Empty(Patcher.Validate(new[] { P("a", "0x0", "AABB"), P("b", "0x2", "CC") }, 8));
    }

    [Fact]
    public void Validate_PartlyEnabledGroup_IsRejected()
    {
        var patches = new[]
        {
            P("g1", "0x0", "AA", enabled: true, group: "G"),
            P("g2", "0x1", "BB", enabled: false, group: "G")
        };

        var errors = Patcher.Validate(patches, 8);
        Assert.Single(errors);
        Assert.Contains("partly enabled", errors[0]);
    }

    [Theory]
    [InlineData("zz", "AA")]
    [InlineData("0x0", "ABC")]
    [InlineData("0x0", "XY")]
    [InlineData("0x0", "")]
    public void Validate_BadOffsetOrData_IsRejectedNotThrown(string offset, string data)
    {
        Assert.Single(Patcher.Validate(new[] { P("a", offset, data) }, 8));
    }

    [Fact]
    public void Validate_IgnoresDisabledPatches()
    {
        Assert.Empty(Patcher.Validate(new[] { P("a", "0xFFFF", "AA", enabled: false) }, 8));
    }

    [Fact]
    public void GetChoices_CollapsesGroupsInFileOrder()
    {
        var patches = new[]
        {
            P("a", "0x0", "AA"),
            P("g1", "0x1", "AA", group: "G"),
            P("b", "0x2", "AA"),
            P("g2", "0x3", "AA", group: "G")
        };

        var choices = Patcher.GetChoices(patches);

        Assert.Equal(new[] { "a", "G", "b" }, choices.Select(c => c.Name));
        Assert.Equal(2, choices[1].Patches.Count);
    }

    [Theory]
    [InlineData("1", new[] { 0 })]
    [InlineData("1,3", new[] { 0, 2 })]
    [InlineData(" 3 , 1 ,1", new[] { 2, 0 })]
    [InlineData("2 3", new[] { 1, 2 })]
    public void ParseSelection_ValidInput(string input, int[] expected)
    {
        Assert.Equal(expected, Patcher.ParseSelection(input, 3));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("4")]
    [InlineData("a")]
    [InlineData("1,x")]
    public void ParseSelection_InvalidInput_ReturnsNull(string input)
    {
        Assert.Null(Patcher.ParseSelection(input, 3));
    }

    [Fact]
    public void Load_PatchWithoutGroup_HasNullGroup()
    {
        string json = "[{\"Name\":\"a\",\"IsEnabled\":true,\"Offset\":\"0x1\",\"Data\":\"AA\"}]";
        Patch[] patches = Patcher.Load(json);

        Assert.Single(patches);
        Assert.Null(patches[0].Group);
        Assert.True(patches[0].IsEnabled);
    }

    [Theory]
    [InlineData("Patches32.json")]
    [InlineData("Patches64.json")]
    public void ShippedPatchFiles_AreValidWithEverythingEnabledExceptKnownConflicts(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, fileName);
        Patch[] patches = Patcher.Load(File.ReadAllText(path));

        Assert.NotEmpty(patches);
        Assert.All(patches, p => Assert.False(p.IsEnabled));

        foreach (Patch patch in patches)
            patch.IsEnabled = true;

        // The client exe is larger than every shipped offset; int.MaxValue skips the size check.
        Assert.Empty(Patcher.Validate(patches, int.MaxValue));
    }

    [Fact]
    public void ShippedCustomPortsPatch_IsOneSixPartGroup()
    {
        Patch[] patches = Patcher.Load(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Patches64.json")));
        var group = Patcher.GetChoices(patches).Single(c => c.Name == "Custom ports");

        Assert.Equal(6, group.Patches.Count);
        Assert.All(patches.Where(p => p.Name.StartsWith("Custom ports")), p => Assert.Equal("Custom ports", p.Group));
    }
}
