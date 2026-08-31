using AdbClient.Service.Helpers;

namespace AdbClient.Service.Test.Helpers;

public class ApplicationVersionTest
{
    [Theory]
    [InlineData(1, 1, 0, "1.1.0")]
    [InlineData(1, 2, 3, "1.2.3")]
    public void Format_ReturnsThreePartVersion(int major, int minor, int build, string expected)
    {
        Assert.Equal(expected, ApplicationVersion.Format(new Version(major, minor, build, 42)));
    }

    [Theory]
    [InlineData("v1.2.0", "v1.1.0", true)]
    [InlineData("v1.1.1", "v1.1.0", true)]
    [InlineData("v1.1.0", "v1.1.0", false)]
    [InlineData("v1.0.9", "v1.1.0", false)]
    [InlineData("invalid", "v1.1.0", false)]
    public void IsNewerRelease_UsesSemanticVersionOrder(string releaseTag, string currentTag, bool expected)
    {
        Assert.Equal(expected, ApplicationVersion.IsNewerRelease(releaseTag, currentTag));
    }
}
