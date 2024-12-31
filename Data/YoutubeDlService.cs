using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

public class YoutubeDlService
{
    private readonly ConcurrentDictionary<string, DownloadTask> _tasks = new();
    private readonly IHubContext<SignalRHub> _hubContext;

    public const string ExecutablePath = "Executables/yt.exe";

    public YoutubeDlService(IHubContext<SignalRHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public string StartDownload(string url)
    {
        var taskId = Guid.NewGuid().ToString();
        var task = new DownloadTask(taskId, url, _hubContext);
        _tasks[taskId] = task;
        task.Start();
        return taskId;
    }

    public List<DownloadTask> GetTasks()
    {
        return _tasks.Values.ToList();
    }

    public DownloadTask? GetTask(string id)
    {
        return _tasks.TryGetValue(id, out var task) ? task : null;
    }
}
