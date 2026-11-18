using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema.Api.Entities;

[Table("screenings")]
public sealed class Screening
{
    [Key, Column("id")] public int Id { get; set; }
    [Column("movie_id")] public int MovieId { get; set; }
    [Column("hall_id")] public int HallId { get; set; }
    [Column("starts_at_utc")] public DateTime StartsAtUtc { get; set; }
    [Column("price", TypeName = "numeric(10,2)")] public decimal Price { get; set; }
    public Movie Movie { get; set; } = null!;
    public Hall Hall { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

[Table("tickets")]
public sealed class Ticket
{
    [Key, Column("id")] public int Id { get; set; }
    [Column("screening_id")] public int ScreeningId { get; set; }
    [Column("seat_number")] public int SeatNumber { get; set; }
    [Required, MaxLength(100), Column("customer_name")] public string CustomerName { get; set; } = string.Empty;
    [Column("price", TypeName = "numeric(10,2)")] public decimal Price { get; set; }
    [Column("sold_at_utc")] public DateTime SoldAtUtc { get; set; }
    public Screening Screening { get; set; } = null!;
}
