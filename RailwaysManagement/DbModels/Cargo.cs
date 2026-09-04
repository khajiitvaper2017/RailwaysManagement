using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace RailwaysManagement.DbModels;

[Table("Cargos")]
public class Cargo : ISoftDeletable, IAuditable
{
    [Key] [MaxLength(225)] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required][MaxLength(150)] public string Name { get; set; } = "Вантаж";

    [Range(1, int.MaxValue)] public int WagonsCount { get; set; } = 1;

    [JsonIgnore]
    public ICollection<RoutePartCargo> RoutePartCargos { get; set; } = new List<RoutePartCargo>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    [MaxLength(225)] public string? RouteRequestId { get; set; }

    [ForeignKey(nameof(RouteRequestId))] public RouteRequest? RouteRequest { get; set; }
}