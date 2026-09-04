using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.Models.Actions;

[ComplexType]
public class PickupFromPlantAction : IRouteAction
{
    public string Type { get; set; } = "PickupFromPlant";
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string CargoId { get; set; }
    public string CargoName { get; set; }
    public string TrainId { get; set; }
    public string TrainName { get; set; }
    public DateTime? Time { get; set; }

    public string Description => $"«аб≥р вантажу {CargoName} у кл≥Їнта {ClientName} " +
                                 $"на пот€г {TrainName} " +
                                 (Time.HasValue ? $"о {Time:HH:mm:ss}" : "");

    public ICollection<Location> Locations { get; set; }
}