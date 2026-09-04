using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.Models.Actions;

[ComplexType]
public class StationDepartureAction : IRouteAction
{
    public string Type { get; set; } = "StationDeparture";
    public string StationId { get; set; }
    public string StationName { get; set; }
    public string TrainId { get; set; }
    public string TrainName { get; set; }
    public DateTime? Time { get; set; }
    public string Description => $"Відправлення потяга {TrainName} " +
                                  $"з станції {StationName}" +
                                  $"{(Time.HasValue ? $" о {Time:HH:mm:ss}" : "")}";
    public ICollection<Location> Locations { get; set; }
}