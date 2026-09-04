using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.DbModels;

[Table("RoutePartCargo")]
public class RoutePartCargo
{
    [MaxLength(225)]
    public string RoutePartId { get; set; }

    [ForeignKey(nameof(RoutePartId))] public RoutePart? RoutePart { get; set; }

    [MaxLength(225)]
    public string CargoId { get; set; }

    [ForeignKey(nameof(CargoId))] public Cargo Cargo { get; set; }

    [Range(0, int.MaxValue)] public int Order { get; set; }
}
