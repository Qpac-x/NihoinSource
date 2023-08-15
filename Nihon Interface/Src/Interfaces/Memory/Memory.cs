using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using static Nihon.Static.Native;

namespace Nihon.MemoryFramework;

public static class Memory
{
    public static uint Call(RtTarget Proc, IntPtr Method, IntPtr Parameters)
    {
        IntPtr hThread = CreateRemoteThread(
            Proc.Handle,
            IntPtr.Zero,
            0U,
            Method,
            Parameters,
            0,
            out _);

        WaitForSingleObject(
            hThread,
            uint.MaxValue);

        GetExitCodeThread(
            hThread,
            out uint ExitCode);

        return ExitCode;
    }

    public static IntPtr Copy(RtTarget Proc, IntPtr Method, int Size = default)
    {
        VirtualQueryEx(
            Proc.Handle,
            Method,
            out MemoryBasicInformation Info,
            Marshal.SizeOf(typeof(MemoryBasicInformation)));

        if (Size == default || Size <= 0)
            Size = Info.RegionSize.ToInt32() - (Method.ToInt32() - Info.BaseAddress.ToInt32());

        IntPtr Clone = VirtualAllocEx(
            Proc.Handle,
            IntPtr.Zero,
            Size,
            AllocationType.Commit | AllocationType.Reserve,
            MemoryProtection.ReadWriteExecute);

        VirtualProtectEx(
             Proc.Handle,
             Method,
             5,
             MemoryProtection.ReadWriteExecute,
             out MemoryProtection OldProtect);

        byte[] Buffer = new byte[Size];
        ReadProcessMemory(
            Proc.Handle,
            Method,
            Buffer,
            Size,
            out _);

        WriteProcessMemory(
            Proc.Handle,
            Clone,
            Buffer,
            Size,
            out _);

        VirtualProtectEx(
             Proc.Handle,
             Method,
             5,
             OldProtect,
             out _);

        return Clone;
    }

    public static uint Rebase(RtModule Module, IntPtr Address, uint? Base = null) =>
        (uint)Module.Base + (uint)Address - (Base ?? 0);

    public static IntPtr Allocate(RtTarget Proc, int Size, MemoryProtection Protection = MemoryProtection.ReadWriteExecute) =>
        VirtualAllocEx(
            Proc.Handle,
            IntPtr.Zero,
            Size,
            AllocationType.Commit | AllocationType.Reserve,
            Protection);

    public static bool Free(RtTarget Target, IntPtr Address) =>
        VirtualFreeEx(
            Target.Handle,
            Address,
            0,
            FreeType.Release);

    public static MemoryProtection Protect(RtTarget Target, IntPtr Address, int Size, MemoryProtection Protection) =>
        VirtualProtectEx(
            Target.Handle,
            Address,
            Size,
            Protection,
            out MemoryProtection Old
            ) ? Old : MemoryProtection.NoAccess;

    /* <- Generic Read/Write -> */

    public static byte[] Read(RtTarget Target, IntPtr Address, int Length)
    {
        byte[] Buffer = new byte[Length];

        return ReadProcessMemory(
            Target.Handle,
            Address,
            Buffer,
            Length,
            out _) != true
            ? null
            : Buffer;
    }

    public static bool Write(RtTarget Target, IntPtr Address, byte[] Buffer) =>
        WriteProcessMemory(
            Target.Handle,
            Address,
            Buffer,
            Buffer.Length,
            out _);

    /* ------------------ */

    /* <- T Read/Write -> */

    public static T Read<T>(RtTarget Target, IntPtr Address) where T : unmanaged
    {
        int Size = Marshal.SizeOf(typeof(T));
        byte[] Buffer = new byte[Size];

        ReadProcessMemory(
            Target.Handle,
            Address,
            Buffer,
            Size,
            out _);

        return ByteArrayToStructure<T>(Buffer);
    }

    public static bool Write<T>(RtTarget Target, IntPtr Address, T Buffer) where T : unmanaged =>
        WriteProcessMemory(
            Target.Handle,
            Address,
            StructureToByteArray(Buffer),
            Marshal.SizeOf(typeof(T)),
            out _);

    /* ------------------ */

    private static T ByteArrayToStructure<T>(byte[] Bytes) where T : struct
    {
        GCHandle Handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
        T Pointer = (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));

