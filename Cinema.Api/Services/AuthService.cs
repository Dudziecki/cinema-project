using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cinema.Api.Data;
using Cinema.Api.Entities;
using Cinema.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Cinema.Api.Services;

public sealed class AuthService(
    CinemaDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    IOptions<JwtOptions> jwtOptions)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == request.Username, cancellationToken);
        if (user is null) return null;

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed) return null;

        var settings = jwtOptions.Value;
        var key = settings.Key ?? throw new InvalidOperationException("Не задан Jwt:Key.");
        var issuer = settings.Issuer;
        var audience = settings.Audience;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer, audience, claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: credentials);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username, user.Role);
    }
}
