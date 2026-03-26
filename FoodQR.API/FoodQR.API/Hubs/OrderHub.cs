using Microsoft.AspNetCore.SignalR;

namespace FoodQR.API.Hubs
{
    public class OrderHub : Hub
    {
        /// <summary>
        /// Client gọi method này để join vào group (kitchen, staff, table_5, etc.)
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Client gọi method này để rời group
        /// </summary>
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
