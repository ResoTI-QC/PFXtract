using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

if (args.Length != 2)
    throw new ArgumentException("Utilisation : IconBuilder <source.png> <destination.ico>");

var sizes = new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
using var source = new Bitmap(args[0]);
var frames = new List<byte[]>();

foreach (var size in sizes)
{
    using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, size, size));
    }

    using var stream = new MemoryStream();
    bitmap.Save(stream, ImageFormat.Png);
    frames.Add(stream.ToArray());
}

using var output = File.Create(args[1]);
using var writer = new BinaryWriter(output);
writer.Write((ushort)0);
writer.Write((ushort)1);
writer.Write((ushort)frames.Count);

var offset = 6 + frames.Count * 16;
for (var i = 0; i < frames.Count; i++)
{
    var size = sizes[i];
    writer.Write((byte)(size == 256 ? 0 : size));
    writer.Write((byte)(size == 256 ? 0 : size));
    writer.Write((byte)0);
    writer.Write((byte)0);
    writer.Write((ushort)1);
    writer.Write((ushort)32);
    writer.Write((uint)frames[i].Length);
    writer.Write((uint)offset);
    offset += frames[i].Length;
}

foreach (var frame in frames)
    writer.Write(frame);

Console.WriteLine($"Icône créée : {args[1]} ({frames.Count} tailles)");
