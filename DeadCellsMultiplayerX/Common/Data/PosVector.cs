using System.Runtime.CompilerServices;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    [MessagePackObject]
    public class PosVector
    {
        [Key(0)] public ulong Packed;

        private const int CX_BITS = 16;
        private const int CY_BITS = 16;
        private const int XR_BITS = 8;
        private const int XY_BITS = 8;

        private const ulong CX_MASK = (1UL << CX_BITS) - 1;
        private const ulong CY_MASK = ((1UL << CY_BITS) - 1) << 16;
        private const ulong DIR_MASK = 1UL << 32;
        private const ulong XR_MASK = ((1UL << XR_BITS) - 1) << 33;
        private const ulong XY_MASK = ((1UL << XY_BITS) - 1) << 41;

        private const int CY_SHIFT = 16;
        private const int DIR_SHIFT = 32;
        private const int XR_SHIFT = 33;
        private const int XY_SHIFT = 41;

        [IgnoreMember]
        public int CX
        {
            get => (int)(Packed & CX_MASK);
            set => Packed = (Packed & ~CX_MASK) | ((ulong)value & CX_MASK);
        }

        [IgnoreMember]
        public int CY
        {
            get => (int)((Packed & CY_MASK) >> CY_SHIFT);
            set => Packed = (Packed & ~CY_MASK) | (((ulong)value << CY_SHIFT) & CY_MASK);
        }

        [IgnoreMember]
        public double XR
        {
            get => ((Packed & XR_MASK) >> XR_SHIFT) / 255.0;
            set
            {
                byte quant = (byte)(value * 255.0);
                Packed = (Packed & ~XR_MASK) | ((ulong)quant << XR_SHIFT);
            }
        }

        [IgnoreMember]
        public double XY
        {
            get => ((Packed & XY_MASK) >> XY_SHIFT) / 255.0;
            set
            {
                byte quant = (byte)(value * 255.0);
                Packed = (Packed & ~XY_MASK) | ((ulong)quant << XY_SHIFT);
            }
        }

        [IgnoreMember]
        public int DIR
        {
            get => (Packed & DIR_MASK) != 0 ? -1 : 1;
            set
            {
                if (value < 0)
                    Packed |= DIR_MASK;
                else
                    Packed &= ~DIR_MASK;
            }
        }

        public PosVector() { }

        public PosVector(int cx, int cy, double xr, double xy, int dir)
        {
            CX = cx;
            CY = cy;
            XR = xr;
            XY = xy;
            DIR = dir;
        }
    }
}