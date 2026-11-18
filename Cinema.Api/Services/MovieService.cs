using System.ComponentModel.DataAnnotations;
using Cinema.Api.Entities;
using Cinema.Api.Repositories;
using Cinema.Contracts;

namespace Cinema.Api.Services;

public sealed class MovieService(IMovieRepository repository, ILogger<MovieService> logger)
{
    public async Task<IReadOnlyList<MovieDto>> GetAllAsync(string? search, int page, int pageSize, string? sortBy, bool descending, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var movies = await repository.GetAllAsync(search, page, pageSize, sortBy, descending, cancellationToken);
        return movies.Select(Map).ToList();
    }

    public async Task<MovieDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var movie = await repository.GetByIdAsync(id, cancellationToken);
        return movie is null ? null : Map(movie);
    }

    public async Task<MovieDto> CreateAsync(MovieUpsertDto dto, CancellationToken cancellationToken)
    {
        Validate(dto);
        var movie = new Movie
        {
            Title = dto.Title.Trim(),
            DurationMinutes = dto.DurationMinutes,
            ReleaseYear = dto.ReleaseYear,
            Description = dto.Description?.Trim()
        };
        var saved = await repository.AddAsync(movie, dto.GenreIds, cancellationToken);
        logger.LogInformation("Created movie {MovieId}: {Title}", saved.Id, saved.Title);
        return Map(saved);
    }

    public async Task<bool> UpdateAsync(int id, MovieUpsertDto dto, CancellationToken cancellationToken)
    {
        Validate(dto);
        return await repository.UpdateAsync(id, new Movie
        {
            Id = id,
            Title = dto.Title.Trim(),
            DurationMinutes = dto.DurationMinutes,
            ReleaseYear = dto.ReleaseYear,
            Description = dto.Description?.Trim()
        }, dto.GenreIds, cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    public Task<int> DeleteOldWithoutScreeningsAsync(int beforeYear, CancellationToken cancellationToken) =>
        repository.DeleteOldWithoutScreeningsAsync(beforeYear, cancellationToken);

    public async Task<bool> SetReleaseYearAsync(int id, int year, CancellationToken cancellationToken)
    {
        if (year < 1888 || year > 2100)
            throw new ValidationException("Год выпуска должен находиться в диапазоне 1888–2100.");
        return await repository.SetReleaseYearAsync(id, year, cancellationToken);
    }

    private static void Validate(MovieUpsertDto dto)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true))
            throw new ValidationException(string.Join("; ", results.Select(x => x.ErrorMessage)));
    }

    private static MovieDto Map(Movie movie) => new(
        movie.Id,
        movie.Title,
        movie.DurationMinutes,
        movie.ReleaseYear,
        movie.Description,
        movie.MovieGenres.Select(x => x.GenreId).ToArray(),
        movie.MovieGenres.Select(x => x.Genre.Name).ToArray());
}
