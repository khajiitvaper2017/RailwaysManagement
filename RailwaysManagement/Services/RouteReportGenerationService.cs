using Microsoft.EntityFrameworkCore;
using RailwaysManagement.DbModels;
using RailwaysManagement.Models;
using System.IO;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;

namespace RailwaysManagement.Services;

public class RouteReportGenerationService
{
    private readonly IDbContextFactory<RailwaysDbContext> _dbContextFactory;

    public RouteReportGenerationService(IDbContextFactory<RailwaysDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<byte[]> GenerateRouteReport(string routeId)
    {
        if (string.IsNullOrEmpty(routeId)) throw new ArgumentNullException(nameof(routeId));

        using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Fetch the route with all related data
        var route = await context.Routes
            .Include(r => r.RailRouteRouteParts)
                .ThenInclude(rrp => rrp.RoutePart)
            .FirstOrDefaultAsync(r => r.Id == routeId && !r.IsDeleted);

        if (route == null) throw new Exception($"Маршрут з ID {routeId} не знайдено");

        // Fetch all the related data from the database
        var allRoutes = await context.Routes.Where(r => !r.IsDeleted).ToListAsync();
        var allStations = await context.Stations.ToListAsync();
        var stationNames = allStations.ToDictionary(s => s.Id, s => s.Name);

        // Get route parts
        var routeParts = await context.RouteParts
            .Where(rp => rp.RailRouteRouteParts.Any(rrp => rrp.RailRouteId == routeId))
            .Include(rp => rp.Train)
            .ToListAsync();

        // Get route stations
        var routeStations = new List<RoutePartStation>();
        foreach (var part in routeParts)
        {
            var partStations = await context.RouteStations
                .Where(rs => rs.RoutePartId == part.Id)
                .ToListAsync();
            routeStations.AddRange(partStations);
        }

        // Get trains for the route
        var routeTrains = routeParts.Select(rp => rp.Train).Where(t => t != null).Distinct().ToList();

        // Get client information
        var routeRequest = await context.RouteRequests
            .Include(rr => rr.SenderClient)
            .Include(rr => rr.ReceiverClient)
            .FirstOrDefaultAsync(rr => rr.RouteId == routeId);

        var senderName = routeRequest?.SenderClient?.Name ?? "-";
        var receiverName = routeRequest?.ReceiverClient?.Name ?? "-";

        // Get cargos for the route
        var cargos = new List<Cargo>();
        if (routeRequest != null)
        {
            cargos = await context.Cargos
                .Where(c => c.RouteRequestId == routeRequest.Id)
                .ToListAsync();
        }

        // Get the planned route
        var plannedRoute = routeParts.FirstOrDefault()?.PlannedRoute;

        // Create the Word document.
        using WordDocument document = new WordDocument();
        
        // Create a document section.
        IWSection section = document.AddSection();
        
        // 1. Add the report title.
        IWParagraph titleParagraph = section.AddParagraph();
        titleParagraph.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
        IWTextRange titleText = titleParagraph.AppendText("Звіт про маршрут");
        titleText.CharacterFormat.Bold = true;
        titleText.CharacterFormat.FontSize = 16;
        
        // Add general route information.
        section.AddParagraph().AppendText($"Маршрут: {route.Name ?? $"Маршрут №{allRoutes.IndexOf(route) + 1}"}");
        section.AddParagraph().AppendText($"Згенеровано: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}");

        // Add space between sections
        section.AddParagraph();

        // 2. Add the route overview.
        section.AddParagraph().AppendText("Основні параметри маршруту").CharacterFormat.Bold = true;
        
        // Create a table with route details.
        IWTable overviewTable = section.AddTable();
        overviewTable.ResetCells(4, 2);
        overviewTable.TableFormat.Borders.BorderType = BorderStyle.Single;
        
        overviewTable[0, 0].AddParagraph().AppendText("Загальна відстань:");
        overviewTable[0, 1].AddParagraph().AppendText(GetRouteDistance(route, plannedRoute) + " км");
        
        overviewTable[1, 0].AddParagraph().AppendText("Орієнтовна тривалість:");
        overviewTable[1, 1].AddParagraph().AppendText(GetRouteDuration(route));
        
        overviewTable[2, 0].AddParagraph().AppendText("Планований початок:");
        overviewTable[2, 1].AddParagraph().AppendText(
            route.RailRouteRouteParts.FirstOrDefault()?.RoutePart.PlannedRoute?.Actions.FirstOrDefault()?.Time.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "Невідомо");
        
        overviewTable[3, 0].AddParagraph().AppendText("Плановане завершення:");
        overviewTable[3, 1].AddParagraph().AppendText(
            route.RailRouteRouteParts.LastOrDefault()?.RoutePart.PlannedRoute?.Actions.LastOrDefault()?.Time.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "Невідомо");

        // Add space between sections
        section.AddParagraph();

        // 3. Add client information.
        section.AddParagraph().AppendText("Інформація про клієнтів").CharacterFormat.Bold = true;
        
        IWTable clientsTable = section.AddTable();
        clientsTable.ResetCells(2, 2);
        clientsTable[0, 0].AddParagraph().AppendText("Відправник:");
        clientsTable[0, 1].AddParagraph().AppendText(senderName);
        clientsTable[1, 0].AddParagraph().AppendText("Отримувач:");
        clientsTable[1, 1].AddParagraph().AppendText(receiverName);

        // Add space between sections
        section.AddParagraph();

        // 4. Add train information.
        section.AddParagraph().AppendText("Призначені потяги").CharacterFormat.Bold = true;
        
        if (routeTrains.Any())
        {
            foreach (var train in routeTrains)
            {
                section.AddParagraph().AppendText($"Потяг: {train.Name}").CharacterFormat.Bold = true;
                
                IWTable trainTable = section.AddTable();
                trainTable.ResetCells(3, 2);
                trainTable[0, 0].AddParagraph().AppendText("Макс. кількість вагонів:");
                trainTable[0, 1].AddParagraph().AppendText(train.MaxCarsCount.ToString());
                
                trainTable[1, 0].AddParagraph().AppendText("Поточна станція:");
                trainTable[1, 1].AddParagraph().AppendText(GetStationName(train.StationId, stationNames));
                
                trainTable[2, 0].AddParagraph().AppendText("Статус:");
                trainTable[2, 1].AddParagraph().AppendText(GetTrainStatus(train, routeStations));
            }
        }

        // Add space between sections
        section.AddParagraph();

        // 5. Add station information.
        section.AddParagraph().AppendText("Станції маршруту").CharacterFormat.Bold = true;
        
        if (routeStations.Any())
        {
            var orderedStations = routeStations.OrderBy(s => s.Order).ToList();
            
            for (int i = 0; i < orderedStations.Count; i++)
            {
                var station = orderedStations[i];
                var stationName = GetStationName(station.StationId, stationNames);
                
                IWParagraph stationPara = section.AddParagraph();
                stationPara.AppendText($"{i + 1}. {stationName}").CharacterFormat.Bold = true;
                
                if (station.ExpectedArrival.HasValue)
                {
                    section.AddParagraph().AppendText($"  Прибуття: {station.ExpectedArrival.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")}");
                }
                
                if (station.ExpectedDeparture.HasValue)
                {
                    section.AddParagraph().AppendText($"  Відправлення: {station.ExpectedDeparture.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")}");
                }
            }
        }

