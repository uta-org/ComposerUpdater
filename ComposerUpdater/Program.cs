using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using System.Drawing;
using Console = Colorful.Console;

namespace ComposerUpdater
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                string webdir = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), "web");

                var composerFiles = Directory.GetFiles(webdir, "*.json", SearchOption.AllDirectories).Where(file =>
                    Path.GetFileNameWithoutExtension(file)?.ToLowerInvariant() == "composer");

                foreach (var file in composerFiles)
                {
                    // Process.Start("composer", "install")
                    using (var process =
                        new Process
                        {
                            StartInfo =
                            new ProcessStartInfo("composer", "install")
                            {
                                WorkingDirectory = Path.GetDirectoryName(file),
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true
                            }
                        })
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        Console.WriteLine(output);

                        process.WaitForExit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex, Color.Red);

                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("All composer packages installed succesfully!", Color.Lime);

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}