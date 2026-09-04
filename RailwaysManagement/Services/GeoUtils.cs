using RailwaysManagement.Models;

namespace RailwaysManagement.Services;

public static class GeoUtils
{
    public static double Haversine((double lat, double lon) p1, (double lat, double lon) p2)
    {
        const double R = 6371000;
        var lat1Rad = DegreesToRadians(p1.lat);
        var lat2Rad = DegreesToRadians(p2.lat);
        var deltaLat = DegreesToRadians(p2.lat - p1.lat);
        var deltaLon = DegreesToRadians(p2.lon - p1.lon);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    public static double CalculateDistance(Location? userLocation, Location argLocation)
    {
        var distance = Haversine((userLocation.Latitude, userLocation.Longitude), (argLocation.Latitude, argLocation.Longitude));
        return distance;
    }
}