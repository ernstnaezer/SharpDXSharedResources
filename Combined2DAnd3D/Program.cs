/*
 * Shared resource between Direct 2D and Direct 3D 11 using Sharp DX
 *  
 * This is a 'one file' example of a shared surfce between two direct X devices written in C# / SharpDX. 
 * Windows 8 Direct2D.1 has native support for this but as long as you are on Windows7 you need some trickery.
 *
 * Direct2D only works when you create a Direct3D 10.1 device, but it can share surfaces with Direct3D 11. 
 * All you need to do is create both devices and render all of your Direct2D content to a texture that you share between them. 
 * I use this technique in my own applications to use Direct2D with Direct3D 11. It incurs a slight cost, but it is small and constant per frame.
 * 
 * A basic outline of the process you will need to use is:
 * 
 * Create your Direct3D 11 device like you do normally.
 * Create a texture with the D3D10_RESOURCE_MISC_SHARED_KEYEDMUTEX option in order to allow access to the ID3D11KeyedMutex interface.
 * Use the GetSharedHandle to get a handle to the texture that can be shared among devices.
 * Create a Direct3D 10.1 device, ensuring that it is created on the same adapter.
 * Use OpenSharedResource function on the Direct3D 10.1 device to get a version of the texture for Direct3D 10.1.
 * Get access to the D3D10 KeyedMutex interface for the texture.
 * Use the Direct3D 10.1 version of the texture to create the RenderTarget using Direct2D.
 * When you want to render with D2D, use the keyed mutex to lock the texture for the D3D10 device. Then, acquire it in D3D11 and render the texture like you were probably already trying to do.
 * It's not trivial, but it works well, and it is the way that they intended you to interoperate between them.
 *
 * http://stackoverflow.com/a/9071915* 
 * https://github.com/enix/SharpDXSharedResources
 *
 * Coded by Aaron Auseth and Ernst Naezer
 * 
 * Freeware: The author, of this software accepts no responsibility for damages resulting from the use of this product and makes no warranty or representation, 
 * either express or implied, including but not limited to, any implied warranty of merchantability or fitness for a particular purpose. 
 * This software is provided "AS IS", and you, its user, assume all risks when using it.
 * 
 * All I ask is that I be given credit if you use as a tutorial or for educational purposes.
 */
