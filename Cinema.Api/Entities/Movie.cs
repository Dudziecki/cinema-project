using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema.Api.Entities;

[Table("movies")]
public sealed class Movie
{
    [Key, Column("id")] public int Id { get; set; }
    [Required, MaxLength(150), Column("title")] public string Title { get; set; } = string.Empty;
    [Range(1, 500), Column("duration_minutes")] public int DurationMinutes { get; set; }
    [Column("release_year")] public int ReleaseYear { get; set; }
    [MaxLength(1000), Column("description")] public string? Description { get; set; }
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
}

[Table("genres")]
public sealed class Genre
{
    [Key, Column("id")] public int Id { get; set; }
    [Required, MaxLength(60), Column("name")] public string Name { get; set; } = string.Empty;
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}

[Table("movie_genres")]
public sealed class MovieGenre
{
    [Column("movie_id")] public int MovieId { get; set; }
    [Column("genre_id")] public int GenreId { get; set; }
    public Movie Movie { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
