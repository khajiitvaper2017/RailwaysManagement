using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace RailwaysManagement.DbModels;

[Table("Trains")]
public class Train : ISoftDeletable, IAuditable
{
    [Key] [MaxLength(225)] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required] [MaxLength(100)] public string Name { get; set; } = "Локомотив";

    public int MaxCarsCount { get; set; } = 50;

    [ForeignKey(nameof(Station))]
    [MaxLength(225)]
    public string StationId { get; set; }

    [InverseProperty(nameof(Station.Trains))]
    public Station Station { get; set; }
    [JsonIgnore]
    public ICollection<RoutePart> RouteParts { get; set; } = new List<RoutePart>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    
    public bool IsAvailable
    {
        get
        {
            // if all route part's route's routerequest are completed, the train is available
            return RouteParts.All(rp =>
                rp.RailRouteRouteParts.All(rrrp => rrrp.RailRoute.RouteRequest?.IsCompleted == true));

        }
    }
}