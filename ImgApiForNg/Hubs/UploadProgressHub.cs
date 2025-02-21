using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ImgApiForNg.Hubs
{
    public class UploadProgressHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            await Clients.Client(connectionId).SendAsync("ReceiveConnectionId", connectionId);
            await base.OnConnectedAsync();
        }
        public async Task SendProgress(string connectionId, int progress, string type)
        {
            if (type == "upload")
            {
                await Clients.Client(connectionId).SendAsync("UploadProgress", progress);
            }
            else if (type == "download")
            {
                await Clients.Client(connectionId).SendAsync("DownloadProgress", progress);
            }
        }
    }
}