        // Add space between sections
        section.AddParagraph();

        // 6. Add cargo information.
        section.AddParagraph().AppendText("Деталі вантажу").CharacterFormat.Bold = true;
        
        if (cargos.Any())
        {
            section.AddParagraph().AppendText($"Загальна кількість вагонів: {cargos.Sum(c => c.WagonsCount)}");
            
            // Create a table with cargo details.
            int totalWagons = cargos.Sum(c => c.WagonsCount);
            IWTable cargoTable = section.AddTable();
            cargoTable.ResetCells(totalWagons + 1, 4);
            cargoTable.TableFormat.Borders.BorderType = BorderStyle.Single;
            
            // Add table headers.
            cargoTable[0, 0].AddParagraph().AppendText("№ вагона").CharacterFormat.Bold = true;
            cargoTable[0, 1].AddParagraph().AppendText("Назва вантажу").CharacterFormat.Bold = true;
            cargoTable[0, 2].AddParagraph().AppendText("Статус").CharacterFormat.Bold = true;
            cargoTable[0, 3].AddParagraph().AppendText("Дата створення").CharacterFormat.Bold = true;
            
            int row = 1;
            foreach (var cargo in cargos)
            {
                for (int i = 0; i < cargo.WagonsCount; i++)
                {
                    cargoTable[row, 0].AddParagraph().AppendText((i + 1).ToString());
                    cargoTable[row, 1].AddParagraph().AppendText(cargo.Name ?? $"Вантаж #{cargo.Id.Substring(0, 4)}");
                    cargoTable[row, 2].AddParagraph().AppendText(GetCargoStatus(cargo));
                    cargoTable[row, 3].AddParagraph().AppendText(cargo.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy"));
                    row++;
                }
            }
        }

        // Add space between sections
        section.AddParagraph();

        // 7. Add the action timeline.
        section.AddParagraph().AppendText("Хронологія дій маршруту").CharacterFormat.Bold = true;
        
        if (plannedRoute?.Actions != null)
        {
            foreach (var action in plannedRoute.Actions)
            {
                section.AddParagraph().AppendText($"• {action.Time?.ToLocalTime().ToString("dd.MM.yyyy HH:mm")}: {action.Description}");
            }
        }

        // Save the document to a memory stream.
        MemoryStream stream = new MemoryStream();
        document.Save(stream, FormatType.Docx);
        document.Close();

        // Return the document as byte array
        return stream.ToArray();
    }

