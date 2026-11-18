using Cinema.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(CinemaDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Genres.AnyAsync(cancellationToken))
        {
            db.Genres.AddRange(
                new Genre { Name = "Драма" },
                new Genre { Name = "Фантастика" },
                new Genre { Name = "Комедия" },
                new Genre { Name = "Приключения" });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Movies.AnyAsync(cancellationToken))
        {
            var drama = await db.Genres.FirstAsync(x => x.Name == "Драма", cancellationToken);
            var sciFi = await db.Genres.FirstAsync(x => x.Name == "Фантастика", cancellationToken);
            var adventure = await db.Genres.FirstAsync(x => x.Name == "Приключения", cancellationToken);
            db.Movies.AddRange(
                new Movie
                {
                    Title = "Интерстеллар", DurationMinutes = 169, ReleaseYear = 2014,
                    Description = "Научно-фантастический фильм.",
                    MovieGenres = [new MovieGenre { GenreId = drama.Id }, new MovieGenre { GenreId = sciFi.Id }]
                },
                new Movie
                {
                    Title = "Парк юрского периода", DurationMinutes = 127, ReleaseYear = 1993,
                    Description = "Приключенческий фильм.",
                    MovieGenres = [new MovieGenre { GenreId = adventure.Id }]
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Halls.AnyAsync(cancellationToken))
        {
            db.Halls.AddRange(
                new Hall
                {
                    Name = "Зал 1", Capacity = 80,
                    ProjectionSystem = new ProjectionSystem { Model = "Cinema Projector X1", InstalledAtUtc = DateTime.UtcNow }
                },
                new Hall
                {
                    Name = "Зал 2", Capacity = 50,
                    ProjectionSystem = new ProjectionSystem { Model = "Cinema Projector X2", InstalledAtUtc = DateTime.UtcNow }
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Screenings.AnyAsync(cancellationToken))
        {
            var movie = await db.Movies.OrderBy(x => x.Id).FirstAsync(cancellationToken);
            var hall = await db.Halls.OrderBy(x => x.Id).FirstAsync(cancellationToken);
            db.Screenings.Add(new Screening
            {
                MovieId = movie.Id,
                HallId = hall.Id,
                StartsAtUtc = DateTime.UtcNow.AddDays(1),
                Price = 12.50m
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            var hasher = new PasswordHasher<AppUser>();
            var admin = new AppUser { Username = "admin", Role = "Admin" };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
            var cashier = new AppUser { Username = "cashier", Role = "Cashier" };
            cashier.PasswordHash = hasher.HashPassword(cashier, "Cashier123!");
            db.Users.AddRange(admin, cashier);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
