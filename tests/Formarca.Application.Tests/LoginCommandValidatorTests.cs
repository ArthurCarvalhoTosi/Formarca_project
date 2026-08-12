using Formarca.Application.IdentityAccess.Auth.Commands;
using Xunit;

namespace Formarca.Application.Tests;

public class LoginCommandValidatorTests
{
    [Fact]
    public void Rejects_Empty_Email()
    {
        var validator = new LoginCommandValidator();
        var result = validator.Validate(new LoginCommand("", "password"));
        Assert.False(result.IsValid);
    }
}
