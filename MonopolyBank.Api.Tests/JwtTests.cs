using Shouldly;
using Xunit;

namespace MonopolyBank.Api.Tests;

public class JwtTests
{
    [Fact]
    public void ShouldGenerateJwt()
    {
        var headerAlg = "HS256";
        var headerTyp = "JWT";
        var payload = new Dictionary<string, object>
        {
            { "GameId", "773c39de-c9c5-4046-8269-5dbccdef3feb" },
            { "UserId", "291fc84d-5853-4374-bb26-3bd76b92f49e" },
        };
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";
        var jwt = JwtService.GenerateJwt(headerAlg, headerTyp, payload, secret);

        jwt.ShouldNotBeNullOrWhiteSpace();
        jwt.Split('.').Length.ShouldBe(3);
        jwt.ShouldBe(
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODI2OS01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4");
    }

    [Fact]
    public void ShouldValidateJwt()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODI2OS01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4";
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";
        var isValid = JwtService.ValidateJwt(jwt, secret);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void ShouldNotValidateJwtWithInvalidSecret()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODI2OS01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4";
        var secret = "ThisIsAVerySafeSecret";
        var isValid = JwtService.ValidateJwt(jwt, secret);
        isValid.ShouldBeFalse();
    }
    
    [Fact]
    public void ShouldReadJwt()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODI2OS01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4";
        var payload = new Dictionary<string, object>
        {
            { "GameId", "773c39de-c9c5-4046-8269-5dbccdef3feb" },
            { "UserId", "291fc84d-5853-4374-bb26-3bd76b92f49e" },
        };
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";
        var result = JwtService.ReadJwt(jwt, secret);
        result.ShouldBe(payload);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("none")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("inl")]
    [InlineData("smth_rndm")]
    [InlineData("1234567890")]
    public void AlgCanNotBeNone(string? alg)
    {
        var headerAlg = alg;
        var headerTyp = "JWT";
        var payload = new Dictionary<string, object>
        {
            { "GameId", "773c39de-c9c5-4046-8269-5dbccdef3feb" },
            { "UserId", "291fc84d-5853-4374-bb26-3bd76b92f49e" },
        };
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";
        Should.Throw<ArgumentException>(() => JwtService.GenerateJwt(headerAlg, headerTyp, payload, secret))
            .Message.ShouldBe("Alg must be \"HS256\".");
    }
    
    [Theory]
    [InlineData("HS512")]
    [InlineData("RS256")]
    [InlineData("hs256")] // JWT alg names are case-sensitive (RFC 7518)
    public void OnlyHs256IsSupported(string alg)
    {
        var payload = new Dictionary<string, object>
        {
            { "GameId", "773c39de-c9c5-4046-8269-5dbccdef3feb" },
            { "UserId", "291fc84d-5853-4374-bb26-3bd76b92f49e" },
        };
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";

        Should.Throw<ArgumentException>(() => JwtService.GenerateJwt(alg, "JWT", payload, secret))
            .Message.ShouldBe("Alg must be \"HS256\".");
    }

    // named theories
    [Fact]
    public void AlgNoneShouldNotBeValid()
    {
        var jwt =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODg4OC01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.";
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";
        Should.Throw<ArgumentException>(() => JwtService.ValidateJwt(jwt, secret))
            .Message.ShouldBe("Alg must be \"HS256\".");
        Should.Throw<ArgumentException>(() => JwtService.ReadJwt(jwt, secret))
            .Message.ShouldBe("Alg must be \"HS256\".");
    }
    
    [Fact]
    public void PayloadManipulationShouldNotBeAllowed()
    {
        // I took the original jwt and changed the payload's gameId to "773c39de-c9c5-4046-8888-5dbccdef3feb"
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODg4OC01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4";
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";
        var isValid = JwtService.ValidateJwt(jwt, secret);
        isValid.ShouldBeFalse();
    }
}