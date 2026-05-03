namespace AdbClient.Service.Services;

public interface ITrackerListGrabber
{
    Task<string[]> GetTrackers();
}