namespace RdtClient.Service.Services.Downloaders;

public class DownloadCompleteEventArgs
{
    public string? Error { get; set; }
}

public class DownloadProgressEventArgs
{
    public long Speed { get; set; }
    public long BytesDone { get; set; }
    public long BytesTotal { get; set; }
}

public interface IDownloader
{
    event EventHandler<DownloadCompleteEventArgs>? DownloadComplete;
    event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
    Task<string> Download();
    Task Cancel();
    Task Pause();
    Task Resume();
}