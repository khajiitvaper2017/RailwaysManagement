// RailwaysManagement\Models\NewtonsoftRouteActionConverter.cs
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailwaysManagement.Models.Actions;
using RailwaysManagement.Services;

namespace RailwaysManagement.Models;


// First, add these new route action types to represent plant pickups and deliveries:

public class NewtonsoftRouteActionConverter : JsonConverter<IRouteAction>
{
    public override bool CanWrite => true;
    public override bool CanRead => true;

    public override IRouteAction ReadJson(JsonReader reader, Type objectType, IRouteAction existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Load the JSON object into a JObject
        JObject jo = JObject.Load(reader);

        // Get the Type property
        string actionType = jo["Type"]?.Value<string>();

        // Create the appropriate concrete type based on the Type property
        IRouteAction routeAction = actionType switch
        {
            "StationDeparture" => new StationDepartureAction(),
            "StationArrival" => new StationArrivalAction(),
            "LoadCargo" => new LoadCargoAction(),
            "UnloadCargo" => new UnloadCargoAction(),
            "PickupFromPlant" => new PickupFromPlantAction(),
            "DeliverToPlant" => new DeliverToPlantAction(),
            "DirectReturnToBase" => new DirectReturnToBaseAction(),
            _ => throw new JsonException($"Unknown route action type: {actionType}")
        };

        // Use the existing serializer to populate the new object
        serializer.Populate(jo.CreateReader(), routeAction);

        return routeAction;
    }

    public override void WriteJson(JsonWriter writer, IRouteAction value, JsonSerializer serializer)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var jo = JObject.FromObject(value, JsonSerializer.Create(settings));
            jo.WriteTo(writer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Json object: {value}");
            throw new JsonSerializationException($"Error serializing route action of type {value.GetType().Name}: {ex.Message}", ex);
        }
    }
}