import json
import folium

# Load the GeoJSON file with UTF-8 encoding
geojson_path = "station.geojson"
with open(geojson_path, encoding="utf-8") as f:
    geojson_data = json.load(f)

# Extract a center coordinate from the first feature
first_feature = geojson_data["features"][0]
geometry = first_feature["geometry"]

if geometry["type"] == "Polygon":
    coords = geometry["coordinates"][0][0]
elif geometry["type"] == "MultiPolygon":
    coords = geometry["coordinates"][0][0][0]
else:
    coords = [0, 0]  # fallback

# Reverse coordinates to [lat, lon]
center = [coords[1], coords[0]]

# Create a folium map
m = folium.Map(location=center, zoom_start=12)

# Add the GeoJSON layer to the map
folium.GeoJson(geojson_data, name="Stations").add_to(m)

# Save the map to an HTML file
output_file = "map.html"
m.save(output_file)

print(f"Map saved to {output_file}")
