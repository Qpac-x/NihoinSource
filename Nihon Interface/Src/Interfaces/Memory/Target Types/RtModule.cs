using System;

namespace Nihon.MemoryFramework;

public class RtModule : IEquatable<RtModule>
{
    public RtTarget Target { get; }

    public readonly IntPtr Base;
    public readonly int Size;

    internal RtModule(RtTarget Target, IntPtr Base, int Size)
    {
        this.Target = Target;
        this.Base = Base;
        this.Size = Size;
    }

    public string Path { get; internal set; }

    public string Name => this.Path != null ? System.IO.Path.GetFileName(this.Path) : null;

    public bool Equals(RtModule Other) =>
        Other != null && Other.Base == this.Base && Other.Target == this.Target;
}
