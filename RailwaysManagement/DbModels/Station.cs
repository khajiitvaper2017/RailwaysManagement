using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using RailwaysManagement.Models;

namespace RailwaysManagement.DbModels;

[Table("Stations")]
public class Station : ISoftDeletable, IAuditable
{
    private int _maxCars;

    [Key] [MaxLength(225)] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required] [MaxLength(150)] public string Name { get; set; }

    public int YardsNumber { get; set; }

    public int MaxCars
    {
        get => _maxCars == 0 ? YardsNumber * 10 : _maxCars;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxCars), "Max cars cannot be negative");
            _maxCars = value;
        }
    }


    public Location Location { get; set; } = new();

    [InverseProperty(nameof(StationConnection.FromStation))]
    [JsonIgnore]
    public ICollection<StationConnection> OutgoingConnections { get; set; } = new List<StationConnection>();

    [InverseProperty(nameof(StationConnection.ToStation))]
    [JsonIgnore]
    public ICollection<StationConnection> IncomingConnections { get; set; } = new List<StationConnection>();

    [JsonIgnore]
    public ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();

    [InverseProperty(nameof(Train.Station))]
    [JsonIgnore]
    public ICollection<Train> Trains { get; set; } = new List<Train>();

    [JsonIgnore]
    public ICollection<RoutePartStation> RouteStations { get; set; } = new List<RoutePartStation>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
}