using System.Globalization;
using System.Text.Json;
using IO.Swagger.Api;
using PolylineEncoder.Net.Models;

namespace RailwaysManagement.Services;

public class OpenRailRoutingResult
{
    public IEnumerable<IGeoCoordinate> Coordinates { get; set; } = new List<IGeoCoordinate>();
    public double Distance { get; set; } = 0.0;
    public TimeOnly Time { get; set; } = new(0, 0, 0);
}
public class OpenRailRoutingService()
{
    private readonly RoutingApi _routingApi = new("https://routing.openrailrouting.org/");
    public OpenRailRoutingResult LastResult { get; private set; } = new();

    public async Task<OpenRailRoutingResult> FindRouteAsync(params JsonElement[] points)
    {
        var route = new List<string>();
        foreach (var point in points)
        {
            var coords = point.GetProperty("geometry").GetProperty("coordinates");
            var lon = coords[0].GetDouble();
            var lat = coords[1].GetDouble();
            var str = $"{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}";
            route.Add(str);
        }

        return await FindRouteAsync(route);
    }

    public async Task<OpenRailRoutingResult> FindRouteAsync(string start, string end)
    {
        return await FindRouteAsync(new List<string>() { start, end });
    }

    public async Task<OpenRailRoutingResult> FindRouteAsync(params string[] points)
    {
        return await FindRouteAsync(points.ToList());
    }

    public async Task<OpenRailRoutingResult> FindRouteAsync(IEnumerable<IGeoCoordinate> points)
    {
        var stringPoints = points.Select(p =>
                $"{p.Latitude.ToString(CultureInfo.InvariantCulture)},{p.Longitude.ToString(CultureInfo.InvariantCulture)}")
            .ToList();

        return await FindRouteAsync(stringPoints);
    }

    public async Task<OpenRailRoutingResult> FindRouteAsync(params IGeoCoordinate?[] points)
    {
        var stringPoints = points.Where(o => o != null).Select(p =>
                $"{p.Latitude.ToString(CultureInfo.InvariantCulture)},{p.Longitude.ToString(CultureInfo.InvariantCulture)}")
            .ToList();

        return await FindRouteAsync(stringPoints);
    }

    public async Task<OpenRailRoutingResult> FindRouteAsync(List<string> points)
    {
        Console.WriteLine($"Finding route for points: {string.Join(", ", points)}");
        var result = _routingApi.GetRoute(points, locale: "uk-UA", elevation: false, profile: "all_tracks", pointsEncoded: false);
        var jsonDocument = JsonDocument.Parse(result);
        var path = jsonDocument.RootElement
            .GetProperty("paths")
            .EnumerateArray()
            .FirstOrDefault();

        var coords = path.GetProperty("points")
            .GetProperty("coordinates")
            .EnumerateArray()
            .Select(c => new GeoCoordinate
            {
                Latitude = c[1].GetDouble(),
                Longitude = c[0].GetDouble()
            })
            .ToList();

        path.TryGetProperty("distance", out var distance);
        path.TryGetProperty("time", out var time);

        var routingResult = new OpenRailRoutingResult
        {
            Coordinates = coords,
            Distance = distance.GetDouble(),
        };

        LastResult = routingResult;
        return routingResult;
    }
}
