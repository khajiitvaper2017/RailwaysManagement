namespace RailwaysManagement.Models.Actions;

public interface IRouteAction
{
    public string Type { get; set; }
    public string Description { get; }
    public DateTime? Time { get; set; }
    public ICollection<Location> Locations { get; set; }
}