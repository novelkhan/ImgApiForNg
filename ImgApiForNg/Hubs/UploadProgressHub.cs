using Microsoft.AspNetCore.SignalR;
using System;
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


        public async Task SendUploadProgress(string connectionId, int progress)
        {
            await Clients.Client(connectionId).SendAsync("UploadProgress", progress);
        }

        public async Task SendDownloadProgress(string connectionId, int progress)
        {
            await Clients.Client(connectionId).SendAsync("DownloadProgress", progress);
        }

        //public async Task SendProgress(string connectionId, int progress, string type)
        //{
        //    if (type == "upload")
        //    {
        //        Console.WriteLine($"Sending Upload Progress: {progress}% to {connectionId}"); // লগ যোগ করুন
        //        await Clients.Client(connectionId).SendAsync("UploadProgress", progress);
        //    }
        //    else if (type == "download")
        //    {
        //        Console.WriteLine($"Sending Download Progress: {progress}% to {connectionId}"); // লগ যোগ করুন
        //        await Clients.Client(connectionId).SendAsync("DownloadProgress", progress);
        //    }
        //}
    }
}