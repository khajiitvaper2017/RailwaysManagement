using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PolylineEncoder.Net.Models;
using RailwaysManagement.DbModels;
using RailwaysManagement.Models;
using RailwaysManagement.Models.Actions;

namespace RailwaysManagement.Services;

// Route action implementation for serialization in PlannedRoute

public class RoutingService
{
    private const double TrainSpeedKmh = 25.0; // Kilometers per hour
    private readonly IDbContextFactory<RailwaysDbContext> _dbContextFactory;
    private readonly OpenRailRoutingService _openRailRoutingService;
    private readonly HashSet<Train> usedTrains = new();

    public RoutingService(IDbContextFactory<RailwaysDbContext> dbContextFactory,
        OpenRailRoutingService openRailRoutingService)
    {
        _dbContextFactory = dbContextFactory;
        _openRailRoutingService = openRailRoutingService;
        Context = _dbContextFactory.CreateDbContext();
    }

    public RailwaysDbContext Context;

    // Main entry: create optimal routes for all unplanned route requests
    public async Task<IEnumerable<RailRoute>> CreateRoutes()
    {
        var createdRoutes = new List<RailRoute>();

        // 1. Get all unplanned route requests
        var unplannedRequests = await Context.RouteRequests
            .Where(rr => !rr.IsPlanned && !rr.IsCompleted && !rr.IsDeleted)
            .Include(rr => rr.Cargos)
            .Include(rr => rr.SenderClient)
            .ThenInclude(sc => sc.AssignedStation)
            .ThenInclude(station => station.Trains)
            .Include(rr => rr.ReceiverClient)
            .ThenInclude(rc => rc.AssignedStation)
            .ThenInclude(station => station.Trains)
            .ToListAsync();

        if (!unplannedRequests.Any()) return createdRoutes;

        // 2. Get all stations and available trains
        var stations = await Context.Stations
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        // First get all non-deleted trains
        var allTrains = await Context.Trains
            .Where(t => !t.IsDeleted)
            .Include(t => t.Station) // Include the station for each train
            .Include(t => t.RouteParts)
            .ThenInclude(rp => rp.RailRouteRouteParts)
            .ThenInclude(rrp => rrp.RailRoute)
            .ThenInclude(rr => rr.RouteRequest)
            .ToListAsync();

        // Then filter in memory based on the IsAvailable property
        var availableTrains = allTrains.Where(t => t.IsAvailable).ToList();

        // create routes considering all route requests simultaneously
        Console.WriteLine($"Creating routes for {unplannedRequests.Count} unplanned requests.");

        var directRoutes = new Dictionary<RouteRequest, List<IGeoCoordinate>>();
        var directRouteStations = new Dictionary<RouteRequest, List<Station>>();

        foreach (var request in unplannedRequests)
        {
            // 3. Find all direct routes for each request
            var startStation = request.SenderClient?.AssignedStation?.Location;
            var endStation = request.ReceiverClient?.AssignedStation?.Location;

            Console.WriteLine($"Finding route for request {request.Id} from {startStation.Latitude},{startStation.Longitude} " +
                              $"to {endStation.Latitude},{endStation.Longitude}.");

            var route = await _openRailRoutingService.FindRouteAsync(startStation, endStation);

            if (!route.Coordinates.Any()) continue;

            directRoutes[request] = route.Coordinates.ToList();
            // Find nearby stations for the route
            var nearbyStations = FindStationsNearRoute(stations, route.Coordinates);
            directRouteStations[request] = nearbyStations;
        }

        // 4. Create individual routes for each request


        createdRoutes = CreateIndividualRoutes(unplannedRequests, directRoutes, directRouteStations, availableTrains);

        // 5. Consolidate route parts of routes with common paths


        await ConsolidateSimple(createdRoutes);

        try
        {
            await Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving routes: {ex.Message}");
            return createdRoutes; // Return empty if save failed
        }

        return createdRoutes;
    }

