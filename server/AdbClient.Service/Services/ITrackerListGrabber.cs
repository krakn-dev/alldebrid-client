namespace RdtClient.Service.Services;

public interface ITrackerListGrabber
{
    Task<string[]> GetTrackers();
}