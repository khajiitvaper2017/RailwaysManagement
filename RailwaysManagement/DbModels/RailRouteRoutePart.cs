using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.DbModels;

[Table("RailRouteRouteParts")]
public class RailRouteRoutePart
{
    [MaxLength(225)]
    public string RailRouteId { get; set; }
    public RailRoute RailRoute { get; set; }

    [MaxLength(225)]
    public string RoutePartId { get; set; }
    public RoutePart RoutePart { get; set; }

    public int OrderInRailRoute { get; set; }
}