using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VFSILib.Common.Enum;
using VFSILib.Util.Extension;

namespace VFSILib.Core.IO;

public class VirtualFileSystemInfo : FileSystemInfo
{
    private bool active;

    public VirtualFileSystemInfo(string path, bool preCheck = true) : this(new FileInfo(path), preCheck)
    {
    }

    public VirtualFileSystemInfo(FileSystemInfo fi, bool preCheck = true) : this(fi.FullName, 0, 0, null, preCheck)
    {
    }

    public VirtualFileSystemInfo(string path, ulong length, ulong offset, VirtualFileSystemInfo parent,
        bool preCheck = true)
    {
        var isDirectory = false;
        FullName = path;
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (length == 0)
        {
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                length = (ulong) new FileInfo(path).Length;
            else
                isDirectory = true;
        }

        FileLength = length;
        ParentDefinedLength = length;
        Offset = offset;
        Parent = parent;
        if (Parent != null)
        {
            Initialized = Parent.Initialized;
            VFSIBytes = Parent.VFSIBytes;
        }
        else if (!isDirectory && preCheck)
        {
            Initialize();
        }
    }

    public VirtualFileSystemInfo(MemoryStream memstream, string name = "Memory", bool preCheck = true) : this(memstream,
        null, name, preCheck)
    {
    }

    public VirtualFileSystemInfo(MemoryStream memstream, VirtualFileSystemInfo parent, string name = "Memory",
        bool preCheck = true)
    {
        VFSIBytes = memstream.ToArray();
        FullName = name;
        FileLength = (ulong) VFSIBytes.Length;
        Offset = 0;
        Parent = parent;

        if (preCheck)
            Initialize();
    }

    // Properties

    public override string Name
    {
        get
        {
            var name = FullName;
            var extPaths = GetExtendedPaths(FullName);
            if (extPaths.Length > 0)
                name = extPaths.Last();
            return Path.GetFileName(name);
        }
    }

    public new string Extension => Path.GetExtension(Name);

    public override bool Exists => GetExistence();

    public new string FullName { get; }

    protected ulong Offset { get; set; }

    protected byte[] MagicBytes { get; set; } = new byte[4];

    protected byte[] VFSIBytes { get; set; }

    public ByteOrder Endianness { get; set; }

    protected bool EndiannessChecked { get; set; } = false;

    public bool NoAccess { get; protected set; }

    public ulong FileLength { get; protected set; }

    public ulong ParentDefinedLength { get; protected set; }

    public VirtualFileSystemInfo Parent { get; }

    public VirtualFileSystemInfo VirtualRoot
    {
        get
        {
            if (Parent != null)
                return Parent.VirtualRoot;

            return this;
        }
    }

    public bool Active
    {
        get => active;
        set
        {
            active = value;
            if (!value)
            {
                if (VFSIBytes != null)
                    VFSIBytes = null;
            }
            else if (VFSIBytes == null)
            {
                GetReadStream();
            }
        }
    }

    protected bool Initialized { get; set; }

    // Methods

    private bool GetExistence()
    {
        if (GetExtendedPaths(FullName).Length < 1)
            return File.Exists(FullName);

        var mainFile = new FileInfo(GetPrimaryPath(FullName));
        if (!mainFile.Exists)
            return false;

        using var fs = GetReadStream();
        return GetExistence(Parent, fs);
    }

    private bool GetExistence(VirtualFileSystemInfo vfi, Stream fs)
    {
        if (vfi.Parent != null)
            if (!GetExistence(vfi.Parent, fs))
                return false;


        fs.Seek(vfi.Offset, SeekOrigin.Current);
        var mb = new byte[4];
        fs.Read(mb, 0, 4);

        return vfi.MagicBytes.SequenceEqual(mb) && !mb.SequenceEqual(new byte[4]);
    }

    protected byte[] GetVirtualRootBytes()
    {
        return VirtualRoot.VFSIBytes;
    }

    protected void OffsetFileStream(VirtualFileSystemInfo vfi, Stream stream)
    {
        if (vfi.Parent != null)
            OffsetFileStream(vfi.Parent, stream);

        stream.Seek(vfi.Offset, SeekOrigin.Current);
    }

    public string GetPrimaryPath()
    {
        return new Regex(@":(?!\\)").Split(FullName)[0];
    }

    public string GetPrimaryPath(string extPath)
    {
        return new Regex(@":(?!\\)").Split(extPath)[0];
    }

    public string[] GetExtendedPaths(string extPath)
    {
        return new Regex(@":(?!\\)").Split(extPath).Skip(1).ToArray();
    }

    public string[] GetExtendedPaths()
    {
        return GetExtendedPaths(FullName);
    }

    public byte[] GetBytes()
    {
        var origActive = Active;
        try
        {
            if (VFSIBytes != null)
                if ((ulong) VFSIBytes.Length == FileLength || FileLength == 0)
                    return VFSIBytes;

            Active = true;
            using var reader = new BinaryReader(GetReadStream());
            var bytes = new byte[FileLength];
            for (ulong i = 0; i < FileLength; i++)
                bytes[i] = reader.ReadByte();
            reader.Close();
            return bytes;
        }
        catch
        {
            return null;
        }
        finally
        {
            Active = origActive;
        }
    }

    protected void UpdateMagicBytes(MemoryStream ms)
    {
        ms.Read(MagicBytes, 0, MagicBytes.Length);
        ms.Position = 0;
    }

    protected virtual void Initialize(bool force = false)
    {
        if (Initialized && !force)
            return;
        Initialized = true;
        using var s = GetReadStream();
        if (s == null)
            return;
        OffsetFileStream(this, s);
        s.Read(MagicBytes, 0, 4);
        s.Close();
    }

    protected virtual Stream GetReadStream()
    {
        try
        {
            if (VFSIBytes == null && VirtualRoot.VFSIBytes == null)
            {
                var fs = new FileStream(GetPrimaryPath(FullName), FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite);
                try
                {
                    if (Offset > 0) OffsetFileStream(this, fs);
                    return fs;
                }
                catch
                {
                    fs.Close();
                    fs.Dispose();
                    return null;
                }
            }

            var memStream = new MemoryStream(VFSIBytes ?? VirtualRoot.VFSIBytes);
            if (Offset > 0) OffsetFileStream(this, memStream);
            return memStream;
        }
        catch
        {
            NoAccess = true;
            return null;
        }
    }

    public override void Delete()
    {
        if (Parent == null)
        {
            var fsi = (FileSystemInfo) this;
            var fi = (FileInfo) fsi;
            if (fi != null)
            {
                fi.Delete();
                return;
            }

            var di = (DirectoryInfo) fsi;
            if (di != null) di.Delete();
        }
    }
}