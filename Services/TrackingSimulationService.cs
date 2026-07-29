using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPMS.Hubs;

namespace TMPMS.Services
{
    public class TrackingSimulationService : BackgroundService
    {
        private readonly IHubContext<TrackingHub> _hubContext;
        private readonly ILogger<TrackingSimulationService> _logger;
        
        // Coordinates definition
        private static readonly Coords StoreCoords = new() { Lat = 10.76008, Lng = 106.68220 };
        private static readonly Coords UserCoords = new() { Lat = 10.75784, Lng = 106.67102 };
        private static readonly List<Coords> Waypoints = GenerateWaypoints(StoreCoords, UserCoords, 15);

        private readonly ConcurrentDictionary<int, SimState> _activeSimulations = new();

        public TrackingSimulationService(IHubContext<TrackingHub> hubContext, ILogger<TrackingSimulationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public class Coords
        {
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        public class SimState
        {
            public int CurrentStep { get; set; }
            public string Status { get; set; } = "Preparing";
            public object Shipper { get; set; } = new
            {
                Name = "Nguyễn Minh Hải",
                Phone = "0912.345.678",
                Plate = "59-A1 789.65"
            };
        }

        private static List<Coords> GenerateWaypoints(Coords start, Coords end, int steps)
        {
            var list = new List<Coords>();
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                list.Add(new Coords
                {
                    Lat = start.Lat + (end.Lat - start.Lat) * t,
                    Lng = start.Lng + (end.Lng - start.Lng) * t
                });
            }
            return list;
        }

        public void StartSimulation(int orderId)
        {
            if (_activeSimulations.ContainsKey(orderId))
            {
                _logger.LogInformation($"Simulation for order {orderId} is already running.");
                return;
            }

            var state = new SimState { CurrentStep = 0, Status = "Preparing" };
            _activeSimulations[orderId] = state;
            _logger.LogInformation($"Started simulation for order {orderId}.");
        }

        public void CancelSimulation(int orderId)
        {
            _activeSimulations.TryRemove(orderId, out _);
            _logger.LogInformation($"Cancelled simulation for order {orderId}.");
        }

        public void UpdateSimulationFromWebhook(int orderId, string status, Coords? customCoords = null, object? customShipper = null)
        {
            if (_activeSimulations.TryGetValue(orderId, out var state))
            {
                state.Status = status;
                if (customShipper != null) state.Shipper = customShipper;
                
                // If status is Arrived or Delivered, remove from background loop
                if (status == "Arrived" || status == "Delivered")
                {
                    _activeSimulations.TryRemove(orderId, out _);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var pair in _activeSimulations)
                    {
                        int orderId = pair.Key;
                        SimState state = pair.Value;

                        state.CurrentStep++;
                        if (state.CurrentStep == 1)
                        {
                            state.Status = "Shipping";
                        }

                        if (state.CurrentStep >= Waypoints.Count)
                        {
                            state.Status = "Arrived";
                            
                            // Send final update
                            await _hubContext.Clients.Group(orderId.ToString()).SendAsync("ReceiveTrackingUpdate", new
                            {
                                orderId = orderId,
                                status = state.Status,
                                shipper = state.Shipper,
                                coords = Waypoints[^1]
                            }, stoppingToken);

                            _activeSimulations.TryRemove(orderId, out _);
                            _logger.LogInformation($"Order {orderId} reached its destination.");
                            continue;
                        }

                        // Broadcast coordinate updates to clients in this group
                        await _hubContext.Clients.Group(orderId.ToString()).SendAsync("ReceiveTrackingUpdate", new
                        {
                            orderId = orderId,
                            status = state.Status,
                            shipper = state.Shipper,
                            coords = Waypoints[state.CurrentStep]
                        }, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in tracking simulation loop.");
                }

                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
