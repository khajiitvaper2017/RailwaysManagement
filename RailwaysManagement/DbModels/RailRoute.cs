using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using RailwaysManagement.Models;

namespace RailwaysManagement.DbModels;

[Table("Routes")]
public class RailRoute : ISoftDeletable, IAuditable
{
    [Key][MaxLength(225)] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required][MaxLength(150)] public string Name { get; set; }

    [JsonIgnore]
    public ICollection<RailRouteRoutePart> RailRouteRouteParts { get; set; } = new List<RailRouteRoutePart>();
    [MaxLength(225)]
    public string? RouteRequestId { get; set; }
    [ForeignKey(nameof(RouteRequestId))] public RouteRequest? RouteRequest { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}