    private async Task ConsolidateSimple(List<RailRoute> createdRoutes)
    {
        Tuple<RailRoute, Station, Station> GetFirstAndThirdStation(RailRoute route)
        {
            var firstStationName = route.RailRouteRouteParts.FirstOrDefault()?.RoutePart?.PlannedRoute?.Actions
                .OfType<StationDepartureAction>().FirstOrDefault()?.StationName;
            var thirdStationName = route.RailRouteRouteParts.FirstOrDefault()?.RoutePart?.PlannedRoute?.Actions
                .OfType<StationArrivalAction>().Skip(2).FirstOrDefault()?.StationName;
            if (string.IsNullOrEmpty(firstStationName) || string.IsNullOrEmpty(thirdStationName)) return null;
            var firstStation = Context.Stations.FirstOrDefault(s => s.Name == firstStationName);
            var thirdStation = Context.Stations.FirstOrDefault(s => s.Name == thirdStationName);
            return Tuple.Create(route, firstStation, thirdStation);
        }

        var sameFirstAndThirdStations = createdRoutes
            .Select(GetFirstAndThirdStation)
            .Where(tuple => tuple != null && tuple.Item2 != null && tuple.Item3 != null)
            .GroupBy(tuple => new { FirstStation = tuple.Item2.Name, ThirdStation = tuple.Item3.Name })
            .Where(group => group.Count() > 1) // Only groups with more than one route
            .ToList();

        await Context.SaveChangesAsync();

        foreach (var group in sameFirstAndThirdStations)
        {
            var firstRoute = group.First().Item1;
            var firstRoutePart = firstRoute.RailRouteRouteParts.FirstOrDefault()?.RoutePart;
            if (firstRoutePart == null) continue; // Skip if no route part in route
            var firstTrain = firstRoutePart.Train;
            if (firstTrain == null) continue; // Skip if no train in route part
            foreach (var tuple in group.Skip(1)) // Skip the first route, it's already processed
            {
                var secondRoute = tuple.Item1;
                var secondRoutePart = secondRoute.RailRouteRouteParts.FirstOrDefault()?.RoutePart;
                if (secondRoutePart == null) continue; // Skip if no route part in second route
                // Assign the same train to the second route part
                secondRoutePart.Train = firstTrain;
                firstTrain.RouteParts.Add(secondRoutePart); // Add second route part to the train's route parts
                // Update train references in actions.
                foreach (var action in secondRoutePart.PlannedRoute.Actions)
                {
                    switch (action)
                    {
                        case StationDepartureAction departureAction:
                            departureAction.TrainId = firstTrain.Id;
                            departureAction.TrainName = firstTrain.Name;
                            break;
                        case StationArrivalAction arrivalAction:
                            arrivalAction.TrainId = firstTrain.Id;
                            arrivalAction.TrainName = firstTrain.Name;
                            break;
                        case PickupFromPlantAction pickupAction:
                            pickupAction.TrainId = firstTrain.Id;
                            pickupAction.TrainName = firstTrain.Name;
                            break;
                        case DeliverToPlantAction deliverAction:
                            deliverAction.TrainId = firstTrain.Id;
                            deliverAction.TrainName = firstTrain.Name;
                            break;
                        case UnloadCargoAction unloadAction:
                            unloadAction.TrainId = firstTrain.Id;
                            unloadAction.TrainName = firstTrain.Name;
                            break;
                        case LoadCargoAction loadAction:
                            loadAction.TrainId = firstTrain.Id;
                            loadAction.TrainName = firstTrain.Name;
                            break;
                        case DirectReturnToBaseAction returnAction:
                            returnAction.TrainId = firstTrain.Id;
                            returnAction.TrainName = firstTrain.Name;
                            break;
                    }
                }

                Context.RouteParts.Update(secondRoutePart); // Update second route part in context
                Console.WriteLine($"Consolidated routes {firstRoute.Name} and {secondRoute.Name} by third station {tuple.Item3.Name}.");
            }
        }
    }

