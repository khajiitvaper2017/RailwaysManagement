using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using PolylineEncoder.Net.Models;

namespace RailwaysManagement.Models;

[ComplexType]
public class Location : IGeoCoordinate
{
    public Location(double latitude, double longitude, string stationId = "")
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public Location() { }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override string ToString()
    {
        return $"{Latitude.ToString(CultureInfo.InvariantCulture)},{Longitude.ToString(CultureInfo.InvariantCulture)}";
    }
}