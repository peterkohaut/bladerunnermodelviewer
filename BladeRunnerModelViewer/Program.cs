using System;
using System.Drawing;
using System.Net;
using BladeRunnerHelper;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Platform.Windows;
using Vector3 = OpenTK.Vector3;

namespace BladeRunnerModelViewer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ModelRenderer model = null;
            int frame = 0;
            int animation = 0;
            float sliceHeight = 0;
            float renderScale = 0.02f;

            int first = 1;
            float firstX = -1;
            float firstY = -1;
            float firstR = -1;
            float firstG = -1;
            float firstB = -1;

            float lastX = -1;
            float lastY = -1;
            float lastR = -1;
            float lastG = -1;
            float lastB = -1;
            using (var game = new GameWindow())
            {
                game.Load += (sender, e) =>
                {
                    model = new ModelRenderer();

                    model.loadIndex();
                    model.loadCore();
                    model.loadFrame();

                    model.setMaxMemory(1 << 20);

                    model.vertex = (x, y, r, g, b) =>
                    {

                        if (first == 1)
                        {
                            first = 0;
                            firstX = x;
                            firstY = y;
                            firstR = r;
                            firstG = g;
                            firstB = b;
                        }
                        else
                        {
                            GL.Begin(PrimitiveType.Quads);
                            GL.Color3(lastR / 31f, lastG / 31f, lastB / 31f);
                            //GL.Color3(r / 31f, g / 31f, b / 31f);
                            GL.Vertex3(x, 0, y);
                            GL.Vertex3(x, sliceHeight, y);
                            //GL.Color3(lastR / 31f, lastG / 31f, lastB / 31f);
                            GL.Vertex3(lastX, sliceHeight, lastY);
                            GL.Vertex3(lastX, 0, lastY);
                            GL.End();
                        }

                        lastX = x;
                        lastY = y;
                        lastR = r;
                        lastG = g;
                        lastB = b;

                    };


                    model.polygonBegin = pol =>
                    {
                        first = 1;
                    };

                    model.polygonEnd = pol =>
                    {
                        GL.Begin(PrimitiveType.Quads);
                        GL.Color3(lastR / 31f, lastG / 31f, lastB / 31f);
                        //GL.Color3(firstR / 31f, firstG / 31f, lastB / 31f);
                        //GL.Color3(lastR / 31f, lastG / 31f, lastB / 31f);
                        GL.Vertex3(firstX, 0, firstY);
                        GL.Vertex3(firstX, sliceHeight, firstY);


                        GL.Vertex3(lastX, sliceHeight, lastY);
                        GL.Vertex3(lastX, 0, lastY);


                        GL.End();
                    };

                    model.sliceBegin = (slice, height) =>
                    {
                        sliceHeight = height;

                    };

                    model.sliceEnd = (slice, height) =>
                    {
                        GL.Translate(0, height, 0);
                    };

                    // setup settings, load textures, sounds
                    game.VSync = VSyncMode.On;
                };

                game.Resize += (sender, e) =>
                {
                    GL.Viewport(0, 0, game.Width, game.Height);

                };


                game.UpdateFrame += (sender, e) =>
                {

                    // add game logic, input handling
                    if (game.Keyboard[Key.Escape])
                    {
                        game.Exit();
                    }


                    if (animation < 0) animation = model.animationsCount - 1;
                    if (animation >= model.animationsCount) animation = 0;

                    if (frame >= model.animations[animation].frameCount) frame = 0;
                    if (frame < 0) frame = model.animations[animation].frameCount - 1;


                    model.setupFrame(animation, frame, new BladeRunnerHelper.Vector3(), 0, 1);
                    Console.WriteLine("a={0,3},f={1,3},x={2,5},y={3,5},z={4,5},angle={5,5}", animation, frame, model.animations[animation].x, model.animations[animation].y, model.animations[animation].z, +model.animations[animation].angle);

                };


                game.KeyDown += (sender, e) =>
                {
                    switch (e.Key)
                    {
                        case Key.Down:
                            animation--;
                            break;
                        case Key.Up:
                            animation++;
                            break;
                        case Key.Left:
                            frame--;
                            break;
                        case Key.Right:
                            frame++;
                            break;
                    }

                };

                var i = 0;

                game.RenderFrame += (sender, e) =>
                {


                    //GL.Enable(EnableCap.CullFace);
                    GL.CullFace(CullFaceMode.Front);
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthFunc(DepthFunction.Less);
                    
                    GL.ClearColor(1, 1, 1, 0);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity();
                    GL.LineWidth(1);

                    GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 100.0);
                    GL.Translate(-0, -0.5f, -10f);
                    GL.Scale(renderScale, renderScale, renderScale);

                    GL.MatrixMode(MatrixMode.Modelview);
                    GL.LoadIdentity();
                    //GL.Rotate(45, 1, 0, 0);
                    GL.Rotate(i, 0, 1, 0);

                    i++;

                    model.drawFrame();


                    game.SwapBuffers();
                };

                // Run the game at 60 updates per second
                game.Run(60.0);
            }
        }
    }
}
