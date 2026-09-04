using System.ComponentModel.DataAnnotations.Schema;
using RailwaysManagement.Models.Actions;

namespace RailwaysManagement.Models;

[ComplexType]
public class PlannedRoute
{
    public ICollection<IRouteAction> Actions { get; set; }
}