namespace Cinema.Api.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string? Key { get; set; }
    public string Issuer { get; set; } = "CinemaApi";
    public string Audience { get; set; } = "CinemaClient";
}
