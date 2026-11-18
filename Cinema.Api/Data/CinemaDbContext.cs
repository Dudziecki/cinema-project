using Cinema.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Api.Data;

public sealed class CinemaDbContext(DbContextOptions<CinemaDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<ProjectionSystem> ProjectionSystems => Set<ProjectionSystem>();
    public DbSet<Screening> Screenings => Set<Screening>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Genre>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();

        modelBuilder.Entity<MovieGenre>().HasKey(x => new { x.MovieId, x.GenreId });
        modelBuilder.Entity<MovieGenre>()
            .HasOne(x => x.Movie).WithMany(x => x.MovieGenres)
            .HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MovieGenre>()
            .HasOne(x => x.Genre).WithMany(x => x.MovieGenres)
            .HasForeignKey(x => x.GenreId).OnDelete(DeleteBehavior.Cascade);

        // One-to-one: one hall has one projection system.
        modelBuilder.Entity<ProjectionSystem>()
            .HasOne(x => x.Hall).WithOne(x => x.ProjectionSystem)
            .HasForeignKey<ProjectionSystem>(x => x.HallId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Movie -> Screenings and Hall -> Screenings.
        modelBuilder.Entity<Screening>()
            .HasOne(x => x.Movie).WithMany(x => x.Screenings)
            .HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Screening>()
            .HasOne(x => x.Hall).WithMany(x => x.Screenings)
            .HasForeignKey(x => x.HallId).OnDelete(DeleteBehavior.Restrict);

        // One-to-many: Screening -> Tickets.
        modelBuilder.Entity<Ticket>()
            .HasOne(x => x.Screening).WithMany(x => x.Tickets)
            .HasForeignKey(x => x.ScreeningId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Ticket>().HasIndex(x => new { x.ScreeningId, x.SeatNumber }).IsUnique();
    }
}
