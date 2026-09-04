namespace RailwaysManagement.Models.Actions;

public class ActionResult : IRouteAction
{
    public string Type { get; set; }
    public string Description { get; set; }
    public DateTime? Time { get; set; } = null;
    public ICollection<Location> Locations { get; set; }
    public ActionResult(string type, string description, ICollection<Location> locations, DateTime? time = null)
    {
        Type = type;
        Description = description;
        Locations = locations;
        Time = time;
    }

    public ActionResult()
    {
        Type = "ActionResult";
        Description = "";
        Locations = new List<Location>();
    }
}