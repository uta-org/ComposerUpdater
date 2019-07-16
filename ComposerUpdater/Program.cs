using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ComposerUpdater
{
    internal class Program
    {
        private static void Main(string[] args)
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

            Console.WriteLine("All composer packages installed succesfully!");
            Console.ReadLine();
        }
    }
}