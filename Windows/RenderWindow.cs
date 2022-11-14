using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GLowScreensaver;

using Microsoft.Win32;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using SQLite;
using WindowState = OpenTK.WindowState;

namespace GLow_Screensaver.Windows
{
    public class RenderWindow : GameWindow
    {
        private int _program = 0;
        private int _fragmentShader = 0;
        private int _vertexShader = 0;
        private DateTime _startTime;
        private Point _mousePosition = new Point(0, 0);
        private DateTime _timeFPS = DateTime.Now;
        private uint _fps = 0;
        private static GraphicsMode _graphicsMode = new GraphicsMode(new ColorFormat(10,10,10,10), 32, GraphicsMode.Default.Stencil, 8, new ColorFormat(10, 10, 10, 10), 3);

        public RenderWindow()
            : base(1024, 768, _graphicsMode, "GLow", GameWindowFlags.Default, DisplayDevice.Default)
        {

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            WindowState = WindowState.Fullscreen;
            VSync = VSyncMode.On;
            InitializeShader();
            SetupViewPort();
            LoadShader();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Exit();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            Cursor = MouseCursor.Empty;
            _mousePosition.X = e.X;
            _mousePosition.Y = e.Y;

        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            Exit();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            Render();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Width, Height);
        }

        public void LoadShader()
        {
            RegistryKey folder = Registry.CurrentUser.CreateSubKey(@"Software\GLow Screensaver\");
            if (folder.GetValue("ShaderId") != null)
            {
                int shaderId = (int)folder.GetValue("ShaderId");
                SQLiteConnection db = Database.Instance.GetConnection();
                GLow_Screensaver.Data.ImageSource source = (from s in db.Table<GLow_Screensaver.Data.ImageSource>() where s.Id == shaderId select s).FirstOrDefault();
                if (source != null)
                    LoadShader(source.SourceCode);
            }
        }

        public void InitializeShader()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            // Starting time for iTime variable
            _startTime = Process.GetCurrentProcess().StartTime;

            // Prépare le vertex shader			
            _vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vertexShader, "void main(void) {gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;}");
            GL.CompileShader(_vertexShader);

            // Crée le programme
            _program = GL.CreateProgram();
            GL.AttachShader(_program, _vertexShader);
            GL.LinkProgram(_program);
            GL.UseProgram(_program);
        }

        public void SetupViewPort()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, Width, 0, Height, -1, 1);    //Bottom left corner pixel has corrdinate 0,0
            GL.Viewport(0, 0, Width, Height);
        }

        public void LoadShader(string imageSourceCode)
        {
            if (_program > 0)
            {
                var builder = new StringBuilder();
                builder.AppendLine("#version 150");
                builder.AppendLine("out vec4 out_frag_color;");
                builder.AppendLine("uniform vec4 iDate;");
                builder.AppendLine("uniform vec4 iMouse;");
                builder.AppendLine("uniform float iTime;");
                builder.AppendLine("uniform vec3 iResolution;");
                builder.AppendLine(imageSourceCode);
                builder.AppendLine("void main(void)");
                builder.AppendLine("{");
                builder.AppendLine("vec4 fragColor;");
                builder.AppendLine("mainImage(fragColor,gl_FragCoord.xy);");
                builder.AppendLine("out_frag_color=fragColor;");
                builder.AppendLine("}");

                // Delete the current fragment shader
                if (_fragmentShader > 0)
                {
                    GL.DetachShader(_program, _fragmentShader);
                    GL.DeleteShader(_fragmentShader);
                    _fragmentShader = 0;
                }

                // Create the new fragment shader
                _fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(_fragmentShader, builder.ToString());
                GL.CompileShader(_fragmentShader);

                // Check if some errors are existing. This method is not clean but i can't use the extensions of OpenGL.
                string result = GL.GetShaderInfoLog(_fragmentShader).Trim();
                if (result != "" && result.ToLower() != "no errors.")
                {
                    Debug.WriteLine(result);
                    // Unsupported for screensaver
                    //MessageBox.Show(result);
                }

                // Attach the fragment shader to the program
                GL.AttachShader(_program, _fragmentShader);
                GL.LinkProgram(_program);
                GL.UseProgram(_program);
            }
        }

        public void Render()
        {
            MakeCurrent();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            int iResolution = GL.GetUniformLocation(_program, "iResolution");
            if (iResolution != -1)
                GL.Uniform3(iResolution, Convert.ToSingle(Width), Convert.ToSingle(Height), 0);

            // Set the iTime variable used by the shader
            float timespan = (float)(DateTime.Now - _startTime).TotalSeconds;
            int iTime = GL.GetUniformLocation(_program, "iTime");
            if (iTime != -1) 
                GL.Uniform1(iTime, (float)timespan);

            // Set the iDate variable used by some shaders
            int iDate = GL.GetUniformLocation(_program, "iDate");
            if (iDate != -1) 
                GL.Uniform4(iDate, (float)DateTime.Now.Year, (float)DateTime.Now.Month, (float)DateTime.Now.Day, (float)DateTime.Now.TimeOfDay.TotalSeconds);

            // Set the position of the mouse iMouse used by the shader
            int iMouse = GL.GetUniformLocation(_program, "iMouse");
            if (iMouse != -1) 
                GL.Uniform4(iMouse, (float)_mousePosition.X, (float)((double)Height - _mousePosition.Y), (float)_mousePosition.X, (float)((double)Height - _mousePosition.Y));

            // Create 2 triangles where the fragment shader will be displayed
            GL.Begin(BeginMode.Quads);
            GL.Color3(0, 0, 0);
            GL.Vertex2(0, 0);
            GL.Vertex2(Width, 0);
            GL.Vertex2(Width, Height);
            GL.Vertex2(0, Height);
            GL.End();

            // Show the back buffer
            SwapBuffers();

            // Update the FPS
            if ((DateTime.Now - _timeFPS).TotalSeconds >= 1)
            {
                _timeFPS = DateTime.Now;
                //FPS.Text = "FPS:" + _fps; // FIXME Display the FPS
                _fps = 0;
            }
            _fps++;
        }

        public override void Exit()
        {
            base.Exit();

            GL.DeleteProgram(_program);
            GL.DeleteShader(_vertexShader);
            GL.DeleteShader(_fragmentShader);
        }

    }
}
