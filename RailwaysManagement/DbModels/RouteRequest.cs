using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace RailwaysManagement.DbModels;

[Table("RouteRequests")]
public class RouteRequest : ISoftDeletable, IAuditable
{
    [Key] [MaxLength(225)] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] [MaxLength(225)] public string SenderClientId { get; set; }
    [ForeignKey(nameof(SenderClientId))] public RailwaysManagementUser SenderClient { get; set; }

    [Required] [MaxLength(225)] public string ReceiverClientId { get; set; }
    [ForeignKey(nameof(ReceiverClientId))] public RailwaysManagementUser ReceiverClient { get; set; }
    [MaxLength(225)] public string? RouteId { get; set; }
    [ForeignKey(nameof(RouteId))] public RailRoute? Route { get; set; }
    [Required] public DateTime DeliveryDeadlineDate { get; set; }
    [Required] public DateTime? ShipmentDate { get; set; }
    [Required] public bool IsPlanned { get; set; }
    [Required] public bool IsCompleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    [InverseProperty(nameof(Cargo.RouteRequest))]
    [JsonIgnore]
    public ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();
}