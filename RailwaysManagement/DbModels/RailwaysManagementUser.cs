using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using RailwaysManagement.Models;

namespace RailwaysManagement.DbModels;

[Table("RailwaysManagementUsers")]
public class RailwaysManagementUser : IdentityUser, ISoftDeletable, IAuditable
{
    [Required] [MaxLength(100)] public string Name { get; set; }
    [Required] public UserRole Role { get; set; }
    public Location? Location { get; set; }
    [MaxLength(225)] public string? AssignedStationId { get; set; }
    [ForeignKey(nameof(AssignedStationId))] public Station? AssignedStation { get; set; }
    [JsonIgnore]
    public ICollection<RouteRequest> RouteRequests { get; set; } = new List<RouteRequest>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; }
}