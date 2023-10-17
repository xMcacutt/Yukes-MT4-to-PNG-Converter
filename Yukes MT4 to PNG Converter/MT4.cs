using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yukes_MT4_to_PNG_Converter
{
    internal class MT4
    {
        public string Name;
        public int BitDepth; //at 0x1C 0x13 is 8bpp 0x14 is 8bpp
        public int Width;
        public int Height;
        public int PaletteOffset;
        public byte[] Palette;
        public byte[] Data;
        public byte[] RGBAData;
    }
}
