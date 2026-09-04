window.mapInterop = {
    _maps: {},
    _dotNetHelpers: {},
    _stationMarkers: {},
    _routePointMarkers: {},
    _currentRouteLayers: {},
    _tempMarkers: {},
    _selectedStationId: {}, // Track the selected station ID for each map
    // Add these new icon definitions near the top of mapInterop object
    _routeStartMarker: {},
    _routeEndMarker: {},
    _actionRouteLayers: {}, // Store action route layers for each map

    // Enhanced displayRoute function with different colors for different action types
    displayRouteWithEndpoints: function (elementId, routeActions) {
        try {
            const map = this._maps[elementId];
            if (!map) {
                console.error("Map not found:", elementId);
                return;
            }

            // Clear any existing route and markers
            this.clearRoute(elementId);

            if (!routeActions || !routeActions.length) {
                console.error("No route actions provided");
                return false;
            }

            // Filter out actions with no locations
            const actionsWithLocations = routeActions.filter(action =>
                action.locations && action.locations.length > 1);

            if (!actionsWithLocations.length) {
                console.error("No valid route segments found");
                return false;
            }

            // Initialize action layers collection
            this._actionRouteLayers[elementId] = [];

            // Create a group to hold all route layers
            const routeLayerGroup = L.layerGroup().addTo(map);
            this._currentRouteLayers[elementId] = routeLayerGroup;

            // Find the first and last valid locations for start/end markers
            const firstAction = actionsWithLocations[0];
            const lastAction = actionsWithLocations[actionsWithLocations.length - 1];

            const startPoint = firstAction.locations[0];
            const endPoint = lastAction.locations[lastAction.locations.length - 1];

            // Create start and end icons
            const startIcon = L.icon({
                iconUrl: "images/start.png",
                iconSize: [32, 32],
                iconAnchor: [16, 32],
                popupAnchor: [0, -32]
            });

            const endIcon = L.icon({
                iconUrl: "images/finish.png",
                iconSize: [32, 32],
                iconAnchor: [16, 32],
                popupAnchor: [0, -32]
            });

            // Add start marker
            const startMarker = L.marker([startPoint.latitude, startPoint.longitude], {
                icon: startIcon,
                zIndexOffset: 1000 // Make sure it's on top
            }).addTo(map);
            this._routeStartMarker[elementId] = startMarker;

            // Add end marker
            const endMarker = L.marker([endPoint.latitude, endPoint.longitude], {
                icon: endIcon,
                zIndexOffset: 1000 // Make sure it's on top
            }).addTo(map);
            this._routeEndMarker[elementId] = endMarker;

            // Create bounds to track the entire route
            const bounds = L.latLngBounds();

            // Color mapping for different action types
            const actionColors = {
                'StationArrival': '#28a745', // Green for arrivals
                'StationDeparture': '#dc3545', // Red for departures
                'LoadCargo': '#17a2b8', // Teal for loading
                'UnloadCargo': '#6610f2', // Purple for unloading
                'DirectReturnToBase': '#fd7e14', // Orange for direct return
                'default': '#0066CC' // Default blue
            };

            // Process each action with locations
            actionsWithLocations.forEach(action => {
                if (!action.locations || action.locations.length < 2) return;

                // Determine color based on action type
                const actionType = action.type || 'default';
                const color = actionColors[actionType] || actionColors['default'];

                // Create polyline for this action's path
                const actionPolyline = L.polyline(
                    action.locations.map(coord => [coord.latitude, coord.longitude]),
                    {
                        color: color,
                        weight: 5,
                        opacity: 0.7,
                        lineJoin: 'round'
                    }
                ).addTo(map);

                // Add tooltip with action description
                if (action.description) {
                    actionPolyline.bindTooltip(action.description);
                }

                // Store the layer and add to group
                this._actionRouteLayers[elementId].push(actionPolyline);
                routeLayerGroup.addLayer(actionPolyline);

                // Extend bounds to include this segment
                actionPolyline.getLatLngs().forEach(latlng => {
                    bounds.extend(latlng);
                });
            });

            // Fit the map to show the entire route
            if (bounds.isValid()) {
                map.fitBounds(bounds, {
                    padding: [50, 50],
                    maxZoom: 12
                });
            }

            console.log("Route with colored action paths displayed successfully");
            return true;
        } catch (error) {
            console.error("Error displaying route with endpoints:", error);
            return false;
        }
    },

    // Update clearRoute to also remove action layers
    clearRoute: function (elementId) {
        try {
            const map = this._maps[elementId];
            if (!map) {
                console.error("Map not found for clearing route:", elementId);
                return;
            }

            // Clear the route layer group if it exists
            if (this._currentRouteLayers[elementId]) {
                map.removeLayer(this._currentRouteLayers[elementId]);
                this._currentRouteLayers[elementId] = null;
            }

            // Clear individual action layers
            if (this._actionRouteLayers[elementId]) {
                this._actionRouteLayers[elementId].forEach(layer => {
                    if (map.hasLayer(layer)) {
                        map.removeLayer(layer);
                    }
                });
                this._actionRouteLayers[elementId] = [];
            }

            // Clear start marker if it exists
            if (this._routeStartMarker[elementId]) {
                map.removeLayer(this._routeStartMarker[elementId]);
                this._routeStartMarker[elementId] = null;
            }

            // Clear end marker if it exists
            if (this._routeEndMarker[elementId]) {
                map.removeLayer(this._routeEndMarker[elementId]);
                this._routeEndMarker[elementId] = null;
            }

            console.log("Route and markers cleared successfully");
        } catch (error) {
            console.error("Error clearing route:", error);
        }
    },
    // Check if Leaflet is loaded
    ensureLeafletLoaded: function() {
        if (typeof L === "undefined") {
            console.error("Leaflet library is not loaded. Make sure to include Leaflet JS and CSS files.");
            return false;
        }
        return true;
    },

    // General map initialization
    initMap: function(elementId, options) {
        if (!this.ensureLeafletLoaded()) return null;

        // Remove existing map if present
        if (window.mapInterop._maps[elementId]) {
            window.mapInterop._maps[elementId].remove();
            delete window.mapInterop._maps[elementId];
        }

        // Default options
        options = options || {};
        const center = options.center || [49.0, 32.0];
        const zoom = options.zoom || 6;
        const minZoom = options.minZoom || 6;
        const attribution = options.attribution || "&copy; OpenStreetMap contributors";
        const tileUrl = options.tileUrl || "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

        const map = L.map(elementId,
            {
                center: center,
                zoom: zoom
            });

        L.tileLayer(tileUrl,
            {
                minZoom: minZoom,
                attribution: attribution
            }).addTo(map);

        window.mapInterop._maps[elementId] = map;
        return map;
    },
    initTrainStationMap: function (mapId, stations, dotNetHelper, selectedStationId) {
        try {
            console.log("Initializing train station map with ID:", mapId);

            // Initialize base map using shared function
            var map = this.initMap(mapId, {
                center: [49.0, 31.0],
                zoom: 6
            });
            if (!map) return;

            // Store dotNetHelper reference
            this._dotNetHelpers[mapId] = dotNetHelper;
            this._selectedStationId[mapId] = selectedStationId || null;

            // Initialize station markers storage
            if (!this._stationMarkers) this._stationMarkers = {};
            this._stationMarkers[mapId] = {};

            // Create station icons (regular and selected)
            const regularStationIcon = L.icon({
                iconUrl: "images/station.png",
                iconSize: [24, 24],
                iconAnchor: [12, 12],
                popupAnchor: [0, -12]
            });

            const selectedStationIcon = L.icon({
                iconUrl: "images/station.png",
                iconSize: [36, 36],  // Larger for selected station
                iconAnchor: [18, 18],
                popupAnchor: [0, -18]
            });

            // Add station markers
            if (stations && stations.length > 0) {
                stations.forEach(station => {
                    if (!station || typeof station.latitude === 'undefined' ||
                        typeof station.longitude === 'undefined' || !station.id) return;

                    const lat = station.latitude;
                    const lng = station.longitude;
                    const name = station.name || "Railway Station";

                    // Check if this is the selected station
                    const isSelected = station.id === selectedStationId;
                    const icon = isSelected ? selectedStationIcon : regularStationIcon;

                    const marker = L.marker([lat, lng], {
                        icon: icon,
                        title: name,
                        stationId: station.id
                    }).addTo(map);

                    marker.bindPopup("<b>" + name + "</b>");

                    // Store marker reference
                    this._stationMarkers[mapId][station.id] = {
                        marker: marker,
                        data: station
                    };

                    marker.on('click', () => {
                        // Reset all station icons
                        Object.values(this._stationMarkers[mapId]).forEach(stationObj => {
                            stationObj.marker.setIcon(regularStationIcon);
                        });

                        // Set this station's icon to selected
                        marker.setIcon(selectedStationIcon);

                        // Store selected station ID
                        this._selectedStationId[mapId] = station.id;

                        // Notify Blazor component
                        dotNetHelper.invokeMethodAsync('StationSelected', station.id);
                    });
                });
            }

            // If a station is selected, center the map on it
            if (selectedStationId && this._stationMarkers[mapId][selectedStationId]) {
                const stationObj = this._stationMarkers[mapId][selectedStationId];
                const marker = stationObj.marker;
                map.setView(marker.getLatLng(), 10);
                marker.openPopup();
            }

            // Force map to recalculate its container size
            setTimeout(function () { map.invalidateSize(); }, 100);

            console.log("Train station map initialized successfully");
        } catch (error) {
            console.error("Error initializing train station map:", error);
        }
    },
    // Station-specific map initialization
    initStationsMap: function(elementId, stations, dotNetHelper) {
        console.log("Initializing stations map with ID:", elementId);

        try {
            var map = this.initMap(elementId);

            window.mapInterop._stationMarkers = window.mapInterop._stationMarkers || {};
            window.mapInterop._stationMarkers[elementId] = {};

            var stationIcon = L.icon({
                iconUrl: "images/station.png",
                iconSize: [24, 24],
                iconAnchor: [12, 12],
                popupAnchor: [0, -12]
            });

            if (stations && stations.length > 0) {
                stations.forEach(function(station) {
                    if (!station.location) return;

                    var lat = station.location.latitude;
                    var lng = station.location.longitude;
                    if (!lat || !lng) return;

                    const marker = L.marker([lat, lng], { icon: stationIcon }).addTo(map);
                    marker.on("click",
                        function() {
                            dotNetHelper.invokeMethodAsync("StationSelected", lat, lng);
                        });

                    const key = lat + "_" + lng;
                    window.mapInterop._stationMarkers[elementId][key] = marker;
                });
            }

            window.mapInterop._dotNetHelpers[elementId] = dotNetHelper;
            window.mapInterop._routePointMarkers[elementId] = {};
            window.mapInterop._currentRouteLayers[elementId] = {};
            window.mapInterop._tempMarkers[elementId] = null;

            window.mapInterop.handleRightClickForNewStation(map, dotNetHelper, elementId);

            console.log("Stations map initialization complete");
        } catch (error) {
            console.error("Error during stations map initialization:", error);
        }
    },
    // Add this to the mapInterop object in leafletInterop.js
    displayRoute: function(elementId, routeCoordinates) {
        try {
            const map = this._maps[elementId];
            if (!map) {
                console.error("Map not found:", elementId);
                return;
            }

            // Clear any existing route layer
            if (this._currentRouteLayers[elementId]) {
                map.removeLayer(this._currentRouteLayers[elementId]);
            }

            // Create a polyline with the route coordinates
            const routePolyline = L.polyline(
                routeCoordinates.map(coord => [coord.latitude, coord.longitude]),
                {
                    color: "#0066CC",
                    weight: 5,
                    opacity: 0.7,
                    lineJoin: "round"
                }
            ).addTo(map);

            // Store the layer for later reference
            this._currentRouteLayers[elementId] = routePolyline;

            // Fit the map to show the entire route
            map.fitBounds(routePolyline.getBounds(),
                {
                    padding: [50, 50],
                    maxZoom: 12
                });

            console.log("Route displayed successfully");
            return true;
        } catch (error) {
            console.error("Error displaying route:", error);
            return false;
        }
    },

    // Allow client to pick any location on map for registration
    initClientLocationMap: function(elementId, stations, dotNetHelper, initialLocation, assignedStationId) {
        try {
            console.log("Initializing client location map with ID:", elementId);
            
            // Initialize base map
            var map = this.initMap(elementId);
            if (!map) return;
            
            // Store dotNetHelper reference for callbacks
            this._dotNetHelpers[elementId] = dotNetHelper;
            this._selectedStationId[elementId] = assignedStationId || null;
            
            // Store station markers for this map
            if (!this._stationMarkers) this._stationMarkers = {};
            this._stationMarkers[elementId] = {};
            
            // Set initial view based on user's existing location or default to Ukraine
            if (initialLocation && initialLocation.latitude && initialLocation.longitude) {
                map.setView([initialLocation.latitude, initialLocation.longitude], 13);
                
                // If user already has a location, add a marker for it
                const userMarker = L.marker([initialLocation.latitude, initialLocation.longitude]).addTo(map);
                window.mapInterop._tempMarkers[elementId] = userMarker;
            } else {
                map.setView([49.0, 32.0], 6); // Default view centered on Ukraine
            }
            
            // Create regular and selected station icons with different sizes
            const regularStationIcon = L.icon({
                iconUrl: "images/station.png",
                iconSize: [24, 24],
                iconAnchor: [12, 12],
                popupAnchor: [0, -12]
            });
            
            const selectedStationIcon = L.icon({
                iconUrl: "images/station.png",
                iconSize: [36, 36],  // 50% larger for selected station
                iconAnchor: [18, 18],
                popupAnchor: [0, -18]
            });
            
            // Add station markers to the map
            if (stations && stations.length > 0) {
                stations.forEach(station => {
                    if (!station || typeof station.latitude === 'undefined' || typeof station.longitude === 'undefined') return;
                    
                    const lat = station.latitude;
                    const lng = station.longitude;
                    const id = station.id;
                    const name = station.name || "Railway Station";
                    
                    if (!lat || !lng || !id) return;
                    
                    // Check if this is the assigned station and use the appropriate icon
                    const isAssignedStation = id === assignedStationId;
                    const icon = isAssignedStation ? selectedStationIcon : regularStationIcon;
                    
                    const marker = L.marker([lat, lng], { icon: icon }).addTo(map)
                        .bindPopup(isAssignedStation ? `<strong>${name}</strong> (Поточна вибрана)` : name);
                    
                    // If this is the assigned station, open its popup automatically
                    if (isAssignedStation) {
                        marker.openPopup();
                        // If we have both location and assigned station, zoom to show both
                        if (initialLocation && initialLocation.latitude && initialLocation.longitude) {
                            const bounds = L.latLngBounds(
                                [initialLocation.latitude, initialLocation.longitude],
                                [lat, lng]
                            );
                            map.fitBounds(bounds, { padding: [50, 50] });
                        } else {
                            // Otherwise just zoom to the station
                            map.setView([lat, lng], 13);
                        }
                    }
                        
                    // Store the marker for later reference
                    this._stationMarkers[elementId][id] = {
                        marker: marker,
                        data: station
                    };
                        
                    marker.on("click", () => {
                        // Reset all station icons to regular size
                        Object.values(this._stationMarkers[elementId]).forEach(stationObj => {
                            stationObj.marker.setIcon(regularStationIcon);
                        });
                        
                        // Set this station's icon to the larger size
                        marker.setIcon(selectedStationIcon);
                        
                        // Store the selected station ID
                        this._selectedStationId[elementId] = id;
                        
                        // Notify Blazor about the station selection by ID
                        dotNetHelper.invokeMethodAsync("StationSelected", id);
                        
                        // Update popup content
                        marker.setPopupContent(`<strong>${name}</strong> (Вибрана)`);
                        marker.openPopup();
                    });
                });
            }
            
            // Handle map click to select any location
            map.on("click", function(e) {
                // Remove old marker
                if (window.mapInterop._tempMarkers[elementId]) {
                    map.removeLayer(window.mapInterop._tempMarkers[elementId]);
                }
                
                // Add new marker at clicked location
                const marker = L.marker([e.latlng.lat, e.latlng.lng]).addTo(map);
                window.mapInterop._tempMarkers[elementId] = marker;
                
                // Notify Blazor of selected location
                dotNetHelper.invokeMethodAsync("ClientLocationSelected", e.latlng.lat, e.latlng.lng);
            });
            
            console.log("Client location map initialized successfully");
        } catch (error) {
            console.error("Error initializing client location map:", error);
        }
    },

    // Used by Create.razor
    initRouteRequestCreateMap: function(elementId, stations, dotNetHelper) {
        const map = this.initMap(elementId);
        if (!map) return;

        this._dotNetHelpers[elementId] = dotNetHelper;

        // Add station markers.
        if (stations && stations.length > 0) {
            const stationIcon = L.icon({
                iconUrl: "images/station.png",
                iconSize: [24, 24],
                iconAnchor: [12, 12]
            });
            stations.forEach(station => {
                if (!station.location) return;
                const { latitude, longitude } = station.location;
                if (!latitude || !longitude) return;

                const marker = L.marker([latitude, longitude], { icon: stationIcon }).addTo(map);
                marker.on("click",
                    () => {
                        dotNetHelper.invokeMethodAsync("StationSelectedForRouteJs", station);
                    });
            });
        }

    },

    // Add a specific function for the routing page
    initRoutingMap: function(elementId, stations) {
        const map = this.initMap(elementId);
        if (!map) return;

        // Store station markers for this map
        if (!this._stationMarkers) this._stationMarkers = {};
        this._stationMarkers[elementId] = {};

        const stationIcon = L.icon({
            iconUrl: "images/station.png",
            iconSize: [24, 24],
            iconAnchor: [12, 12],
            popupAnchor: [0, -12]
        });

        // Add station markers
        if (stations && stations.length > 0) {
            stations.forEach(station => {
                if (!station.location) return;
                const lat = station.location.latitude;
                const lng = station.location.longitude;
                if (!lat || !lng) return;

                const marker = L.marker([lat, lng], { icon: stationIcon })
                    .addTo(map)
                    .bindPopup(station.name || "Станція");

                // Store marker reference
                this._stationMarkers[elementId][station.id] = {
                    marker: marker,
                    data: station
                };
            });
        }
    },

    addStationMarker: function(elementId, station) {
        const map = this._maps[elementId];
        if (!map) return;

        // Exit if station has no location
        if (!station || !station.location || 
            typeof station.location.latitude === 'undefined' || 
            typeof station.location.longitude === 'undefined') return;

        const lat = station.location.latitude;
        const lng = station.location.longitude;
        const name = station.name || "Станція";

        const stationIcon = L.icon({
            iconUrl: "images/station.png",
            iconSize: [24, 24],
            iconAnchor: [12, 12],
            popupAnchor: [0, -12]
        });

        // Add marker to map
        const marker = L.marker([lat, lng], { icon: stationIcon })
            .addTo(map)
            .bindPopup(name);

        // Store marker reference
        if (!this._stationMarkers) this._stationMarkers = {};
        if (!this._stationMarkers[elementId]) this._stationMarkers[elementId] = {};
        this._stationMarkers[elementId][station.id] = {
            marker: marker,
            data: station
        };
    },

    // Creates the context menu element near the cursor point
    _showContextMenu: function(elementId, point, latlng) {
        this._removeContextMenu(); // remove an existing menu

        // Create wrapper div
        const menu = document.createElement("div");
        menu.style.position = "absolute";
        menu.style.left = point.x + "px";
        menu.style.top = point.y + "px";
        menu.style.background = "#fff";
        menu.style.border = "1px solid #ccc";
        menu.style.padding = "5px";
        menu.style.zIndex = 9999;

        // Create "Set Start Point" item
        const startItem = document.createElement("div");
        startItem.innerText = "Set Start Point";
        startItem.style.cursor = "pointer";
        startItem.onclick = () => {
            this._removeContextMenu();
            this._dotNetHelpers[elementId]
                .invokeMethodAsync("SetStartPointCS", latlng.lat, latlng.lng);
        };
        menu.appendChild(startItem);

        // Create "Set Destination Point" item
        const destItem = document.createElement("div");
        destItem.innerText = "Set Destination Point";
        destItem.style.cursor = "pointer";
        destItem.onclick = () => {
            this._removeContextMenu();
            this._dotNetHelpers[elementId]
                .invokeMethodAsync("SetDestinationPointCS", latlng.lat, latlng.lng);
        };
        menu.appendChild(destItem);

        // Add menu to map container
        const mapContainer = this._maps[elementId].getContainer();
        mapContainer.appendChild(menu);
        this._contextMenuDiv = menu;
    },

    // Removes any existing context menu
    _removeContextMenu: function() {
        if (this._contextMenuDiv) {
            this._contextMenuDiv.remove();
            this._contextMenuDiv = null;
        }
    },

    handleRightClickForNewStation: function(map, dotNetHelper, elementId) {
        if (!map || !this.ensureLeafletLoaded()) return;

        try {
            map.on("contextmenu",
                function(e) {
                    e.originalEvent.preventDefault();

                    const oldMarker = window.mapInterop._tempMarkers[elementId];
                    if (oldMarker) {
                        map.removeLayer(oldMarker);
                    }

                    const tempMarker = L.marker([e.latlng.lat, e.latlng.lng]).addTo(map);
                    window.mapInterop._tempMarkers[elementId] = tempMarker;

                    dotNetHelper.invokeMethodAsync("AddNewStationCS", e.latlng.lat, e.latlng.lng);
                });
        } catch (error) {
            console.error("Error setting up right-click handler:", error);
        }
    },

    zoomTo: function(elementId, lat, lng) {
        try {
            const map = window.mapInterop._maps[elementId];
            if (!map) {
                console.error("Map not found for zoom operation:", elementId);
                return;
            }

            map.flyTo([lat, lng], 17, { duration: 1 });
        } catch (error) {
            console.error("Error during zoom operation:", error);
        }
    },

    // Add or update sender/receiver client markers on the map
    setClientLocationMarker: function(elementId, clientType, location) {
        if (!this.ensureLeafletLoaded()) return;

        const map = this._maps[elementId];
        if (!map) return;

        // Use a unique key for sender/receiver
        if (!this._clientMarkers) this._clientMarkers = {};
        if (!this._clientMarkers[elementId]) this._clientMarkers[elementId] = {};

        // Remove old marker if exists
        if (this._clientMarkers[elementId][clientType]) {
            map.removeLayer(this._clientMarkers[elementId][clientType]);
            this._clientMarkers[elementId][clientType] = null;
        }

        if (location && location.latitude && location.longitude) {
            // Choose different icons for sender/receiver
            let iconUrl = "images/client.png";
            // Fallback to default marker if custom icon not found
            const icon = L.icon({
                iconUrl: iconUrl,
                iconSize: [28, 28],
                iconAnchor: [14, 28],
                popupAnchor: [0, -28]
            });

            const marker = L.marker([location.latitude, location.longitude], { icon: icon })
                .addTo(map)
                .bindPopup(clientType === "sender" ? "Відправник" : "Одержувач");
            this._clientMarkers[elementId][clientType] = marker;
        }
    },

    // Optionally, clear all client markers
    clearClientLocationMarkers: function(elementId) {
        if (!this._clientMarkers || !this._clientMarkers[elementId]) return;
        const map = this._maps[elementId];
        if (!map) return;
        for (const key of ["sender", "receiver"]) {
            if (this._clientMarkers[elementId][key]) {
                map.removeLayer(this._clientMarkers[elementId][key]);
                this._clientMarkers[elementId][key] = null;
            }
        }
    }
};
