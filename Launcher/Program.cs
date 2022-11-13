using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher
{
    internal class Program
    {
        private static readonly string GLOW_EXE_PATH = @"c:\Program Files\GLow\GLow Screensaver.exe";

        static void Main(string[] args)
        {
            if (File.Exists(GLOW_EXE_PATH))
            {
                var startInfo = new ProcessStartInfo
                {
                    Arguments = args.Length > 0 ? String.Join(" ", args) : String.Empty,
                    CreateNoWindow = true,
                    FileName = GLOW_EXE_PATH,
                    WorkingDirectory = Path.GetDirectoryName(GLOW_EXE_PATH),
                    UseShellExecute = false
                };

                var process = Process.Start(startInfo);
                process.WaitForExit();
            }
        }
    }
}
