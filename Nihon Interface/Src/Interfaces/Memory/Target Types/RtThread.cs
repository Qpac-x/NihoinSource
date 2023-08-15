using System;

using static Nihon.Static.Native;

namespace Nihon.MemoryFramework;

public class RtThread : IDisposable, IEquatable<RtThread>
{
    public readonly int Id;
    public readonly RtTarget Target;

    bool Disposed;
    IntPtr Handle;

    internal RtThread(RtTarget Target, int ThreadId)
    {
        this.Target = Target;
        this.Id = ThreadId;
    }

    public bool IsOpen { get; private set; }

    public bool IsFrozen
    {
        get
        {
            return SuspendThread(this.Handle) == Success &&
                ResumeThread(this.Handle) == Success;
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool Equals(RtThread Other) =>
        Other != null && Other.Id == this.Id && Other.Target == this.Target;

    public RtThread Open()
    {
        if (this.IsOpen)
            return this;

        this.Handle = OpenThread(
            ThreadAccess.All,
            false,
            this.Id);

        this.IsOpen = this.Handle != InvalidHandle;

        return this.IsOpen ? this : null;
    }

    public bool Close() =>
        !this.IsOpen || CloseHandle(this.Handle) == true;

    public bool Freeze() =>
        SuspendThread(this.Handle) == Success;

    public ThreadContext GetContext()
    {
        ThreadContext Ctx = new ThreadContext { Flags = ContextFlags.All };

        return GetThreadContext(this.Handle, ref Ctx) != true ? new ThreadContext { Flags = ContextFlags.All } : Ctx;
    }

    public bool SetCtx(ThreadContext Ctx) =>
        SetThreadContext(this.Handle, ref Ctx) == true;

    public bool SetEip(IntPtr Eip)
    {
        ThreadContext Ctx = this.GetContext();
        Ctx.Eip = (uint)Eip;
        return this.SetCtx(Ctx);
    }

    protected virtual void Dispose(bool Disposing)
    {
        if (this.Disposed)
            return;

        this.Disposed = true;

        if (Disposing && this.IsOpen)
            this.Close();
    }

    ~RtThread()
    {
        this.Dispose(false);
    }
}
