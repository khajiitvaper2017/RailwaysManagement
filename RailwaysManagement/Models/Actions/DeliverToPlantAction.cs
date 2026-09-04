using System.ComponentModel.DataAnnotations.Schema;

namespace RailwaysManagement.Models.Actions;

[ComplexType]
public class DeliverToPlantAction : IRouteAction
{
    public string Type { get; set; } = "DeliverToPlant";
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string CargoId { get; set; }
    public string CargoName { get; set; }
    public string TrainId { get; set; }
    public string TrainName { get; set; }
    public DateTime? Time { get; set; }
    public string Description => $"Доставка вантажу {CargoName} клієнту {ClientName} потягом {TrainName} " +
                                  $"{(Time.HasValue ? $"о {Time:HH:mm:ss}" : "")}";
    public ICollection<Location> Locations { get; set; }
}