using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domora.Application.Common.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;



namespace Domora.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; }
}

public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtAccessTokenGenerator(
        IOptions<JwtOptions> options
    )
    {
        _options = options.Value;
    }

    public string Generate(
        Guid userId
    )
    {
        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()   
            )
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _options.ExpirationMinutes
            ),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}