        Handle.Free();
        return Pointer;
    }

    private static byte[] StructureToByteArray(object Object)
    {
        int Length = Marshal.SizeOf(Object);
        byte[] Bytes = new byte[Length];

        IntPtr Pointer = Marshal.AllocHGlobal(Length);

        Marshal.StructureToPtr(Object, Pointer, true);
        Marshal.Copy(Pointer, Bytes, 0, Length);
        Marshal.FreeHGlobal(Pointer);

        return Bytes;
    }

    /* <- Generic Instruction -> */

    public static class Instruction
    {
        public static OpCodeInfo? Read(RtTarget Target, IntPtr Address)
        {
            byte[] Buffer = Memory.Read(Target, Address, 15);
            return Buffer == null
                ? null
                : new OpCodeInfo
                {
                    Name = Enum.GetName(typeof(OpCode), Buffer[0]),
                    OpCode = (OpCode)Buffer[0],
                    Operand = new ArraySegment<byte>(Buffer, 1, Buffer.Length - 1).ToArray()
                };
        }

        public static bool Write(RtTarget Target, IntPtr Address, OpCode OpCode, byte[] Operand)
        {
            byte[] Buffer = new byte[Operand.Length + 1];

            Buffer[0] = (byte)OpCode;
            Array.Copy(Operand, 0, Buffer, 1, Operand.Length);

            return Memory.Write(Target, Address, Buffer);
        }

        public struct OpCodeInfo
        {
            public string Name;
            public OpCode OpCode;
            public byte[] Operand;
        }

        public enum OpCode
        {
            Nop = 0x90,
            Ret = 0xC3,
            Call = 0xE8,
            Push = 0x68,
            Pop = 0x58,
            Add = 0x05,
            Sub = 0x2D,
            Cmp = 0x3D,
            JmpShort = 0xEB,
            JnzShort = 0x75,
            JzShort = 0x74,
            JgShort = 0x7F,
            JlShort = 0x7C,
            JgeShort = 0x7D,
            JleShort = 0x7E,
            JmpNear = 0xE9,
            JnzNear = 0x0F85,
            JzNear = 0x0F84,
            JgNear = 0x0F8F,
            JlNear = 0x0F8C,
            JgeNear = 0x0F8D,
            JleNear = 0x0F8E,
            JmpFar = 0xEA,
            JnzFar = 0x0F85,
            JzFar = 0x0F84,
            JgFar = 0x0F8F,
            JlFar = 0x0F8C,
            JgeFar = 0x0F8D,
            JleFar = 0x0F8E,
            MovEax = 0xB8,
            MovEcx = 0xB9,
            MovEdx = 0xBA,
            MovEbx = 0xBB,
            MovEsp = 0xBC,
            MovEbp = 0xBD,
            MovEsi = 0xBE,
            MovEdi = 0xBF,
            PushEax = 0x50,
            PushEcx = 0x51,
            PushEdx = 0x52,
            PushEbx = 0x53,
            PushEsp = 0x54,
            PushEbp = 0x55,
            PushEsi = 0x56,
            PushEdi = 0x57,
            PopEax = 0x58,
            PopEcx = 0x59,
            PopEdx = 0x5A,
            PopEbx = 0x5B,
            PopEsp = 0x5C,
            PopEbp = 0x5D,
            PopEsi = 0x5E,
            PopEdi = 0x5F,
            Pushad = 0x60,
            Popad = 0x61,
        }
    }

    /* ------------------ */
}

public class Pe32
{
    public FileInfo
    Info
    { get; set; }

    public ImageDosHeader
    DosHeader
    { get; set; }

    public ImageFileHeader
    FileHeader
    { get; set; }

    public ImageOptionalHeader
    OptionalHeader
    { get; set; }

    public ImageSectionHeader[]
    SectionHeaders
    { get; set; }
}

public class PeReader
{
    private ImageDosHeader ImageDos;
    private ImageFileHeader ImageFile;
    private ImageOptionalHeader ImageOptional;
    private ImageSectionHeader[] ImageSection;

    public string Path;

    public bool Open(out Pe32 File)
    {
        using (FileStream Stream = new FileStream(this.Path, FileMode.Open, System.IO.FileAccess.Read))
        {
            BinaryReader Reader = new BinaryReader(Stream);

            this.ImageDos = this.ReadBinary<ImageDosHeader>(Reader);

            Stream.Seek(this.ImageDos.e_lfanew, SeekOrigin.Begin);

            uint NtSignature = Reader.ReadUInt32();
            this.ImageFile = this.ReadBinary<ImageFileHeader>(Reader);
            this.ImageOptional = this.ReadBinary<ImageOptionalHeader>(Reader);

            this.ImageSection = new ImageSectionHeader[this.ImageFile.NumberOfSections];

            for (int i = 0; i < this.ImageSection.Length; ++i)
                this.ImageSection[i] = this.ReadBinary<ImageSectionHeader>(Reader);

            for (int i = 0; i < this.ImageOptional.DataDirectory.Length; ++i)
                this.ImageOptional.DataDirectory[i].Type = (DataDirectoryType)i;
        }

        File = new Pe32
        {
            Info = new FileInfo(this.Path),

            DosHeader = ImageDos,
            FileHeader = ImageFile,
            OptionalHeader = ImageOptional,
            SectionHeaders = ImageSection
        };


        return File != null;
    }

    private T ReadBinary<T>(BinaryReader Reader)
    {
        byte[] Bytes = Reader.ReadBytes(Marshal.SizeOf(typeof(T)));

        GCHandle Handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
        T Struct = (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));
        Handle.Free();

        return Struct;
    }
}
