using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.Models.Actions;

[ComplexType]
public class DirectReturnToBaseAction : IRouteAction
{
    public string Type { get; set; } = "DirectReturnToBase";
    public string StationId { get; set; }
    public string StationName { get; set; }
    public string TrainId { get; set; }
    public string TrainName { get; set; }
    public DateTime? Time { get; set; }
    public string Description => $"Пряме повернення потяга {TrainName} " +
                                  $"на станцію {StationName}" +
                                  $"{(Time.HasValue ? $" о {Time:HH:mm:ss}" : "")}";
    public ICollection<Location> Locations { get; set; }
}