namespace Combined2DAnd3D
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using SharpDX;
    using SharpDX.D3DCompiler;
    using SharpDX.DXGI;
    using SharpDX.Direct2D1;
    using SharpDX.Direct3D;
    using SharpDX.Direct3D11;
    using SharpDX.Windows;
    using Buffer = SharpDX.Direct3D11.Buffer;
    using Device11 = SharpDX.Direct3D11.Device;
    using FeatureLevel = SharpDX.Direct3D10.FeatureLevel;
    using Resource = SharpDX.Direct3D11.Resource;

    internal class Program
        : TessellationSink
    {
        // Vertex Structure
        // LayoutKind.Sequential is required to ensure the public variables
        // are written to the datastream in the correct order.
        [StructLayout(LayoutKind.Sequential)]
        public struct VertexPositionColor
        {
            public Vector4 Position;
            public Color4 Color;
            public static readonly InputElement[] InputElements = new[] {
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

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexPositionTexture
        {
            public Vector4 Position;
            public Vector2 TexCoord;

            public static readonly InputElement[] InputElements = new[] {
				new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
				new InputElement("TEXCOORD",0,Format.R32G32_Float, 16 ,0)
			};
            public static readonly int SizeInBytes = Marshal.SizeOf(typeof(VertexPositionTexture));
            public VertexPositionTexture(Vector4 position, Vector2 texCoord)
            {
                Position = position;
                TexCoord = texCoord;
            }
            public VertexPositionTexture(Vector3 position, Vector2 texCoord)
            {
                Position = new Vector4(position, 1);
                TexCoord = texCoord;
            }
        }

        private RenderForm _form;
        private Device11 _device11;
        private SwapChain _swapChain;
        private GeometrySink _geometrySink;
        private SharpDX.Direct3D10.Device1 _device101;

        public void Run()
        {
            _form = new RenderForm("2d and 3d combined...it's like magic");
            _form.KeyDown += (sender, args) => { if (args.KeyCode == Keys.Escape) _form.Close(); };

            // DirectX DXGI 1.1 factory
            var factory1 = new Factory1();

            // The 1st graphics adapter
            var adapter1 = factory1.GetAdapter1(0);

            // ---------------------------------------------------------------------------------------------
            // Setup direct 3d version 11. This context will be used to display the combined elements
            // ---------------------------------------------------------------------------------------------

            var description = new SwapChainDescription
                           {
                               BufferCount = 1,
                               ModeDescription = new ModeDescription(_form.Width, _form.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                               IsWindowed = true,
                               OutputHandle = _form.Handle,
                               SampleDescription = new SampleDescription(1, 0),
                               SwapEffect = SwapEffect.Discard,
                               Usage = Usage.RenderTargetOutput
                           };

            Device11.CreateWithSwapChain(adapter1, DeviceCreationFlags.None, description, out _device11, out _swapChain);
            _device101 = new SharpDX.Direct3D10.Device1(adapter1, SharpDX.Direct3D10.DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_0);

            // create a view of our render target, which is the backbuffer of the swap chain we just created
            RenderTargetView renderTarget;
            using (var resource = Resource.FromSwapChain<Texture2D>(_swapChain, 0))
                renderTarget = new RenderTargetView(_device11, resource);

            // setting a viewport is required if you want to actually see anything
            var context = _device11.ImmediateContext;
            var viewport = new Viewport(0.0f, 0.0f, _form.ClientSize.Width, _form.ClientSize.Height);
            context.OutputMerger.SetTargets(renderTarget);
            context.Rasterizer.SetViewports(viewport);

            // ---------------------------------------------------------------------------------------------
            // Setup direct 3d version 10 as a bridge between the 2d and 3d world
            // ---------------------------------------------------------------------------------------------

            // Create the DirectX11 texture2D.  This texture will be shared with the DirectX10
            // device.  The DirectX10 device will be used to render text onto this texture.  DirectX11
            // will then draw this texture (blended) onto the screen.
            // The KeyedMutex flag is required in order to share this resource.
            var textureD3D11 = new Texture2D(_device11, new Texture2DDescription
                                                            {
                                                                Width = _form.Width,
                                                                Height = _form.Height,
                                                                MipLevels = 1,
                                                                ArraySize = 1,
                                                                Format = Format.B8G8R8A8_UNorm,
                                                                SampleDescription = new SampleDescription(1, 0),
                                                                Usage = ResourceUsage.Default,
                                                                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                                                                CpuAccessFlags = CpuAccessFlags.None,
                                                                OptionFlags = ResourceOptionFlags.SharedKeyedmutex
                                                            });

            // A DirectX10 Texture2D sharing the DirectX11 Texture2D

            var sharedResource = new SharpDX.DXGI.Resource(textureD3D11.NativePointer);
            var textureD3D10 = _device101.OpenSharedResource<SharpDX.Direct3D10.Texture2D>(sharedResource.SharedHandle);

            // The KeyedMutex is used just prior to writing to textureD3D11 or textureD3D10.
            // This is how DirectX knows which DirectX (10 or 11) is supposed to be writing
            // to the shared texture.  The keyedMutex is just defined here, they will be used
            // a bit later.
            var mutexD3D10 = new KeyedMutex(textureD3D10.NativePointer);
            var mutexD3D11 = new KeyedMutex(textureD3D11.NativePointer);

            // ---------------------------------------------------------------------------------------------
            // Setup Direct 2d
            // ---------------------------------------------------------------------------------------------

            // Direct2D Factory
            var factory2D = new SharpDX.Direct2D1.Factory(FactoryType.SingleThreaded, DebugLevel.Information);
           
            //using (var res11 = RenderTarget.QueryInterface<SharpDX.DXGI.Resource>())
            //using (var res10 = _device101.OpenSharedResource<SharpDX.DXGI.Resource>(res11.SharedHandle))
            //using (var surface = res10.QueryInterface<Surface>())
            //{
            //    var renderTarget2D = new RenderTarget(
            //            factory2D,
            //            surface,
            //            new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied))
            //    );
            //}
            //renderTarget2D.AntialiasMode = AntialiasMode.PerPrimitive;

            // New RenderTargetView from the backbuffer
            var backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0);

            RenderTarget renderTarget2D;
            using (var surface = backBuffer.QueryInterface<Surface>())
            {
                renderTarget2D = new RenderTarget(factory2D, surface, new RenderTargetProperties(new PixelFormat(Format.Unknown, AlphaMode.Premultiplied)))
                                     {
                                         AntialiasMode = AntialiasMode.PerPrimitive
                                     };
            }

            var solidColorBrush = new SolidColorBrush(renderTarget2D, Colors.White);

            // ---------------------------------------------------------------------------------------------------
            // Setup the rendering data
            // ---------------------------------------------------------------------------------------------------

            // Load Effect. This includes both the vertex and pixel shaders.
            // Also can include more than one technique.
            ShaderBytecode shaderByteCode = ShaderBytecode.CompileFromFile(
                "effectDx11.fx",
                "fx_5_0",
                ShaderFlags.EnableStrictness);

            var effect = new Effect(_device11, shaderByteCode);

            // create triangle vertex data, making sure to rewind the stream afterward
            var verticesTriangle = new DataStream(VertexPositionColor.SizeInBytes * 3, true, true);
            verticesTriangle.Write(new VertexPositionColor(new Vector3(0.0f, 0.5f, 0.5f),new Color4(1.0f, 0.0f, 0.0f, 1.0f)));
            verticesTriangle.Write(new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.5f),new Color4(0.0f, 1.0f, 0.0f, 1.0f)));
            verticesTriangle.Write(new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.5f),new Color4(0.0f, 0.0f, 1.0f, 1.0f)));

            verticesTriangle.Position = 0;

            // create the triangle vertex layout and buffer
            var layoutColor = new InputLayout(_device11, effect.GetTechniqueByName("Color").GetPassByIndex(0).Description.Signature, VertexPositionColor.InputElements);
            var vertexBufferColor = new Buffer(_device11, verticesTriangle, (int)verticesTriangle.Length, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            verticesTriangle.Close();

            // create text vertex data, making sure to rewind the stream afterward
            // Top Left of screen is -1, +1
            // Bottom Right of screen is +1, -1
            var verticesText = new DataStream(VertexPositionTexture.SizeInBytes * 4, true, true);
            verticesText.Write(new VertexPositionTexture(new Vector3(-1, 1, 0),new Vector2(0, 0f)));
            verticesText.Write(new VertexPositionTexture(new Vector3(1, 1, 0),new Vector2(1, 0)));
            verticesText.Write(new VertexPositionTexture(new Vector3(-1, -1, 0),new Vector2(0, 1)));
            verticesText.Write(new VertexPositionTexture(new Vector3(1, -1, 0),new Vector2(1, 1)));

            verticesText.Position = 0;

            // create the text vertex layout and buffer
            var layoutText = new InputLayout(_device11, effect.GetTechniqueByName("Text").GetPassByIndex(0).Description.Signature, VertexPositionTexture.InputElements);
            var vertexBufferText = new Buffer(_device11, verticesText, (int)verticesText.Length, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            verticesText.Close();

            // Think of the shared textureD3D10 as an overlay.
            // The overlay needs to show the text but let the underlying triangle (or whatever)
            // show thru, which is accomplished by blending.
            var bsd = new BlendStateDescription();
            bsd.RenderTarget[0].IsBlendEnabled = true;
            bsd.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            bsd.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            bsd.RenderTarget[0].BlendOperation = BlendOperation.Add;
            bsd.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            bsd.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            bsd.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            bsd.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            
            var blendStateTransparent = new BlendState(_device11, bsd);

            // ---------------------------------------------------------------------------------------------------
            // Create and tesselate an ellipse
            // ---------------------------------------------------------------------------------------------------

            var ellipse = new EllipseGeometry(factory2D, new Ellipse(new PointF(_form.Width / 2.0f, _form.Height / 2.0f), _form.Width / 2.0f - 100, _form.Height / 2.0f - 100));

            // Populate a PathGeometry from Ellipse tessellation 
            var tesselatedGeometry = new PathGeometry(factory2D);
            _geometrySink = tesselatedGeometry.Open();

            // Force RoundLineJoin otherwise the tesselated looks buggy at line joins
            _geometrySink.SetSegmentFlags(PathSegment.ForceRoundLineJoin);

            // Tesselate the ellipse to our TessellationSink
            ellipse.Tessellate(1, this);

            _geometrySink.Close();

            // ---------------------------------------------------------------------------------------------------
            // Main rendering loop
            // ---------------------------------------------------------------------------------------------------

            // Main loop
            RenderLoop
                .Run(_form,
                     () =>
                         {
                             // clear the render target to black
                             context.ClearRenderTargetView(renderTarget, Colors.BlueViolet);

                             // Draw the triangle
                             // configure the Input Assembler portion of the pipeline with the vertex data
                             context.InputAssembler.InputLayout = layoutColor;
                             context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                             context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBufferColor, VertexPositionColor.SizeInBytes, 0));
                             context.OutputMerger.BlendState = null;
                             var currentTechnique = effect.GetTechniqueByName("Color");
                             for (var pass = 0; pass < currentTechnique.Description.PassCount; ++pass)
                             {
                                 using (var effectPass = currentTechnique.GetPassByIndex(pass))
                                 {
                                     System.Diagnostics.Debug.Assert(effectPass.IsValid, "Invalid EffectPass");
                                     effectPass.Apply(context);
                                 }
                                 context.Draw(3, 0);
                             };

                             // Draw Ellipse on the shared Texture2D
                             // Need to Acquire the shared texture for use with DirectX10
                             mutexD3D10.Acquire(0, 100);
                             renderTarget2D.BeginDraw();
                             renderTarget2D.Clear(Colors.Orange);
                             renderTarget2D.DrawGeometry(tesselatedGeometry, solidColorBrush);
                             renderTarget2D.EndDraw();
                             mutexD3D10.Release(0);

                             // Draw the shared texture2D onto the screen
                             // Need to Aquire the shared texture for use with DirectX11
                             mutexD3D11.Acquire(0, 100);
                             var srv = new ShaderResourceView(_device11, textureD3D11);
                             effect.GetVariableByName("g_Overlay").AsShaderResource().SetResource(srv);
                             context.InputAssembler.InputLayout = layoutText;
                             context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
                             context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBufferText, VertexPositionTexture.SizeInBytes, 0));
                             context.OutputMerger.BlendState = blendStateTransparent;
                             currentTechnique = effect.GetTechniqueByName("2DOverlay");
                             
                             for (var pass = 0; pass < currentTechnique.Description.PassCount; ++pass)
                             {
                                 using (var effectPass = currentTechnique.GetPassByIndex(pass))
                                 {
                                     System.Diagnostics.Debug.Assert(effectPass.IsValid, "Invalid EffectPass");
                                     effectPass.Apply(context);
                                 }
                                 context.Draw(4, 0);
                             }
                             srv.Dispose();
                             mutexD3D11.Release(0);

                             _swapChain.Present(0, PresentFlags.None);
                         });
        }

        private static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
        }

        public void Dispose()
        {           
        }

        public IDisposable Shadow { get; set; }
        public void AddTriangles(Triangle[] triangles)
        {
            // Add Tessellated triangles to the opened GeometrySink
            foreach (var triangle in triangles)
            {
                _geometrySink.BeginFigure(triangle.Point1, FigureBegin.Filled);
                _geometrySink.AddLine(triangle.Point2);
                _geometrySink.AddLine(triangle.Point3);
                _geometrySink.EndFigure(FigureEnd.Closed);
            }
        }

        public void Close()
        {
            
        }
    }
}