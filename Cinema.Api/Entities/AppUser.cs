using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cinema.Api.Entities;

[Table("app_users")]
public sealed class AppUser
{
    [Key, Column("id")] public int Id { get; set; }
    [Required, MaxLength(50), Column("username")] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(500), Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(20), Column("role")] public string Role { get; set; } = "Cashier";
}
