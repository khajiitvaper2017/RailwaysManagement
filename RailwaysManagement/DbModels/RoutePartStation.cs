using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.DbModels;

[Table("RoutePartStations")]
public class RoutePartStation
{
    [Key]
    public int Id { get; set; }

    [MaxLength(225)]
    public string RoutePartId { get; set; }
    [ForeignKey(nameof(RoutePartId))] public RoutePart RoutePart { get; set; }

    [MaxLength(225)]
    public string StationId { get; set; }
    [ForeignKey(nameof(StationId))] public Station? Station { get; set; }

    [Range(0, int.MaxValue)] public int Order { get; set; }
    public DateTime? ExpectedArrival { get; set; }
    public DateTime? ExpectedDeparture { get; set; }
}