    private void ConsolidateRouteParts(List<RailRoute> routes,
    Dictionary<RouteRequest, List<Station>> directRouteStations, List<Train> availableTrains)
    {
        if (!routes.Any())
        {
            Console.WriteLine("No routes to consolidate.");
            return; // No routes to consolidate
        }

        // Group routes by same start station (plannedroute first action)
        var groupedRoutes = routes.GroupBy(route =>
            route.RailRouteRouteParts.FirstOrDefault()?.RoutePart?.PlannedRoute?.Actions
                .OfType<StationDepartureAction>().FirstOrDefault()?.StationName);
        foreach (var railRoutes in groupedRoutes)
        {
            if (railRoutes.Count() <= 1) continue;

            Console.WriteLine($"Consolidating routes for station {railRoutes.Key} with {railRoutes.Count()} routes.");

            var firstRoute = railRoutes.FirstOrDefault();
            var secondRoute = railRoutes.Skip(1).FirstOrDefault();

            if (firstRoute == null || secondRoute == null)
            {
                Console.WriteLine("First or second route is null, skipping consolidation.");
                continue; // Skip if any route is null
            }

            var firstRoutePart = firstRoute.RailRouteRouteParts.FirstOrDefault()?.RoutePart;
            var secondRoutePart = secondRoute.RailRouteRouteParts.FirstOrDefault()?.RoutePart;

            if (firstRoutePart == null || secondRoutePart == null)
            {
                Console.WriteLine("First or second route part is null, skipping consolidation.");
                continue; // Skip if any route part is null
            }

            // Find the longest common path between the first route and second route
            var firstRouteStations = directRouteStations.ContainsKey(firstRoute.RouteRequest)
                ? directRouteStations[firstRoute.RouteRequest]
                : firstRoutePart.RouteStations.Select(rs => rs.Station).ToList();
            var secondRouteStations = directRouteStations.ContainsKey(secondRoute.RouteRequest)
                ? directRouteStations[secondRoute.RouteRequest]
                : secondRoutePart.RouteStations.Select(rs => rs.Station).ToList();

            var commonStations = FindLongestCommonStationPath(firstRouteStations, secondRouteStations);
            if (commonStations.Count < 2)
            {
                Console.WriteLine("Not enough common stations to proceed with consolidation.");
                continue;
            }

            // Split the route part into two parts: common and individual
            // if commonpath does not start from the first station, skip it
            if (commonStations[0].Id != firstRouteStations[0].Id) continue;

            var firstCommonStation = commonStations.First();
            var lastCommonStation = commonStations.Last();

            // Create the common route part before assigning route stations.
            var commonRoutePart = new RoutePart
            {
                Train = firstRoutePart.Train,
                PlannedRoute = new PlannedRoute
                {
                    // Look for last common station action
                    // copy from beginning to the last common station inclusive
                    Actions = firstRoutePart.PlannedRoute.Actions
                        .Reverse()
                        .SkipWhile(action =>
                            !(action is StationArrivalAction arrivalAction &&
                              arrivalAction.StationId == lastCommonStation.Id))
                        .Reverse().ToList()
                }
            };

            // First add the common route part to the context so it gets an ID
            Context.RouteParts.Add(commonRoutePart);

            commonRoutePart = Context.RouteParts.Where(rp => rp.Id == commonRoutePart.Id)
                .Include(rp => rp.RailRouteRouteParts)
                .ThenInclude(rrp => rrp.RailRoute)
                .FirstOrDefault() ?? commonRoutePart; // Ensure we have the correct RoutePart with ID

            // Now create RouteStations with the correct RoutePartId
            commonRoutePart.RouteStations = commonStations.Select((station, index) => new RoutePartStation
            {
                StationId = station.Id,
                Order = index + 1, // Order starts from 1
                RoutePartId = commonRoutePart.Id // Use commonRoutePart.Id, not firstRoutePart.Id
            }).ToList();

            // Remove route stations from the first and second route parts.
            firstRoutePart.RouteStations = firstRoutePart.RouteStations
                .Reverse()
                .TakeWhile(rs => rs.StationId != lastCommonStation.Id)
                .Reverse().ToList();
            secondRoutePart.RouteStations = secondRoutePart.RouteStations
                .Reverse()
                .TakeWhile(rs => rs.StationId != lastCommonStation.Id)
                .Reverse().ToList();

            // Remove common actions from the first and second route parts.
            firstRoutePart.PlannedRoute.Actions = firstRoutePart.PlannedRoute.Actions
                .Reverse()
                .TakeWhile(action =>
                    !(action is StationArrivalAction arrivalAction &&
                      arrivalAction.StationId == lastCommonStation.Id))
                .Reverse().ToList();

            secondRoutePart.PlannedRoute.Actions = secondRoutePart.PlannedRoute.Actions
                .Reverse()
                .SkipWhile(action =>
                    !(action is StationDepartureAction departureAction &&
                      departureAction.StationId == firstCommonStation.Id))
                .Reverse().ToList();

            // Assign a new train from the last common station.
            var lastCommonStationTrains = Context.Trains
                .Where(t => t.StationId == lastCommonStation.Id)
                .ToList();
            var availableTrain = lastCommonStationTrains.FirstOrDefault(t => !usedTrains.Contains(t) && t.IsAvailable);

            if (availableTrain == null)
            {
                Console.WriteLine($"No available train found at {lastCommonStation.Name} for consolidation.");
                continue; // Skip if no available train
            }

            commonRoutePart.Train = availableTrain;
            // Insert a departure action from the last common station at the beginning of the second route part.
            secondRoutePart.PlannedRoute.Actions = secondRoutePart.PlannedRoute.Actions.Reverse().Append(
                new StationDepartureAction
                {
                    StationId = lastCommonStation.Id,
                    StationName = lastCommonStation.Name,
                    TrainId = availableTrain.Id,
                    TrainName = availableTrain.Name,
                    Time = commonRoutePart.PlannedRoute.Actions
                        .Last().Time + TimeSpan.FromMinutes(30),
                    Locations = new List<Location> { lastCommonStation.Location }
                }).Reverse().ToList();

            // Set the first route part as the second route part in sequence.
            firstRoutePart.RailRouteRouteParts.First().OrderInRailRoute = 2;

            // Add the common route part to the first route.
            var firstCommonRailRouteRoutePart = new RailRouteRoutePart
            {
                RailRouteId = firstRoute.Id,
                RoutePartId = commonRoutePart.Id,
                OrderInRailRoute = 1
            };

            commonRoutePart.RailRouteRouteParts.Add(firstCommonRailRouteRoutePart);
            firstRoute.RailRouteRouteParts.Add(firstCommonRailRouteRoutePart);

            // Update the second route.
            secondRoutePart.RailRouteRouteParts.First().OrderInRailRoute = 2;

            var secondCommonRailRouteRoutePart = new RailRouteRoutePart
            {
                RailRouteId = secondRoute.Id,
                RoutePartId = commonRoutePart.Id,
                OrderInRailRoute = 1
            };

            commonRoutePart.RailRouteRouteParts.Add(secondCommonRailRouteRoutePart);
            secondRoute.RailRouteRouteParts.Add(secondCommonRailRouteRoutePart);

            // Update the context
            Console.WriteLine($"Consolidated routes: {firstRoute.Name} and {secondRoute.Name} into common route part.");
            Console.WriteLine($"Common route part: {commonRoutePart.Id} with train {commonRoutePart.Train.Name}.");
            Console.WriteLine($"First route part: {firstRoutePart.Id} with train {firstRoutePart.Train.Name}.");
            Console.WriteLine($"Second route part: {secondRoutePart.Id} with train {secondRoutePart.Train.Name}.");


            Context.RouteParts.Update(commonRoutePart);
            Context.RouteParts.Update(firstRoutePart);
            Context.RouteParts.Update(secondRoutePart);

            Context.Routes.Update(firstRoute);
            Context.Routes.Update(secondRoute);
        }
    }

