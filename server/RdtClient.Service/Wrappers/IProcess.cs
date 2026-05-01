using System.Diagnostics;

namespace RdtClient.Service.Wrappers;

public interface IProcess : IDisposable
{
    event EventHandler<string?>? OutputDataReceived;
    event EventHandler<string?>? ErrorDataReceived;

    public ProcessStartInfo StartInfo { get; set; }

    void BeginOutputReadLine();
    void BeginErrorReadLine();
    bool WaitForExit(int milliseconds);
    void Start();
}
