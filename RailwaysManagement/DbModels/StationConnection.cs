using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.DbModels;

[Table("StationConnections")]
public class StationConnection : ISoftDeletable
{
    [MaxLength(225)]
    public string FromStationId { get; set; }

    [ForeignKey(nameof(FromStationId))] public Station FromStation { get; set; }

    [MaxLength(225)]
    public string ToStationId { get; set; }

    [ForeignKey(nameof(ToStationId))] public Station ToStation { get; set; }

    [Range(0.0, double.MaxValue)] public double Distance { get; set; }

    public bool IsDeleted { get; set; }
}