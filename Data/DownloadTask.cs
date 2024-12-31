using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

public class DownloadTask
{
    public string Id { get; }
    public string Url { get; }
    public string Title { get; set; } = "Fetching title...";
    public string Status { get; set; } = "Queued";
    public string Progress { get; set; } = "0%";

    private readonly IHubContext<SignalRHub> _hubContext;
    private Process? _process;

    public DownloadTask(string id, string url, IHubContext<SignalRHub> hubContext)
    {
        Id = id;
        Url = url;
        _hubContext = hubContext;
    }

    public void Start()
    {
        Status = "Starting";
        NotifyClient();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, YoutubeDlService.ExecutablePath),
                Arguments = $"-o \"downloads/%(title)s [%(id)s].%(ext)s\" {Url}", // Save to 'downloads/' folder
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.OutputDataReceived += async (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.WriteLine($"Output: {args.Data}"); // Log output
                if (args.Data.Contains("Destination:"))
                {
                    var filePath = args.Data.Split("Destination:")[1].Trim();
                    Console.WriteLine($"File saved to: {filePath}");
                }
                Progress = ParseProgress(args.Data);
                Status = "Downloading";
                await NotifyClient();
            }
        };

        _process.ErrorDataReceived += async (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                Console.WriteLine($"Error: {args.Data}"); // Log the error
                Status = "Error: " + args.Data;
                await NotifyClient();
            }
        };

        _process.Exited += async (sender, args) =>
        {
            Status = _process.ExitCode == 0 ? "Completed" : "Failed";
            Progress = "100%";
            await NotifyClient();
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private string ParseProgress(string data)
    {
        Console.WriteLine($"Raw Output: {data}");
        var match = Regex.Match(data, @"(\d+\.\d+)%");
        return match.Success ? match.Groups[1].Value + "%" : Progress;
    }

    private async Task NotifyClient()
    {
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate", Id, Status, Progress);
    }
}