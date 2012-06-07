namespace Combined2DAnd3D
{
    using System.Runtime.InteropServices;
    using SharpDX;
    using SharpDX.DXGI;
    using SharpDX.Direct3D11;

    [StructLayout(LayoutKind.Sequential)]
    internal struct VertexPositionColor
    {
        public Vector4 Position;
        public Color4 Color;
        public static readonly InputElement[] inputElements = new[] {
            new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            new InputElement("COLOR",0,Format.R32G32B32A32_Float,16,0)
        };
        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(VertexPositionColor));
        public VertexPositionColor(Vector4 position, Color4 color)
        {
            Position = position;
            Color = color;
        }
        public VertexPositionColor(Vector3 position, Color4 color)
        {
            Position = new Vector4(position, 1);
            Color = color;
        }
    }
}