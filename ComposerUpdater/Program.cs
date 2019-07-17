using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using System.Drawing;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Protocol.Plugins;
using Console = Colorful.Console;

namespace ComposerUpdater
{
    internal class Program
    {
        private static AnsiConsole AnsiConsole { get; set; }

        //private static string GitPath { get; set; } = @"C:\Program Files\Git\bin\git.exe";

        private static void Main(string[] args)
        {
            string workingDir = Path.GetDirectoryName(Environment.CurrentDirectory);

            Console.Write("Do you want to re-added all the submodules? [y/N]: ");
            if (Console.ReadLine().ToLowerInvariant() == "y")
            {
                const string pathStart = "path",
                    urlStart = "url";

                string submodulePath = Path.Combine(workingDir, "old.gitmodules");

                if (!File.Exists(submodulePath))
                {
                    // TODO: mv it
                    Console.WriteLine("No old submodules detected, skipping...", Color.Yellow);
                }
                else
                {
                    string gitPath = @"C:\Program Files\Git\bin\git.exe";

                    if (CheckForApp(ref gitPath, "Git"))
                        return;

                    var lines = File.ReadAllLines(submodulePath);

                    var parts = new List<string>();
                    foreach (var line in lines)
                    {
                        string trimmedLine = line.ToLowerInvariant().Trim();

                        if (string.IsNullOrEmpty(trimmedLine))
                            continue;

                        bool isPath = trimmedLine.Contains(pathStart);
                        bool isUrl = trimmedLine.Contains(urlStart);

                        if (isPath || isUrl)
                            parts.Add(trimmedLine);
                        else
                        {
                            if (parts.Count == 2)
                            {
                                string literalFormat = "{0} = ";

                                string pathString = parts[0].Replace(string.Format(literalFormat, pathStart),
                                    string.Empty),
                                       urlString = parts[1].Replace(string.Format(literalFormat, urlStart),
                                    string.Empty);

                                CreateProcess(gitPath, $@"submodule add {urlString} ""{pathString}""", workingDir, null, GitProcessOnOutputDataReceived);

                                // Console.WriteLine($"Adding submodule from URL --> '{urlString}' at path --> '{pathString}'");
                                parts.Clear();
                            }
                        }
                    }
                }
            }

            AnsiConsole = AnsiConsole.GetOutput();

            try
            {
                const int slashLimit = 2;
                const string match = "utilidades";
                string composerPath = @"C:\ProgramData\ComposerSetup\bin\composer.bat";

                if (CheckForApp(ref composerPath, "Composer"))
                    return;

                string webdir = Path.Combine(workingDir, "web");

                var composerFiles = Directory.GetFiles(webdir, "*.json", SearchOption.AllDirectories).Where(file =>
                    Path.GetFileNameWithoutExtension(file)?.ToLowerInvariant() == "composer");

                foreach (var file in composerFiles)
                {
                    CreateProcess("cmd", $@"/c ""{composerPath}"" install", Path.GetDirectoryName(file), process =>
                    {
                        if (!process.StartInfo.WorkingDirectory.ToLowerInvariant().Contains(match))
                        {
                            Console.WriteLine($"Skipped process at '{process.StartInfo.WorkingDirectory}'",
                                Color.Yellow);
                            return true;
                        }

                        return false;
                    }, null);
                    //continue;
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

        private static bool CheckForApp(ref string path, string appName)
        {
            if (!File.Exists(path))
            {
                Console.Write($"{appName} not found please, specify its path (file path) manually: ", Color.Red);
                string appPath = Console.ReadLine();

                if (!File.Exists(path))
                {
                    Console.WriteLine($"Can't continue without the {appName} executable path, please retry restarting this app.",
                        Color.Red);
                    return true;
                }

                path = appPath;
            }

            return false;
        }

        private static void GitProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private static void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e, bool displayRed)
        {
            //if (string.IsNullOrEmpty(e.Data))
            //    return;

            if (displayRed)
                Console.WriteLine(e.Data, Color.Red);
            else
                Console.WriteLine(e.Data);
        }

        private static void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (string.IsNullOrEmpty(e.Data))
            //    return;

            // Collect the sort command output.
            AnsiConsole.WriteLine(e.Data);
        }

        private static void GetExecutingString(ProcessStartInfo info)
        {
            Console.WriteLine($"Executing: {info.FileName} {info.Arguments} at '{info.WorkingDirectory}'");
        }

        private static void CreateProcess(string fileName, string arguments, string workingDir,
            Func<Process, bool> continueFunc, DataReceivedEventHandler outputHandler)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (string.IsNullOrEmpty(arguments))
                throw new ArgumentNullException(nameof(arguments));

            if (string.IsNullOrEmpty(workingDir))
                throw new ArgumentNullException(nameof(workingDir));

            using (var process =
                new Process
                {
                    StartInfo =
                        new ProcessStartInfo(fileName, arguments)
                        {
                            WorkingDirectory = workingDir,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                })
            {
                if (continueFunc?.Invoke(process) == true)
                    return;

                GetExecutingString(process.StartInfo);

                process.OutputDataReceived += outputHandler ?? ((sender, e) => ProcessOnErrorDataReceived(sender, e, false));
                process.ErrorDataReceived += (sender, e) => ProcessOnErrorDataReceived(sender, e, true);
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
            }
        }
    }
}