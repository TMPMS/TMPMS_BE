using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using TMPMS.Services;

namespace TMPMS.Hubs
{
    public class TrackingHub : Hub
    {
        private readonly TrackingSimulationService _simulationService;

        public TrackingHub(TrackingSimulationService simulationService)
        {
            _simulationService = simulationService;
        }

        public async Task JoinOrderTrackingGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, orderId);
            
            if (int.TryParse(orderId, out int id))
            {
                _simulationService.StartSimulation(id);
            }
        }

        public async Task LeaveOrderTrackingGroup(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, orderId);
        }
    }
}
