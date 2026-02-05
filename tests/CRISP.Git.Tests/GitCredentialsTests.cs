using CRISP.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace CRISP.Git.Tests;

public class GitCredentialsTests
{
    [Fact]
    public void GitCredentials_ShouldHoldUsernameAndPassword()
    {
        // Arrange & Act
        var credentials = new GitCredentials
        {
            Username = "test-user",
            Password = "test-token"
        };

        // Assert
        credentials.Username.Should().Be("test-user");
        credentials.Password.Should().Be("test-token");
    }
}
