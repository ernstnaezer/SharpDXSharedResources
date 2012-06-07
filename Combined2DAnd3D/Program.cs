﻿namespace Combined2DAnd3D
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
    using Device10 = SharpDX.Direct3D10.Device1;

    using FeatureLevel = SharpDX.Direct3D10.FeatureLevel;
    using Resource = SharpDX.Direct3D11.Resource;

    internal class Program
        : TessellationSink
    {
        private GeometrySink _geometrySink;

        // Vertex Structure
        // LayoutKind.Sequential is required to ensure the public variables
        // are written to the datastream in the correct order.
        [StructLayout(LayoutKind.Sequential)]
        public struct VertexPositionColor
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

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexPositionTexture
        {
            public Vector4 Position;
            public Vector2 TexCoord;

            public static readonly InputElement[] inputElements = new[] {
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


        public void Run()
        {
            var form = new RenderForm("2d and 3d combined...it's like magic");
            form.KeyDown += (sender, args) => { if (args.KeyCode == Keys.Escape) form.Close(); };

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
                    //ModeDescription = new ModeDescription(form.Width, form.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    IsWindowed = true,
                    OutputHandle = form.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    Usage = Usage.RenderTargetOutput
                };

            Device11 device11;
            SwapChain swapChain;

            Device11.CreateWithSwapChain(adapter1, DeviceCreationFlags.None, description, out device11, out swapChain);

            // create a view of our render target, which is the backbuffer of the swap chain we just created
            RenderTargetView renderTargetView;
            using (var resource = Resource.FromSwapChain<Texture2D>(swapChain, 0))
                renderTargetView = new RenderTargetView(device11, resource);

            // setting a viewport is required if you want to actually see anything
            var context = device11.ImmediateContext;
            
            var viewport = new Viewport(0.0f, 0.0f, form.ClientSize.Width, form.ClientSize.Height);
            context.OutputMerger.SetTargets(renderTargetView);
            context.Rasterizer.SetViewports(viewport);

            // ---------------------------------------------------------------------------------------------
            // Setup direct 3d version 10 as a bridge between the 2d and 3d world
            // ---------------------------------------------------------------------------------------------

            var device10 = new Device10(adapter1, SharpDX.Direct3D10.DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_1);

            // Create the DirectX11 texture2D.  This texture will be shared with the DirectX10
            // device.  The DirectX10 device will be used to render text onto this texture.  DirectX11
            // will then draw this texture (blended) onto the screen.
            // The KeyedMutex flag is required in order to share this resource.
            var textureD3D11 = new Texture2D(device11, new Texture2DDescription
                                                            {
                                                                Width = form.Width,
                                                                Height = form.Height,
                                                                MipLevels = 1,
                                                                ArraySize = 1,
                                                                Format = Format.B8G8R8A8_UNorm,
                                                                SampleDescription = new SampleDescription(1, 0),
                                                                Usage = ResourceUsage.Default,
                                                                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                                                                CpuAccessFlags = CpuAccessFlags.None,
                                                                OptionFlags = ResourceOptionFlags.SharedKeyedmutex
                                                            });

            // This is how DirectX knows which DirectX (10 or 11) is supposed to be writing
            // to the shared texture.  
            var sharedResource = textureD3D11.QueryInterface<SharpDX.DXGI.Resource>();
            var sharedTexture = device10.OpenSharedResource<SharpDX.Direct3D10.Texture2D>(sharedResource.SharedHandle);
            var sharedMutex = sharedTexture.QueryInterface<KeyedMutex>();

            // ---------------------------------------------------------------------------------------------
            // Setup Direct 2d
            // ---------------------------------------------------------------------------------------------

            // Direct2D Factory
            var factory2D = new SharpDX.Direct2D1.Factory(FactoryType.SingleThreaded, DebugLevel.Information);

            // Here we bind the texture we're created on our direct3d11 device through the direct3d10 
            // to the direct 2d render target.... a shared surface

            // Direct2D and DirectX10 can interoperate thru DXGI.
            var surface = sharedTexture.AsSurface();
            var rtp = new RenderTargetProperties
                {
                    MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_10,
                    Type = RenderTargetType.Hardware,
                    Usage = RenderTargetUsage.None,
                    PixelFormat = new PixelFormat(Format.Unknown, AlphaMode.Premultiplied)
                };

            var renderTarget2D = new RenderTarget(factory2D, surface, rtp);
            var solidColorBrush = new SolidColorBrush(renderTarget2D, Colors.Red);

            // ---------------------------------------------------------------------------------------------------
            // Setup the rendering data
            // ---------------------------------------------------------------------------------------------------

            // Load Effect. This includes both the vertex and pixel shaders.
            // Also can include more than one technique.
            ShaderBytecode shaderByteCode = ShaderBytecode.CompileFromFile(
                "effectDx11.fx",
                "fx_5_0",
                ShaderFlags.EnableStrictness);

            var effect = new Effect(device11, shaderByteCode);

            // create triangle vertex data, making sure to rewind the stream afterward
            var verticesTriangle = new DataStream(VertexPositionColor.SizeInBytes * 3, true, true);
            verticesTriangle.Write(new VertexPositionColor(new Vector3(0.0f, 0.5f, 0.5f),new Color4(1.0f, 0.0f, 0.0f, 1.0f)));
            verticesTriangle.Write(new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.5f),new Color4(0.0f, 1.0f, 0.0f, 1.0f)));
            verticesTriangle.Write(new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.5f),new Color4(0.0f, 0.0f, 1.0f, 1.0f)));

            verticesTriangle.Position = 0;

            // create the triangle vertex layout and buffer
            var layoutColor = new InputLayout(device11, effect.GetTechniqueByName("Color").GetPassByIndex(0).Description.Signature, VertexPositionColor.inputElements);
            var vertexBufferColor = new Buffer(device11, verticesTriangle, (int)verticesTriangle.Length, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
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
            var layoutOverlay = new InputLayout(device11, effect.GetTechniqueByName("Overlay").GetPassByIndex(0).Description.Signature, VertexPositionTexture.inputElements);
            var vertexBufferText = new Buffer(device11, verticesText, (int)verticesText.Length, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
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
            
            var blendStateTransparent = new BlendState(device11, bsd);

            // ---------------------------------------------------------------------------------------------------
            // Create and tesselate an ellipse
            // ---------------------------------------------------------------------------------------------------

            var ellipse = new EllipseGeometry(factory2D, new Ellipse(new PointF(form.Width / 2.0f, form.Height / 2.0f), form.Width / 2.0f, form.Height / 2.0f));

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

            bool first = true;

            // Main loop
            RenderLoop
                .Run(form,
                     () =>
                         {
                             if(first)
                             {
                                 form.Activate();
                                 first = false;
                             }
                                 
                             // clear the render target to black
                             context.ClearRenderTargetView(renderTargetView, Colors.DarkSlateGray);

                             //// Draw the triangle
                             //// configure the Input Assembler portion of the pipeline with the vertex data
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
                             sharedMutex.Acquire(0, 100);
                             renderTarget2D.BeginDraw();
                             renderTarget2D.Clear(Colors.Orange);
                             renderTarget2D.DrawEllipse(new Ellipse(new DrawingPointF(10,10), 20,20), solidColorBrush, 1, null);
                             renderTarget2D.DrawGeometry(tesselatedGeometry, solidColorBrush);
                             renderTarget2D.EndDraw();
                             sharedMutex.Release(0);

                             // Draw the shared texture2D onto the screen
                             // Need to Aquire the shared texture for use with DirectX11
                             sharedMutex.Acquire(0, 100);
                             var srv = new ShaderResourceView(device11, textureD3D11);
                             effect.GetVariableByName("g_Overlay").AsShaderResource().SetResource(srv);
                             context.InputAssembler.InputLayout = layoutOverlay;
                             context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
                             context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBufferText, VertexPositionTexture.SizeInBytes, 0));
                             context.OutputMerger.BlendState = blendStateTransparent;
                             currentTechnique = effect.GetTechniqueByName("Overlay");

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
                             sharedMutex.Release(0);

                             swapChain.Present(0, PresentFlags.None);
                         });

            // dispose everything
            vertexBufferColor.Dispose();
            vertexBufferText.Dispose();
            layoutColor.Dispose();
            layoutOverlay.Dispose();
            effect.Dispose();
            shaderByteCode.Dispose();
            renderTarget2D.Dispose();
            swapChain.Dispose();
            device11.Dispose();
            device10.Dispose();
            sharedMutex.Dispose();
            sharedTexture.Dispose();
            textureD3D11.Dispose();
            factory1.Dispose();
            adapter1.Dispose();
            sharedResource.Dispose();
            factory2D.Dispose();
            surface.Dispose();
            solidColorBrush.Dispose();
            blendStateTransparent.Dispose();
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