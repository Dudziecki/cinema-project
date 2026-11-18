using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema.Api.Entities;

[Table("halls")]
public sealed class Hall
{
    [Key, Column("id")] public int Id { get; set; }
    [Required, MaxLength(80), Column("name")] public string Name { get; set; } = string.Empty;
    [Range(1, 1000), Column("capacity")] public int Capacity { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    public ProjectionSystem? ProjectionSystem { get; set; }
    public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
}

[Table("projection_systems")]
public sealed class ProjectionSystem
{
    [Key, Column("id")] public int Id { get; set; }
    [Column("hall_id")] public int HallId { get; set; }
    [Required, MaxLength(100), Column("model")] public string Model { get; set; } = string.Empty;
    [Column("installed_at_utc")] public DateTime InstalledAtUtc { get; set; }
    public Hall Hall { get; set; } = null!;
}