    private List<RailRoute> CreateIndividualRoutes(List<RouteRequest> unplannedRequests,
        Dictionary<RouteRequest, List<IGeoCoordinate>> directRoutes,
        Dictionary<RouteRequest, List<Station>> directRouteStations, List<Train> availableTrains)
    {
        // Create individual routes for each request
        // Iterate through each unplanned request and create a route
        // Use OpenRailRoutingService to find the route
        // Find stations along the route
        // Create RailRoute and RoutePart for each request
        // Create correct PlannedRoute for each route part using actions
        var createdRoutes = new List<RailRoute>();

        foreach (var request in unplannedRequests)
        {
            var usedTrain = availableTrains.FirstOrDefault(t =>
                t.StationId == request.SenderClient?.AssignedStation?.Id && !usedTrains.Contains(t));

            if (usedTrain == null)
            {
                Console.WriteLine(
                    $"No available train found for request {request.Id} at {request.SenderClient?.AssignedStation?.Name}.");
                continue; // Skip if no available train
            }

            var newRoutePart = new RoutePart
            {
                Train = usedTrain
            };

            newRoutePart.RouteStations = directRouteStations[request]
                .Select((station, index) => new RoutePartStation
                {
                    StationId = station.Id,
                    Order = index + 1, // Order starts from 1
                    RoutePartId = newRoutePart.Id
                }).ToList();

            var newRoute = new RailRoute
            {
                Name = $"Маршрут {request.SenderClient?.Name} - {request.ReceiverClient?.Name}",
                RouteRequest = request
            };

            var newRailRouteRoutePart = new RailRouteRoutePart
            {
                RailRoute = newRoute,
                RoutePart = newRoutePart
            };

            newRoute.RailRouteRouteParts.Add(newRailRouteRoutePart);
            newRoutePart.RailRouteRouteParts.Add(newRailRouteRoutePart);

            var datetime = request.ShipmentDate ?? DateTime.UtcNow;

            newRoutePart.PlannedRoute = new PlannedRoute
            {
                Actions = new List<IRouteAction>()
            };

            newRoutePart.RoutePartCargos = request.Cargos.Select((cargo, index) => new RoutePartCargo
            {
                CargoId = cargo.Id,
                RoutePartId = newRoutePart.Id,
                Order = index + 1
            }).ToList();

            // Train departures to take cargo from client location
            newRoutePart.PlannedRoute.Actions.Add(
                new StationDepartureAction
                {
                    StationId = request.SenderClient?.AssignedStation?.Id,
                    StationName = request.SenderClient?.AssignedStation?.Name,
                    TrainId = usedTrain?.Id,
                    TrainName = usedTrain?.Name,
                    Time = datetime,
                    Locations = new List<Location>
                    {
                        request.SenderClient?.AssignedStation.Location
                    }
                });

            var time = TimeSpan.FromHours(CalculateDistance(
                request.SenderClient?.AssignedStation.Location.Latitude ?? 0,
                request.SenderClient?.AssignedStation.Location.Longitude ?? 0,
                request.SenderClient?.Location.Latitude ?? 0,
                request.SenderClient?.Location.Longitude ?? 0) / 1000 / TrainSpeedKmh);
            datetime = datetime.Add(time);

            foreach (var requestCargo in request.Cargos)
            {
                newRoutePart.PlannedRoute.Actions.Add(
                    new PickupFromPlantAction
                    {
                        TrainId = usedTrain?.Id,
                        TrainName = usedTrain?.Name,
                        Locations = new List<Location>
                        {
                            request.ReceiverClient?.Location
                        },
                        Time = datetime,
                        CargoId = requestCargo.Id,
                        CargoName = requestCargo.Name,
                        ClientId = request.SenderClient?.Id,
                        ClientName = request.SenderClient?.Name
                    }
                );
                datetime = datetime.Add(TimeSpan.FromMinutes(15));
            }

            // go through all stations in the route

            foreach (var station in directRouteStations[request])
            {
                // Add station arrival action
                newRoutePart.PlannedRoute.Actions.Add(
                    new StationArrivalAction
                    {
                        StationId = station.Id,
                        StationName = station.Name,
                        TrainId = usedTrain?.Id,
                        TrainName = usedTrain?.Name,
                        Time = datetime,
                        Locations = new List<Location> { station.Location }
                    }
                );
                // Calculate time to next station
                if (station == directRouteStations[request].Last()) continue;
                var nextStation = directRouteStations[request][directRouteStations[request].IndexOf(station) + 1];
                var distance = GetDistanceBetweenStations(station, nextStation);
                time = TimeSpan.FromHours(distance / TrainSpeedKmh);
                datetime = datetime.Add(time);
            }

            // departure from last station to receiver client location

            newRoutePart.PlannedRoute.Actions.Add(
                new StationDepartureAction
                {
                    StationId = directRouteStations[request].Last().Id,
                    StationName = directRouteStations[request].Last().Name,
                    TrainId = usedTrain?.Id,
                    TrainName = usedTrain?.Name,
                    Time = datetime,
                    Locations = new List<Location>
                    {
                        directRouteStations[request].Last().Location
                    }
                });

            var timeToReceiver = TimeSpan.FromHours(CalculateDistance(
                directRouteStations[request].Last().Location.Latitude,
                directRouteStations[request].Last().Location.Longitude,
                request.ReceiverClient.Location.Latitude,
                request.ReceiverClient.Location.Longitude) / 1000 / TrainSpeedKmh);

            datetime = datetime.Add(timeToReceiver);

            // DeliverToPlantAction at receiver client location
            newRoutePart.PlannedRoute.Actions.Add(
                new DeliverToPlantAction
                {
                    TrainId = usedTrain?.Id,
                    TrainName = usedTrain?.Name,
                    Locations = new List<Location>
                    {
                        request.ReceiverClient?.Location
                    },
                    Time = datetime,
                    ClientId = request.ReceiverClient?.Id,
                    ClientName = request.ReceiverClient?.Name
                }
            );

            // Unload cargo at receiver client location
            foreach (var requestCargo in request.Cargos)
            {
                newRoutePart.PlannedRoute.Actions.Add(
                    new UnloadCargoAction
                    {
                        TrainId = usedTrain?.Id,
                        TrainName = usedTrain?.Name,
                        Locations = new List<Location>
                        {
                            request.ReceiverClient?.Location
                        },
                        Time = datetime,
                        CargoId = requestCargo.Id,
                        CargoName = requestCargo.Name
                    }
                );
                datetime = datetime.Add(TimeSpan.FromMinutes(15));
            }

            // Return to base 
            var expectedArrivalTimeToBase = datetime.Add(TimeSpan.FromHours(
                CalculateDistance(request.ReceiverClient?.Location.Latitude ?? 0,
                    request.ReceiverClient?.Location.Longitude ?? 0,
                    usedTrain.Station.Location.Latitude,
                    usedTrain.Station.Location.Longitude) / 1000 / TrainSpeedKmh));

            newRoutePart.PlannedRoute.Actions.Add(
                new DirectReturnToBaseAction
                {
                    TrainId = usedTrain?.Id,
                    TrainName = usedTrain?.Name,
                    Locations = new List<Location>
                    {
                        request.SenderClient?.AssignedStation?.Location
                    },
                    StationId = request.SenderClient?.AssignedStation?.Id,
                    StationName = request.SenderClient?.AssignedStation?.Name,
                    Time = expectedArrivalTimeToBase
                }
            );

            // Add route to the list of created routes
            Context.Routes.Add(newRoute);
            Context.RouteParts.Add(newRoutePart);

            request.IsPlanned = true; // Mark request as planned
            request.RouteId = newRoute.Id; // Link request to the created route

            Context.RouteRequests.Update(request); // Mark as planned

            createdRoutes.Add(newRoute); // Add to the list of created routes
            usedTrains.Add(usedTrain); // Mark train as used
        }

        return createdRoutes; // Return the list of created routes
    }

