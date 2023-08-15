using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

using static Nihon.Static.Native;

namespace Nihon;

public class ModuleControl
{
    #region Storage

    public enum InjectOutput : uint
    {
        Success,
        ModuleNotFound,
        OpenProcessFail,
        AllocationFail,
        LoadLibraryFail,
        AlreadyInjected,
        ProcessNotOpen,
        Unknown
    }

    private static int iProc { get; set; }
    private static IntPtr hProc { get; set; }
    private static string Path { get; set; }

    private const string ProcessName = "Windows10Universal";

    #endregion

    #region Imports

    private const string ModuleLibrary = "Import-Module.dll";

    [DllImport(ModuleLibrary, CallingConvention = CallingConvention.StdCall, EntryPoint = "run_script")]
    private static extern bool Execute(
        IntPtr hProc,
        int Pid,
        string Path,
        [MarshalAs(UnmanagedType.LPWStr)] string Script);

    [DllImport(ModuleLibrary, CallingConvention = CallingConvention.StdCall, EntryPoint = "is_injected")]
    private static extern bool IsAttached(
        IntPtr hProc,
        int Pid,
        string Path);

    #endregion

    public static bool IsInjected =>
        hProc == IntPtr.Zero 
        || iProc == 0
        || Path == null ? false : IsAttached(hProc, iProc, Path);

    public static InjectOutput Inject(string ModulePath)
    {
        if (File.Exists(ModulePath) != true)
            return InjectOutput.ModuleNotFound;

        #region Set-Permissions

        FileInfo FileInfo = new FileInfo(ModulePath);

        FileSecurity Security = FileInfo.GetAccessControl();
        SecurityIdentifier Identity = new SecurityIdentifier("S-1-15-2-1");

        Security.AddAccessRule(new FileSystemAccessRule(
            Identity,
            FileSystemRights.FullControl,
            InheritanceFlags.None,
            PropagationFlags.NoPropagateInherit,
            AccessControlType.Allow));

        FileInfo.SetAccessControl(Security);

        #endregion

        #region Create-Directories

        string Folder = "";

        foreach (string DirLocation in Directory.GetDirectories(Environment.GetEnvironmentVariable("LocalAppData") + "\\Packages"))
        {
            if (DirLocation.Contains("OBLOXCORPORATION"))
                if (Directory.GetDirectories(DirLocation + "\\AC").Any((string Dir) => Dir.Contains("Temp")))
                    Folder = DirLocation + "\\AC";
        }

        string[] Directories = { System.IO.Path.Combine(Folder, "workspace"), System.IO.Path.Combine(Folder, "autoexec") };

        for (int i = 0; i < Directories.Length; ++i)
            if (Directory.Exists(Directories[i]))
                Directory.CreateDirectory(Directories[i]);

        #endregion

        Process Proc = Process.GetProcessesByName(ProcessName).Length > 0 ?
            Process.GetProcessesByName(ProcessName)[0] : null;

        if (Proc == null)
            return InjectOutput.ProcessNotOpen;

        if (IsInjected == true || iProc == Proc.Id)
            return InjectOutput.AlreadyInjected;

        IntPtr hProcess = OpenProcess(
             ProcessAccess.All,
             false,
             Proc.Id);

        if (hProcess == IntPtr.Zero)
            return InjectOutput.OpenProcessFail;

        hProc = hProcess;
        iProc = Proc.Id;
        Path = ModulePath;

        IntPtr AllocatedMemory = VirtualAllocEx(
            hProcess,
            IntPtr.Zero,
            ModulePath.Length + 1,
            AllocationType.Commit | AllocationType.Reserve,
            MemoryProtection.ReadWriteExecute);

        if (AllocatedMemory == IntPtr.Zero)
            return InjectOutput.AllocationFail;

        byte[] Bytes = Encoding.Default.GetBytes(ModulePath);

        if (WriteProcessMemory(
            hProcess,
            AllocatedMemory,
            Bytes,
            Bytes.Length,
            out _) == false)
            return InjectOutput.Unknown;

        IntPtr ProcAddress = GetProcAddress(
            LoadLibraryA("kernel32.dll"),
            "LoadLibraryA");

        FreeLibrary(LoadLibraryA("kernel32.dll"));

        return ProcAddress == IntPtr.Zero || CreateRemoteThread(
            hProcess,
            IntPtr.Zero,
            0,
            ProcAddress,
            AllocatedMemory,
            CreationFlags.RunImmediately,
            out _) == IntPtr.Zero
            ? InjectOutput.LoadLibraryFail
            : InjectOutput.Success;
    }

    public static bool RunScript(string Script)
    {
        if (Script == string.Empty || IsInjected == false)
            return false;

        return Execute(hProc, iProc, Path, Script);
    }
}
