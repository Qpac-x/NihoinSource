using System;

using static Nihon.Static.Native;

namespace Nihon.MemoryFramework;

public class RtTarget : IDisposable, IEquatable<RtTarget>
{
    internal IntPtr Handle;

    internal RtTarget(int Id) =>
        this.Id = Id;

    public int Id { get; internal set; }
    public bool IsOpen { get; private set; }

    public RtTarget Open()
    {
        if (this.IsOpen == true)
            return this;

        this.Handle = OpenProcess(
            ProcessAccess.All,
            false,
            this.Id);

        this.IsOpen = this.Handle != InvalidHandle;

        return this.IsOpen ? this : null;
    }

    public RtModule MainModule { get; internal set; }

    public string Path => this.MainModule.Path;
    public string Name => this.MainModule.Name;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool Equals(RtTarget Other) =>
        Other != null && this.Id == Other.Id;

    public bool IsDebuggerPresent
    {
        get
        {
            bool Flag = false;
            CheckRemoteDebuggerPresent(this.Handle, ref Flag);

            return Flag;
        }
    }

    protected virtual void Dispose(bool Disposing)
    {
        bool Disposed = false;
        if (Disposed)
            return;

        Disposed = true;

        if (Disposing)
            CloseHandle(this.Handle);
    }

    ~RtTarget()
    {
        this.Dispose(false);
    }
}
