using Cinema.Api.Data;
using Cinema.Api.Entities;
using Cinema.Api.Services;
using Cinema.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapCinemaEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
        {
            var errors = EndpointValidation.Validate(request);
            if (errors.Count > 0) return (IResult)Results.ValidationProblem(errors);
            var response = await auth.LoginAsync(request, ct);
            return response is null ? (IResult)Results.Unauthorized() : Results.Ok(response);
        });

        app.MapGet("/api/genres", async (CinemaDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Genres.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new GenreDto(x.Id, x.Name)).ToListAsync(ct)));

        var movies = app.MapGroup("/api/movies");
        movies.MapGet("", async (MovieService service, string? search, int? page, int? pageSize, string? sortBy, bool? descending, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(search, page ?? 1, pageSize ?? 20, sortBy, descending ?? false, ct)));
        movies.MapGet("/{id:int}", async (int id, MovieService service, CancellationToken ct) =>
        {
            var movie = await service.GetByIdAsync(id, ct);
            return movie is null ? (IResult)Results.NotFound() : Results.Ok(movie);
        });
        movies.MapPost("", async (MovieUpsertDto dto, MovieService service, CancellationToken ct) =>
        {
            var errors = EndpointValidation.Validate(dto);
            if (errors.Count > 0) return (IResult)Results.ValidationProblem(errors);
            try
            {
                var created = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/movies/{created.Id}", created);
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(new { message = ex.Message }); }
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        movies.MapPut("/{id:int}", async (int id, MovieUpsertDto dto, MovieService service, CancellationToken ct) =>
        {
            var errors = EndpointValidation.Validate(dto);
            if (errors.Count > 0) return (IResult)Results.ValidationProblem(errors);
            try
            {
                var updated = await service.UpdateAsync(id, dto, ct);
                return updated ? (IResult)Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(new { message = ex.Message }); }
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        movies.MapDelete("/{id:int}", async (int id, MovieService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? (IResult)Results.NoContent() : Results.Conflict(new { message = "Фильм не найден или у него уже есть сеансы." });
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        movies.MapDelete("/bulk-old", async (int beforeYear, MovieService service, CancellationToken ct) =>
        {
            if (beforeYear < 1888 || beforeYear > 2100)
                return (IResult)Results.BadRequest(new { message = "Год должен находиться в диапазоне 1888–2100." });
            var deletedCount = await service.DeleteOldWithoutScreeningsAsync(beforeYear, ct);
            return Results.Ok(new { deletedCount });
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        movies.MapPatch("/{id:int}/release-year", async (int id, int year, MovieService service, CancellationToken ct) =>
        {
            try
            {
                var updated = await service.SetReleaseYearAsync(id, year, ct);
                return updated ? (IResult)Results.NoContent() : Results.NotFound();
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException ex)
            {
                return (IResult)Results.BadRequest(new { message = ex.Message });
            }
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        var screenings = app.MapGroup("/api/screenings");
        screenings.MapGet("", async (CinemaDbContext db, CancellationToken ct) =>
        {
            var result = await db.Screenings.AsNoTracking()
                .Include(x => x.Movie).Include(x => x.Hall)
                .OrderBy(x => x.StartsAtUtc)
                .Select(x => new ScreeningDto(
                    x.Id, x.MovieId, x.Movie.Title, x.HallId, x.Hall.Name,
                    x.Hall.Capacity, x.StartsAtUtc, x.Price))
                .ToListAsync(ct);
            return Results.Ok(result);
        });
        screenings.MapPost("", async (ScreeningCreateDto dto, CinemaDbContext db, CancellationToken ct) =>
        {
            var errors = EndpointValidation.Validate(dto);
            if (dto.StartsAtUtc <= DateTime.UtcNow)
                errors[nameof(dto.StartsAtUtc)] = ["Дата и время сеанса должны быть в будущем (UTC)."];
            if (errors.Count > 0) return (IResult)Results.ValidationProblem(errors);
            var movieExists = await db.Movies.AnyAsync(x => x.Id == dto.MovieId, ct);
            var hallExists = await db.Halls.AnyAsync(x => x.Id == dto.HallId && x.IsActive, ct);
            if (!movieExists || !hallExists)
                return (IResult)Results.BadRequest(new { message = "Указанный фильм или активный зал не найдены." });
            var screening = new Screening
            {
                MovieId = dto.MovieId,
                HallId = dto.HallId,
                StartsAtUtc = DateTime.SpecifyKind(dto.StartsAtUtc, DateTimeKind.Utc),
                Price = dto.Price
            };
            db.Screenings.Add(screening);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/screenings/{screening.Id}", new { screening.Id });
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        var tickets = app.MapGroup("/api/tickets").RequireAuthorization(policy => policy.RequireRole("Admin", "Cashier"));
        tickets.MapPost("", async (TicketCreateDto dto, AdoNetTicketRepository repository, CancellationToken ct) =>
        {
            var errors = EndpointValidation.Validate(dto);
            if (errors.Count > 0) return (IResult)Results.ValidationProblem(errors);
            try { return Results.Created("/api/tickets", await repository.SellTicketAsync(dto, ct)); }
            catch (InvalidOperationException ex) { return (IResult)Results.BadRequest(new { message = ex.Message }); }
        });
        tickets.MapGet("", async (CinemaDbContext db, CancellationToken ct) =>
        {
            var result = await db.Tickets.AsNoTracking()
                .OrderByDescending(x => x.SoldAtUtc)
                .Select(x => new TicketDto(x.Id, x.ScreeningId, x.SeatNumber, x.CustomerName, x.Price, x.SoldAtUtc))
                .Take(100).ToListAsync(ct);
            return Results.Ok(result);
        });

        app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
    }
}