    private string GetStationName(string stationId, Dictionary<string, string> stationNames)
    {
        return stationNames.TryGetValue(stationId, out var name) ? name : stationId;
    }

    private string GetRouteDistance(RailRoute route, PlannedRoute plannedRoute)
    {
        if (plannedRoute?.Actions == null || !plannedRoute.Actions.Any())
            return "Невідомо";

        // Get all locations from all actions
        var allLocations = GetAllRouteLocations(plannedRoute);

        if (allLocations.Count < 2)
            return "Невідомо";

        // Calculate rough distance in kilometers
        double totalDistance = 0;

        for (var i = 0; i < allLocations.Count - 1; i++)
        {
            var start = allLocations[i];
            var end = allLocations[i + 1];
            totalDistance += CalculateDistance(start.Latitude, start.Longitude, end.Latitude, end.Longitude);
        }

        return Math.Round(totalDistance, 1).ToString();
    }

    private List<Location> GetAllRouteLocations(PlannedRoute plannedRoute)
    {
        if (plannedRoute?.Actions == null || !plannedRoute.Actions.Any())
            return new List<Location>();

        var allLocations = new List<Location>();
        foreach (var action in plannedRoute.Actions)
        {
            if (action.Locations != null && action.Locations.Any())
            {
                allLocations.AddRange(action.Locations);
            }
        }

        return allLocations;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert degrees to radians
        lat1 = lat1 * Math.PI / 180.0;
        lon1 = lon1 * Math.PI / 180.0;
        lat2 = lat2 * Math.PI / 180.0;
        lon2 = lon2 * Math.PI / 180.0;

        // Haversine formula
        var dlon = lon2 - lon1;
        var dlat = lat2 - lat1;
        var a = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2);
        var c = 2 * Math.Asin(Math.Sqrt(a));

        // Radius of earth in kilometers
        double r = 6371;

        return c * r;
    }

    private string GetRouteDuration(RailRoute route)
    {
        if (route == null)
            return "Невідомо";
        if (!route.RailRouteRouteParts.Any())
            return "Невідомо";

        var firstAction = route.RailRouteRouteParts.First().RoutePart.PlannedRoute?.Actions.FirstOrDefault();
        var lastAction = route.RailRouteRouteParts.Last().RoutePart.PlannedRoute?.Actions.LastOrDefault();

        if (firstAction == null || lastAction == null)
            return "Невідомо";

        var duration = lastAction.Time - firstAction.Time;
        if (duration is null || duration < TimeSpan.Zero)
            return "Невідомо";
        
        return FormatDuration(duration.Value);
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.Days > 0)
            return $"{duration.Days}д {duration.Hours}г {duration.Minutes}хв";
        if (duration.Hours > 0)
            return $"{duration.Hours}г {duration.Minutes}хв";
        return $"{duration.Minutes}хв";
    }

    private string GetTrainStatus(Train train, List<RoutePartStation> stations)
    {
        if (!stations.Any())
            return "Очікує відправлення";

        var currentStation = stations.FirstOrDefault(s => s.StationId == train.StationId);
        if (currentStation == null)
            return "Готується до маршруту";

        var firstStation = stations.MinBy(s => s.Order);
        var lastStation = stations.MaxBy(s => s.Order);

        if (currentStation.StationId == firstStation?.StationId)
            return "Готовий до відправлення";
        if (currentStation.StationId == lastStation?.StationId)
            return "Прибув до кінцевої станції";
        return "В дорозі";
    }

    private string GetCargoStatus(Cargo cargo)
    {
        return cargo.RouteRequestId != null ? "Призначено до перевезення" : "Очікує призначення";
    }
}