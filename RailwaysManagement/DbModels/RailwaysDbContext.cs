using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RailwaysManagement.Models;
using RailwaysManagement.Services;

namespace RailwaysManagement.DbModels;

public class RailwaysDbContext : IdentityDbContext<RailwaysManagementUser>
{
    private readonly OpenRailRoutingService _routingService;

    public RailwaysDbContext(DbContextOptions<RailwaysDbContext> options, GeoJsonService geoJsonService,
        OpenRailRoutingService routingService)
        : base(options)
    {
        _routingService = routingService;
    }

    public DbSet<RailwaysManagementUser> RailwaysManagementUsers { get; set; }
    public DbSet<Station> Stations { get; set; }
    public DbSet<Train> Trains { get; set; }
    public DbSet<Cargo> Cargos { get; set; }
    public DbSet<RoutePartCargo> RoutePartCargos { get; set; }
    public DbSet<RailRoute> Routes { get; set; }
    public DbSet<RoutePartStation> RouteStations { get; set; }
    public DbSet<RailRouteRoutePart> RailRouteRouteParts { get; set; }
    public DbSet<RouteRequest> RouteRequests { get; set; }
    public DbSet<RoutePart> RouteParts { get; set; }
    public DbSet<StationConnection> StationConnections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RailwaysManagementUser>(b =>
        {
            // Each User can have many entries in the UserRole join table
            b.HasMany(e => e.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
            // optional location. not required
            b.OwnsOne(e => e.Location);
            
            // Add source station relationship
            b.HasOne(e => e.AssignedStation)
                .WithMany()
                .HasForeignKey(e => e.AssignedStationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RailwaysManagementUser>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Station>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Train>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Cargo>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RailRoute>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RouteRequest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StationConnection>().HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<RoutePart>()
            .Property(r => r.PlannedRoute)
            .HasConversion(
                v => JsonConvert.SerializeObject(v, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = { new NewtonsoftRouteActionConverter() }
                }),
                v => JsonConvert.DeserializeObject<PlannedRoute>(v, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = { new NewtonsoftRouteActionConverter() }
                }));

        modelBuilder.Entity<Cargo>()
            .HasOne(c => c.RouteRequest)
            .WithMany(r => r.Cargos)
            .HasForeignKey(c => c.RouteRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Train>()
            .HasOne(t => t.Station)
            .WithMany(s => s.Trains)
            .HasForeignKey(t => t.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Explicitly configure RouteRequest.ReceiverClient relationship
        modelBuilder.Entity<RouteRequest>()
            .HasOne(rr => rr.ReceiverClient)
            .WithMany()
            .HasForeignKey(rr => rr.ReceiverClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Explicitly configure RouteRequest.SenderClient relationship
        modelBuilder.Entity<RouteRequest>()
            .HasOne(rr => rr.SenderClient)
            .WithMany()
            .HasForeignKey(rr => rr.SenderClientId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<RailRouteRoutePart>()
            .HasKey(rrr => new { rrr.RailRouteId, rrr.RoutePartId });

        // Configure the many-to-many relationship between RailRoute and RoutePart
        // through the RailRouteRoutePart junction table
        modelBuilder.Entity<RailRouteRoutePart>()
            .HasOne(rrr => rrr.RailRoute)
            .WithMany(rr => rr.RailRouteRouteParts) // References the collection in RailRoute.cs
            .HasForeignKey(rrr => rrr.RailRouteId);

        modelBuilder.Entity<RailRouteRoutePart>()
            .HasOne(rrr => rrr.RoutePart)
            .WithMany(rp => rp.RailRouteRouteParts) // References the collection in RoutePart.cs
            .HasForeignKey(rrr => rrr.RoutePartId);


        modelBuilder.Entity<RoutePartStation>()
            .HasKey(rs => new { rs.RoutePartId, rs.StationId });

        modelBuilder.Entity<RoutePartStation>()
            .HasOne(rs => rs.RoutePart)
            .WithMany(r => r.RouteStations)
            .HasForeignKey(rs => rs.RoutePartId)
            .IsRequired(false);

        modelBuilder.Entity<RoutePartStation>()
            .HasOne(rs => rs.Station)
            .WithMany(s => s.RouteStations)
            .HasForeignKey(rs => rs.StationId)
            .IsRequired(false);

        modelBuilder.Entity<StationConnection>()
            .HasKey(sc => new { sc.FromStationId, sc.ToStationId });

        modelBuilder.Entity<StationConnection>()
            .HasOne(sc => sc.FromStation)
            .WithMany(s => s.OutgoingConnections)
            .HasForeignKey(sc => sc.FromStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StationConnection>()
            .HasOne(sc => sc.ToStation)
            .WithMany(s => s.IncomingConnections)
            .HasForeignKey(sc => sc.ToStationId)
            .OnDelete(DeleteBehavior.Restrict); // Prevents multiple cascade paths

        modelBuilder.Entity<RoutePart>()
            .HasOne(rp => rp.Train)
            .WithMany(t => t.RouteParts)
            .HasForeignKey(rp => rp.TrainId)
            .IsRequired(false);

        modelBuilder.Entity<RoutePartCargo>()
            .HasKey(rpc => new { rpc.RoutePartId, rpc.CargoId });
        modelBuilder.Entity<RoutePartCargo>()
            .HasOne(rpc => rpc.RoutePart)
            .WithMany(rp => rp.RoutePartCargos)
            .HasForeignKey(rpc => rpc.RoutePartId)
            .IsRequired(false);
    }

    private void ApplyAuditInformation()
    {
        var auditableEntries = ChangeTracker.Entries<IAuditable>().ToList();
        foreach (var entry in auditableEntries)
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
            }
    }

    private void ApplySoftDelete()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable)
            .ToList();
        foreach (var entry in entries)
        {
            entry.State = EntityState.Modified;
            ((ISoftDeletable)entry.Entity).IsDeleted = true;
            if (entry.Entity is IAuditable auditable) auditable.LastModifiedAtUtc = DateTime.UtcNow;
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        ApplySoftDelete();
        return base.SaveChanges();
    }

    public async Task UpdateStationConnectionsDistances()
    {
        // 1) Load all stations and existing connections (where Distance > 0) up front
        var allStations = await Stations.ToListAsync();

        var existingPairs = await StationConnections
            .Where(sc => sc.Distance > 0)
            .Select(sc => new { sc.FromStationId, sc.ToStationId })
            .ToListAsync();

        var existingSet = new HashSet<(string from, string to)>(
            existingPairs.Select(x => (x.FromStationId, x.ToStationId))
        );

        // 2) Generate only unique pairs (i < j) and skip those already done
        var pairsToCompute = allStations
            .SelectMany((sa, i) => allStations
                .Skip(i + 1)
                .Select(sb => (sa, sb)))
            .Where(pair =>
                !existingSet.Contains((pair.sa.Id, pair.sb.Id)) &&
                !existingSet.Contains((pair.sb.Id, pair.sa.Id))
            )
            .ToList();

        if (!pairsToCompute.Any())
        {
            Console.WriteLine("No new station-station distances to compute.");
            return;
        }

        // 3) Throttle parallel requests
        const int MaxConcurrency = 8;
        var semaphore = new SemaphoreSlim(MaxConcurrency);

        var tasks = pairsToCompute.Select(async pair =>
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                Console.WriteLine($"Calculating distance between {pair.sa.Name} and {pair.sb.Name}");

                double distance;
                try
                {
                    var result = await _routingService
                        .FindRouteAsync(pair.sa.Location, pair.sb.Location)
                        .ConfigureAwait(false);
                    distance = result.Distance;
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Error calculating distance between " +
                        $"{pair.sa.Name} and {pair.sb.Name}: {e.Message}");
                    return (pair.sa.Id, pair.sb.Id, distance: -1.0);
                }

                return (pair.sa.Id, pair.sb.Id, distance);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        // 4) Await all in parallel
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 5) Build new connections (both directions) and filter out failures/zero distances
        var newConnections = results
            .Where(r => r.distance > 0)
            .SelectMany(r => new[]
            {
                new StationConnection
                {
                    FromStationId = r.Item1,
                    ToStationId   = r.Item2,
                    Distance      = r.distance
                },
                new StationConnection
                {
                    FromStationId = r.Item2,
                    ToStationId   = r.Item1,
                    Distance      = r.distance
                }
            })
            .ToList();

        if (newConnections.Any())
        {
            StationConnections.AddRange(newConnections);
            await SaveChangesAsync().ConfigureAwait(false);
            Console.WriteLine($"Added {newConnections.Count} new station-connection records.");
        }
        else
        {
            Console.WriteLine("No valid distances returned; nothing to save.");
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        ApplySoftDelete();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignClosestStationsToUsers()
    {
        // calc a direct line distance and assign the closest station to each user that does not have an assigned station
        var usersWithoutStation = await RailwaysManagementUsers
            .Where(u => u.AssignedStation==null)
            .Where(u => u.Location != null)
            .ToListAsync();
        if (!usersWithoutStation.Any())
            return;
        var stations = await Stations.ToListAsync();

        foreach (var user in usersWithoutStation)
        {
            var closestStation = stations.MinBy(s => GeoUtils.CalculateDistance(user.Location, s.Location));
            if (closestStation == null) continue;
            user.AssignedStationId = closestStation.Id;
        }
        await SaveChangesAsync();
    }
}