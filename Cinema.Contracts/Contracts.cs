using System.ComponentModel.DataAnnotations;

namespace Cinema.Contracts;

public sealed record LoginRequest(
    [property: Required, MinLength(3), MaxLength(50)] string Username,
    [property: Required, MinLength(6), MaxLength(100)] string Password);

public sealed record LoginResponse(string AccessToken, string Username, string Role);

public sealed record GenreDto(int Id, string Name);

public sealed record MovieDto(
    int Id,
    string Title,
    int DurationMinutes,
    int ReleaseYear,
    string? Description,
    IReadOnlyList<int> GenreIds,
    IReadOnlyList<string> Genres);

public sealed class MovieUpsertDto
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 500)]
    public int DurationMinutes { get; set; }

    [Range(1888, 2100)]
    public int ReleaseYear { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public List<int> GenreIds { get; set; } = [];
}

public sealed record ScreeningDto(
    int Id,
    int MovieId,
    string MovieTitle,
    int HallId,
    string HallName,
    int HallCapacity,
    DateTime StartsAtUtc,
    decimal Price);

public sealed class ScreeningCreateDto
{
    [Range(1, int.MaxValue)] public int MovieId { get; set; }
    [Range(1, int.MaxValue)] public int HallId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    [Range(typeof(decimal), "0.01", "10000")] public decimal Price { get; set; }
}

public sealed class TicketCreateDto
{
    [Range(1, int.MaxValue)] public int ScreeningId { get; set; }
    [Range(1, int.MaxValue)] public int SeatNumber { get; set; }
    [Required, StringLength(100, MinimumLength = 2)] public string CustomerName { get; set; } = string.Empty;
}

public sealed record TicketDto(
    int Id,
    int ScreeningId,
    int SeatNumber,
    string CustomerName,
    decimal Price,
    DateTime SoldAtUtc);
