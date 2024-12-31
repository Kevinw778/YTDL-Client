using Microsoft.AspNetCore.SignalR;

public class SignalRHub : Hub
{
    public async Task SendUpdate(string downloadId, string status, string progress)
    {
        try
        {
            await Clients.All.SendAsync("ReceiveUpdate", downloadId, status, progress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendUpdate: {ex.Message}");
            throw;
        }
    }
}
