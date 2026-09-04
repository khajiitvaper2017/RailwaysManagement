using RailwaysManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using System.Collections.Generic; // Required for ICollection

namespace RailwaysManagement.DbModels;

[Table("RouteParts")]
public class RoutePart
{
    [Key][MaxLength(225)] public string Id { get; set; } = Guid.NewGuid().ToString();

    // Properties moved from RailRoute
    public PlannedRoute PlannedRoute { get; set; } = new();

    [JsonIgnore]
    public ICollection<RoutePartStation> RouteStations { get; set; } = new List<RoutePartStation>();

    [JsonIgnore]
    public ICollection<RoutePartCargo> RoutePartCargos { get; set; } = new List<RoutePartCargo>();

    // Train relationship
    [MaxLength(225)]
    public string? TrainId { get; set; }
    [ForeignKey(nameof(TrainId))] public Train? Train { get; set; }

    // Navigation property for many-to-many relationship with RailRoute
    [JsonIgnore]
    public ICollection<RailRouteRoutePart> RailRouteRouteParts { get; set; } = new List<RailRouteRoutePart>();
}