    private List<Station> FindLongestCommonStationPath(List<Station> path1, List<Station> path2)
    {
        if (path1 == null || path2 == null || !path1.Any() || !path2.Any()) return new List<Station>();

        var m = path1.Count;
        var n = path2.Count;
        var lcsLengths = new int[m + 1, n + 1];

        // Build the LCS lengths table
        for (var i = 0; i <= m; i++)
        for (var j = 0; j <= n; j++)
            if (i == 0 || j == 0)
                lcsLengths[i, j] = 0;
            else if (path1[i - 1].Id == path2[j - 1].Id) // Compare station IDs
                lcsLengths[i, j] = lcsLengths[i - 1, j - 1] + 1;
            else
                lcsLengths[i, j] = Math.Max(lcsLengths[i - 1, j], lcsLengths[i, j - 1]);

        // Reconstruct the LCS path
        var longestCommonPath = new List<Station>();
        var idx = lcsLengths[m, n];

        var currentRow = m;
        var currentCol = n;
        while (currentRow > 0 && currentCol > 0)
            if (path1[currentRow - 1].Id == path2[currentCol - 1].Id)
            {
                // Prepend the station to maintain order
                longestCommonPath.Insert(0, path1[currentRow - 1]);
                idx--;
                currentRow--;
                currentCol--;
            }
            else if (lcsLengths[currentRow - 1, currentCol] > lcsLengths[currentRow, currentCol - 1])
            {
                currentRow--;
            }
            else
            {
                currentCol--;
            }

        return longestCommonPath;
    }

