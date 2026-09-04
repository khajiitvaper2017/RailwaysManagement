using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Newtonsoft.Json;
using RailwaysManagement.DbModels;
using RailwaysManagement.Models;
using Path = System.IO.Path;

namespace RailwaysManagement.Services;
[Table("Stations")]
public class OldStation
{
    [Key]
    [MaxLength(225)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Name { get; set; } = string.Empty;

    public int YardsNumber { get; set; }

    private int _maxCars;
    public int MaxCars
    {
        get
        {
            if (_maxCars == 0)
                _maxCars = YardsNumber * 10;
            return _maxCars;
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxCars), "Max cars cannot be negative");
            _maxCars = value;
        }
    }
    [JsonIgnore]
    public virtual ICollection<Train> Trains { get; set; } = new List<Train>();

    public Location Location { get; set; } = new Location();
}
public class GeoJsonService : IDisposable
{
    private JsonDocument _doc;

    public List<OldStation> Stations { get; } = new();
    private IHostEnvironment _env;

    public GeoJsonService(IHostEnvironment env)
    {
        _env = env;
        LoadStations();
    }

    public void LoadStationsFromJson()
    {
        var env = _env;
        var path = Path.Combine(env.ContentRootPath, "geojson", "export.geojson");
        var json = File.ReadAllText(path);
        _doc = JsonDocument.Parse(json);

        var featuresJsonElements = _doc.RootElement
            .GetProperty("features")
            .EnumerateArray()
            .ToList();

        var yardsElements = featuresJsonElements
            .Where(f =>
            {
                var p = f.GetProperty("properties");
                return (p.TryGetProperty("service", out var s)
                        && (s.GetString() == "yard" || s.GetString() == "siding"))
                       && (p.TryGetProperty("railway", out var r) && r.GetString() != "disused" && r.GetString() != "abandoned");
            })
            .ToList();

        const double radiusMeters = 400;
        const int threshold = 15;

        Stations.AddRange(featuresJsonElements
            .Where(f =>
            {
                var p = f.GetProperty("properties");
                if (!p.TryGetProperty("railway", out var r))
                    return false;
                string railwayValue = r.GetString() ?? string.Empty;
                if (railwayValue != "station" && railwayValue != "halt")
                    return false;
                return true;
            })
            .Select(f =>
            {
                int yardsCount = CountYardsForStation(f, yardsElements, radiusMeters);
                return new { f, yardsCount };
            })
            .Where(x => x.yardsCount >= threshold)
            .Select(x =>
            {
                var f = x.f;
                var p = f.GetProperty("properties");
                string name = p.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                string id = f.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
                var coords = ComputeCentroid(f);
                Debug.WriteLine($"Station {name} added with coords {coords.Lat}, {coords.Lon} and yards count {x.yardsCount}");
                return new OldStation
                {
                    Name = name,
                    Id = id,
                    YardsNumber = x.yardsCount,
                    Location = new Location(latitude: coords.Lat, longitude: coords.Lon, stationId: id)
                };
            })
            .ToList());

        SaveStations();
    }

    public void SaveStations()
    {
        // Save Stations as simple json

        var json = JsonConvert.SerializeObject(Stations, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture,
            NullValueHandling = NullValueHandling.Ignore
        });

        var env = _env;
        var path = Path.Combine(env.ContentRootPath, "geojson", "stations.json");
        File.WriteAllText(path, json);
    }

    public void LoadStations()
    {
        var env = _env;
        var path = Path.Combine(env.ContentRootPath, "geojson", "stations.json");
        if (!File.Exists(path))
        {
            LoadStationsFromJson();
            return;
        }
        var json = File.ReadAllText(path);
        Stations.Clear();
        var stations = JsonConvert.DeserializeObject<List<OldStation>>(json,
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Culture = CultureInfo.InvariantCulture,
                NullValueHandling = NullValueHandling.Ignore
            });

        if (stations == null || stations.Count == 0)
        {
            Debug.WriteLine("No stations found in the file");
            LoadStationsFromJson();
            return;
        }

        Stations.AddRange(stations);
    }

    public void Dispose()
    {
        SaveStations();
        _doc.Dispose();
    }

    private static (double Lat, double Lon) ComputeCentroid(JsonElement f)
    {
        var geom = f.GetProperty("geometry");
        var type = geom.GetProperty("type").GetString();
        if (type == "Point")
        {
            var c = geom.GetProperty("coordinates");
            return (c[1].GetDouble(), c[0].GetDouble());
        }

        var coords = geom.GetProperty("coordinates");
        double sumLat = 0, sumLon = 0;
        var cnt = 0;
        if (type == "LineString")
        {
            foreach (var pt in coords.EnumerateArray())
            {
                sumLon += pt[0].GetDouble();
                sumLat += pt[1].GetDouble();
                cnt++;
            }
        }
        else if (type == "Polygon")
        {
            foreach (var ring in coords.EnumerateArray())
                foreach (var pt in ring.EnumerateArray())
                {
                    sumLon += pt[0].GetDouble();
                    sumLat += pt[1].GetDouble();
                    cnt++;
                }
        }

        return (sumLat / cnt, sumLon / cnt);
    }

    private static int CountYardsForStation(JsonElement station, IReadOnlyList<JsonElement> yardElements, double radiusMeters)
    {
        var coords = ComputeCentroid(station);

        double stationLat = coords.Lat;
        double stationLon = coords.Lon;

        const double earthRadius = 6371000;
        double stationLatRad = stationLat * Math.PI / 180;
        double cosLat = Math.Cos(stationLatRad);
        int count = 0;
        double thresh2 = radiusMeters * radiusMeters;

        foreach (var yard in yardElements)
        {
            var yardCentroid = ComputeCentroid(yard);
            double dLat = (stationLat - yardCentroid.Lat) * Math.PI / 180;
            double dLon = (stationLon - yardCentroid.Lon) * Math.PI / 180 * cosLat;
            double x = earthRadius * dLon;
            double y = earthRadius * dLat;
            if (x * x + y * y <= thresh2)
            {
                count++;
            }
        }
        return count;
    }
}
