using System.ComponentModel.DataAnnotations.Schema;
using RailwaysManagement.Models;

namespace RailwaysManagement.Models.Actions;

[ComplexType]
public class LoadCargoAction : IRouteAction
{
    public string Type { get; set; } = "LoadCargo";
    public string CargoId { get; set; }
    public string CargoName { get; set; }
    public string TrainId { get; set; }
    public string TrainName { get; set; }
    public DateTime? Time { get; set; }
    public string Description => $"Завантаження вантажу {CargoName}" +
                                 $" на потяг {TrainName} " +
                                 (Time.HasValue ? $"о {Time:HH:mm:ss}" : "");
    public ICollection<Location> Locations { get; set; }
}