    private double GetDistanceBetweenStations(Station from, Station to)
    {
        var connections = Context.StationConnections
            .Where(sc => (sc.FromStationId == from.Id && sc.ToStationId == to.Id) ||
                         (sc.FromStationId == to.Id && sc.ToStationId == from.Id))
            .ToList();
        if (from == null || to == null) return 0;
        if (from.Id == to.Id) return 0;

        var connection = connections.FirstOrDefault(sc =>
            (sc.FromStationId == from.Id && sc.ToStationId == to.Id) ||
            (sc.FromStationId == to.Id && sc.ToStationId == from.Id));

        if (connection != null && connection.Distance > 0)
            return connection.Distance / 1000; // Convert meters to kilometers
        // Fallback to direct calculation if not in connections or distance is 0
        if (from.Location != null && to.Location != null)
        {
            Console.WriteLine($"Calculating distance between {from.Name} and {to.Name} directly.");
            return CalculateDistance(from.Location.Latitude, from.Location.Longitude, to.Location.Latitude,
                to.Location.Longitude) / 1000; // to km
        }

        return 0; // Should not happen if data is consistent
    }

    private bool IsPathContained(List<Station> subPath, List<Station> mainPath)
    {
        if (subPath == null || mainPath == null || !subPath.Any() || subPath.Count > mainPath.Count)
            return false;

        for (var i = 0; i <= mainPath.Count - subPath.Count; i++)
        {
            var match = true;
            for (var j = 0; j < subPath.Count; j++)
                if (mainPath[i + j].Id != subPath[j].Id)
                {
                    match = false;
                    break;
                }

            if (match) return true;
        }

        return false;
    }

