using System.IO;

namespace VFSILib.Util.Extension;

public static class StreamExtention
{
    public static void Seek(this Stream s, ulong offset, SeekOrigin origin)
    {
        if (offset > long.MaxValue)
        {
            var halfOffset = (long) (offset / 2);
            var r = (long) (offset % 2);
            s.Seek(halfOffset, origin);
            s.Seek(halfOffset, SeekOrigin.Current);
            s.Seek(r, SeekOrigin.Current);
        }
        else
        {
            s.Seek((long) offset, origin);
        }
    }

    public static MemoryStream ToMemoryStream(this Stream s, bool leaveOpen = false)
    {
        var memstream = new MemoryStream();

        s.CopyTo(memstream);
        if (!leaveOpen)
        {
            s.Close();
            s.Dispose();
        }

        memstream.Position = 0;
        return memstream;
    }
}