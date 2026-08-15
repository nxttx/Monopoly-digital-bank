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
        jwt.ShouldBe("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODI2OS01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4");
    }
    
    [Fact]
    public void ShouldValidateJwt()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODI2OS01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4";
        var secret = "ThisIsAVerySafeSecret_q2o3789pbyrp";
        var isValid = JwtService.ValidateJwt(jwt, secret);
        isValid.ShouldBeTrue();
    }
    
    [Fact]
    public void ShouldNotValidateJwtWithInvalidSecret()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJHYW1lSWQiOiI3NzNjMzlkZS1jOWM1LTQwNDYtODI2OS01ZGJjY2RlZjNmZWIiLCJVc2VySWQiOiIyOTFmYzg0ZC01ODUzLTQzNzQtYmIyNi0zYmQ3NmI5MmY0OWUifQ.vif14PFwggDLgnSsLYdgGdwA3OuilaCUYvbcxkfInc4";
        var secret = "ThisIsAVerySafeSecret";
        var isValid = JwtService.ValidateJwt(jwt, secret);
        isValid.ShouldBeFalse();
    }
}