    // Add this helper method to find stations near a route
    private List<Station> FindStationsNearRoute(List<Station> allStations, IEnumerable<IGeoCoordinate> routeCoordinates,
        double maxDistanceMeters = 850)
    {
        var nearbyStations = new List<Station>();
        foreach (var station in allStations)
        {
            // Skip stations without location
            if (station.Location == null) continue;

            // Check if station is within maxDistanceMeters of any point on the route
            foreach (var coordinate in routeCoordinates)
            {
                var distance = CalculateDistance(
                    station.Location.Latitude, station.Location.Longitude,
                    coordinate.Latitude, coordinate.Longitude);

                if (distance <= maxDistanceMeters)
                {
                    nearbyStations.Add(station);
                    break; // Station is near the route, no need to check more points
                }
            }
        }

        Station closestToStart = null;
        Station closestToFinish = null;

        foreach (var station in nearbyStations)
        {
            // Find the closest station to the start of the route
            if (closestToStart == null || CalculateDistance(
                station.Location.Latitude, station.Location.Longitude,
                routeCoordinates.First().Latitude, routeCoordinates.First().Longitude) <
                CalculateDistance(closestToStart.Location.Latitude, closestToStart.Location.Longitude,
                    routeCoordinates.First().Latitude, routeCoordinates.First().Longitude))
            {
                closestToStart = station;
            }
            // Find the closest station to the end of the route
            if (closestToFinish == null || CalculateDistance(
                station.Location.Latitude, station.Location.Longitude,
                routeCoordinates.Last().Latitude, routeCoordinates.Last().Longitude) <
                CalculateDistance(closestToFinish.Location.Latitude, closestToFinish.Location.Longitude,
                    routeCoordinates.Last().Latitude, routeCoordinates.Last().Longitude))
            {
                closestToFinish = station;
            }
        }

        // Sort stations by distance from the start of the route using station connections.
        nearbyStations = nearbyStations.OrderBy(station => GetDistanceBetweenStations(closestToStart, station)).ToList();
        

        if (!nearbyStations.Any())
        {
            Console.WriteLine("No nearby stations found within the specified distance.");
        }

        return nearbyStations;
    }

    // Helper method to calculate distance between two points in meters
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}
