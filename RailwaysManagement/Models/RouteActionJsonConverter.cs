using System.Text.Json;
using System.Text.Json.Serialization;
using RailwaysManagement.Models.Actions;

namespace RailwaysManagement.Models;

public class RouteActionJsonConverter : JsonConverter<IRouteAction>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IRouteAction).IsAssignableFrom(typeToConvert);
    }

    public override IRouteAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // We need to look ahead to find the type
        var readerClone = reader;
        
        if (readerClone.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object");
        }
        
        // Move to first property
        readerClone.Read();
        
        string typePropertyName = null;
        
        // Find the "Type" property
        while (readerClone.TokenType == JsonTokenType.PropertyName)
        {
            string propertyName = readerClone.GetString();
            readerClone.Read(); // Move to property value
            
            if (propertyName.Equals("Type", StringComparison.OrdinalIgnoreCase))
            {
                typePropertyName = readerClone.GetString();
                break;
            }
            
            // Skip this property value
            readerClone.Skip();
            readerClone.Read(); // Move to next property name or end object
        }
        
        // Now we know the type, we can deserialize to the correct concrete type
        return typePropertyName switch
        {
            "StationDeparture" => JsonSerializer.Deserialize<StationDepartureAction>(ref reader, options),
            "StationArrival" => JsonSerializer.Deserialize<StationArrivalAction>(ref reader, options),
            "LoadCargo" => JsonSerializer.Deserialize<LoadCargoAction>(ref reader, options),
            "UnloadCargo" => JsonSerializer.Deserialize<UnloadCargoAction>(ref reader, options),
            "PickupFromPlant" => JsonSerializer.Deserialize<PickupFromPlantAction>(ref reader, options),
            "DeliverToPlant" => JsonSerializer.Deserialize<DeliverToPlantAction>(ref reader, options),
            "DirectReturnToBase" => JsonSerializer.Deserialize<DirectReturnToBaseAction>(ref reader, options),
            _ => throw new JsonException($"Unknown route action type: {typePropertyName}")
        };
    }

    public override void Write(Utf8JsonWriter writer, IRouteAction value, JsonSerializerOptions options)
    {
        // Use the specific type serializer based on the concrete type
        switch (value)
        {
            case StationDepartureAction stationDeparture:
                JsonSerializer.Serialize(writer, stationDeparture, options);
                break;
            case StationArrivalAction stationArrival:
                JsonSerializer.Serialize(writer, stationArrival, options);
                break;
            case LoadCargoAction loadCargo:
                JsonSerializer.Serialize(writer, loadCargo, options);
                break;
            case UnloadCargoAction unloadCargo:
                JsonSerializer.Serialize(writer, unloadCargo, options);
                break;
            default:
                throw new JsonException($"Unknown route action type: {value.GetType().Name}");
        }
    }
}