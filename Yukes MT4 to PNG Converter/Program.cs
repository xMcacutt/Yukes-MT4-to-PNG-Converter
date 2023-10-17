using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using System;
using System.Drawing.Imaging;
using SixLabors.ImageSharp.Formats;
using System.IO;
using ImageMagick;

namespace Yukes_MT4_to_PNG_Converter
{
    public class Program
    {
        static string _path;
        static bool _run = true;

        public static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (File.Exists(arg))
                {
                    Convert(arg);
                }
            }
            if (args.Length != 0) return;

            while (_run)
            {
                Console.Clear();
                Console.WriteLine("MT4 Converter for The DOG Island");

                string input = "";
                while (!string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase) && !string.Equals(input, "n", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Would you like to convert all MT4s?   Y / N");
                    input = Console.ReadLine();
                }
                if (string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = "";
                    while (!string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase) && !string.Equals(input, "n", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Move files when complete?   Y / N");
                        input = Console.ReadLine();
                    }
                    bool move = string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase);

                    Console.WriteLine("Please enter path to root directory");
                    _path = Console.ReadLine().Replace("\"", "");
                    while (!Directory.Exists(_path))
                    {
                        Console.WriteLine("Path was invalid");
                        Console.WriteLine("Please enter path to root directory");
                        _path = Console.ReadLine().Replace("\"", " ");
                    }
                    string output = "";
                    if (move)
                    {
                        Console.WriteLine("Please enter path to output root directory");
                        output = Console.ReadLine().Replace("\"", "");
                        while (!Directory.Exists(_path))
                        {
                            Console.WriteLine("Path was invalid");
                            Console.WriteLine("Please enter path to output root directory");
                            output = Console.ReadLine().Replace("\"", " ");
                        }
                    }
                    Console.WriteLine("Converting...");
                    ConvertAll(_path, output);
                    Console.WriteLine("Conversion completed successfully");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Please enter path to MT4");
                    _path = Console.ReadLine().Replace("\"", "");
                    while (!File.Exists(_path) || !_path.EndsWith(".mt4", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Path was invalid");
                        Console.WriteLine("Please enter path to MT4");
                        _path = Console.ReadLine().Replace("\"", " ");
                    }
                    Console.WriteLine("Converting...");
                    Convert(_path);
                    Console.WriteLine("Conversion completed successfully");
                    Console.ReadLine();
                }
            }
        }

        public static void Convert(string path)
        {
            var f = File.OpenRead(path);

            if (!Enumerable.SequenceEqual(ReadBytes(f, 4), new byte[] { 0x4D, 0x54, 0x50, 0x34 }))
            {
                Console.WriteLine("File is not mt4. MTP4 Magic not present. Ending conversion.");
                return;
            }
            MT4 mt4 = new();
            mt4.Width = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
            mt4.Height = mt4.Width;
            f.Seek(0x40, SeekOrigin.Begin);
            mt4.BitDepth = (int)Math.Log2(BitConverter.ToInt16(ReadBytes(f, 2), 0));
            f.Seek(0x14, SeekOrigin.Begin);
            mt4.PaletteOffset = (int)BitConverter.ToUInt32(ReadBytes(f, 4), 0) + 0x10;
            f.Seek(mt4.PaletteOffset, SeekOrigin.Begin);
            mt4.Palette = ReadBytes(f, (int)(4 * Math.Pow(2, mt4.BitDepth)));
            f.Seek(0x50, SeekOrigin.Begin);
            long x = f.Position;
            mt4.Data = ReadBytes(f, (int)(mt4.Width * mt4.Height * ((float)mt4.BitDepth / 8f)));
            f.Close();

            mt4.Palette = PS2ShiftPalette(mt4.Palette);
            mt4.RGBAData = ComposeRGBA(mt4.Data, mt4.Palette, mt4.BitDepth);

            Image<Rgba32> image = Image.LoadPixelData<Rgba32>(mt4.RGBAData, mt4.Width, mt4.Height);
            image.Save(Path.GetFileNameWithoutExtension(path) + ".png", new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            return;

        }

        public static void ConvertAll(string inDir, string outDir)
        {
            foreach (var file in Directory.GetFiles(inDir, "*.mt4", SearchOption.AllDirectories))
            {
                var f = File.OpenRead(file);

                if (!Enumerable.SequenceEqual(ReadBytes(f, 4), new byte[] { 0x4D, 0x54, 0x50, 0x34 }))
                {
                    Console.WriteLine("File is not mt4. MTP4 Magic not present. Ending conversion.");
                    return;
                }
                MT4 mt4 = new();
                mt4.Width = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
                mt4.Height = mt4.Width;
                f.Seek(0x40, SeekOrigin.Begin);
                mt4.BitDepth = (int)Math.Log2(BitConverter.ToInt16(ReadBytes(f, 2), 0));
                f.Seek(0x14, SeekOrigin.Begin);
                mt4.PaletteOffset = (int)BitConverter.ToUInt32(ReadBytes(f, 4), 0) + 0x10;
                f.Seek(mt4.PaletteOffset, SeekOrigin.Begin);
                mt4.Palette = ReadBytes(f, (int)(f.Length - mt4.PaletteOffset));
                f.Seek(0x50, SeekOrigin.Begin);
                mt4.Data = ReadBytes(f, (int)(f.Length - mt4.Palette.Length - 0x40));
                f.Close();

                mt4.Palette = PS2ShiftPalette(mt4.Palette);
                mt4.RGBAData = ComposeRGBA(mt4.Data, mt4.Palette, mt4.BitDepth);

                Image<Rgba32> image = Image.LoadPixelData<Rgba32>(mt4.RGBAData, mt4.Width, mt4.Height);

                string relativePath = Path.GetRelativePath(inDir, file);
                string outputPath = Path.Combine(outDir, relativePath);

                // Create the necessary directories if they don't exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                image.Save(Path.GetFileNameWithoutExtension(outputPath) + ".png", new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                return;
            }
        }

        public static byte[] ReadBytes(FileStream f, int len)
        {
            byte[] buffer = new byte[len];
            f.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static byte[] ComposeRGBA(byte[] data, byte[] palette, int depth)
        {
            using MemoryStream output = new MemoryStream();
            foreach (byte b in data)
            {
                if (depth == 4)
                {
                    int i2 = (b & 0xF0) >> 4;
                    int i1 = b & 0x0F;
                    output.Write(new byte[] { palette[i1 * 4], palette[i1 * 4 + 1], palette[i1 * 4 + 2], (byte)(palette[i1 * 4 + 3] * 255f / 128f) });
                    output.Write(new byte[] { palette[i2 * 4], palette[i2 * 4 + 1], palette[i2 * 4 + 2], (byte)(palette[i2 * 4 + 3] * 255f / 128f) });
                }
                if (depth == 8)
                {
                    output.Write(new byte[] { palette[b * 4], palette[b * 4 + 1], palette[b * 4 + 2], (byte)(palette[b * 4 + 3] * 255f / 128f) });
                }
            }
            return output.ToArray();
        }

        public static byte[] PS2ShiftPalette(byte[] palette)
        {
            if (palette.Length < 128) return palette;
            using MemoryStream output = new MemoryStream();
            for (int iChunk = 0; iChunk < palette.Length / 128; iChunk++)
            {
                byte[][] groups = new byte[4][];
                for (int iGroup = 0; iGroup < 4; iGroup++)
                {
                    groups[iGroup] = palette.Skip(iChunk * 128 + iGroup * 32).Take(32).ToArray();
                }
                output.Write(groups[0]);
                output.Write(groups[2]);
                output.Write(groups[1]);
                output.Write(groups[3]);
            }
            return output.ToArray();
        }

    }
}