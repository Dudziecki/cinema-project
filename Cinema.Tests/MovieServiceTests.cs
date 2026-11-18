using Cinema.Api.Entities;
using Cinema.Api.Repositories;
using Cinema.Api.Services;
using Cinema.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cinema.Tests;

public sealed class MovieServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidMovie_ReturnsCreatedMovie()
    {
        var repository = new FakeMovieRepository();
        var service = new MovieService(repository, NullLogger<MovieService>.Instance);

        var result = await service.CreateAsync(new MovieUpsertDto
        {
            Title = "Новый фильм", DurationMinutes = 110, ReleaseYear = 2025, GenreIds = [1]
        }, CancellationToken.None);

        Assert.Equal("Новый фильм", result.Title);
        Assert.Equal("Драма", Assert.Single(result.Genres));
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
    {
        var service = new MovieService(new FakeMovieRepository(), NullLogger<MovieService>.Instance);
        var dto = new MovieUpsertDto { Title = "", DurationMinutes = 100, ReleaseYear = 2024 };
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_InvalidDuration_ThrowsValidationException()
    {
        var service = new MovieService(new FakeMovieRepository(), NullLogger<MovieService>.Instance);
        var dto = new MovieUpsertDto { Title = "Фильм", DurationMinutes = 0, ReleaseYear = 2024 };
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_MissingMovie_ReturnsFalse()
    {
        var service = new MovieService(new FakeMovieRepository(), NullLogger<MovieService>.Instance);
        var dto = new MovieUpsertDto { Title = "Фильм", DurationMinutes = 100, ReleaseYear = 2024 };
        var result = await service.UpdateAsync(999, dto, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingMovieWithoutScreenings_ReturnsTrue()
    {
        var repository = new FakeMovieRepository();
        repository.Items.Add(new Movie { Id = 1, Title = "Тест", DurationMinutes = 90, ReleaseYear = 2020 });
        var service = new MovieService(repository, NullLogger<MovieService>.Instance);
        Assert.True(await service.DeleteAsync(1, CancellationToken.None));
    }

    private sealed class FakeMovieRepository : IMovieRepository
    {
        public List<Movie> Items { get; } = [];
        private readonly List<Genre> _genres = [new Genre { Id = 1, Name = "Драма" }];

        public Task<List<Movie>> GetAllAsync(string? search, int page, int pageSize, string? sortBy, bool descending, CancellationToken cancellationToken)
        {
            IEnumerable<Movie> query = Items;
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(query.Skip((page - 1) * pageSize).Take(pageSize).ToList());
        }

        public Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken) => Task.FromResult(_genres);

        public Task<Movie> AddAsync(Movie movie, IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken)
        {
            movie.Id = Items.Count + 1;
            movie.MovieGenres = genreIds.Select(id =>
            {
                var genre = _genres.Single(x => x.Id == id);
                return new MovieGenre { MovieId = movie.Id, GenreId = id, Genre = genre };
            }).ToList();
            Items.Add(movie);
            return Task.FromResult(movie);
        }

        public Task<bool> UpdateAsync(int id, Movie movie, IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken)
        {
            var existing = Items.FirstOrDefault(x => x.Id == id);
            if (existing is null) return Task.FromResult(false);
            existing.Title = movie.Title;
            existing.DurationMinutes = movie.DurationMinutes;
            existing.ReleaseYear = movie.ReleaseYear;
            existing.Description = movie.Description;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.RemoveAll(x => x.Id == id) > 0);

        public Task<int> DeleteOldWithoutScreeningsAsync(int beforeYear, CancellationToken cancellationToken)
        {
            var count = Items.RemoveAll(x => x.ReleaseYear < beforeYear && x.Screenings.Count == 0);
            return Task.FromResult(count);
        }

        public Task<bool> SetReleaseYearAsync(int id, int releaseYear, CancellationToken cancellationToken)
        {
            var movie = Items.FirstOrDefault(x => x.Id == id);
            if (movie is null) return Task.FromResult(false);
            movie.ReleaseYear = releaseYear;
            return Task.FromResult(true);
        }
    }
}
