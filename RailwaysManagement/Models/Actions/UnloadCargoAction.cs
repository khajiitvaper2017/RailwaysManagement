using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.Models.Actions;

[ComplexType]
public class UnloadCargoAction : IRouteAction
{
    public string Type { get; set; } = "UnloadCargo";
    public string CargoId { get; set; }
    public string CargoName { get; set; }
    public string TrainId { get; set; }
    public string TrainName { get; set; }
    public DateTime? Time { get; set; }
    public string Description => $"Розвантаження вантажу {CargoName}" +
                                  $" з потяга {TrainName}" +
                                  (Time.HasValue ? $" о {Time:HH:mm:ss}" : "");
    public ICollection<Location> Locations { get; set; }
}