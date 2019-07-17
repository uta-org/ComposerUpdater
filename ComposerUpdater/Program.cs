using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Utils;
using Console = Colorful.Console;

namespace ComposerUpdater
{
    internal class Program
    {
        /// <summary>
        /// Gets or sets the ANSI console.
        /// </summary>
        /// <value>
        /// The ANSI console.
        /// </value>
        private static AnsiConsole AnsiConsole { get; set; }

        //private static string GitPath { get; set; } = @"C:\Program Files\Git\bin\git.exe";

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
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

                                CreateProcess(gitPath, $@"submodule add {urlString} ""{pathString}""", workingDir, GitProcessOnOutputDataReceived);

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
                const int slashLimit = 1;
                const string match = "utilidades";

                string composerPath = @"C:\ProgramData\ComposerSetup\bin\composer.phar",
                       phpPath = @"C:\xampp\php\php.exe";

                if (CheckForApp(ref composerPath, "Composer"))
                    return;

                if (CheckForApp(ref phpPath, "PHP"))
                    return;

                string webDir = Path.Combine(workingDir, "web");

                var composerFiles = Directory.GetFiles(webDir, "*.json", SearchOption.AllDirectories).Where(file =>
                    Path.GetFileNameWithoutExtension(file)?.ToLowerInvariant() == "composer");

                foreach (var file in composerFiles)
                {
                    CreateProcess(phpPath, $@"{composerPath} install", Path.GetDirectoryName(file), process =>
                    {
                        string wDir = process.StartInfo.WorkingDirectory.ToLowerInvariant();

                        if (!wDir.Contains(match))
                        {
                            Console.WriteLine($"Skipped process at '{process.StartInfo.WorkingDirectory}'",
                                Color.Yellow);

                            return true;
                        }

                        if (Regex.Matches(wDir.Substring(wDir.IndexOf(match)), @"\\").Count > slashLimit)
                        {
                            Console.WriteLine("Skipped sub-composer...");

                            return true;
                        }

                        return false;
                    }, null);
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

        /// <summary>
        /// Process output data received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataReceivedEventArgs"/> instance containing the event data.</param>
        private static void GitProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        /// <summary>
        /// Processes the error data received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataReceivedEventArgs"/> instance containing the event data.</param>
        /// <param name="displayRed">if set to <c>true</c> [display red].</param>
        private static void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e, bool displayRed)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine();
                return;
            }

            if (displayRed)
                Console.WriteLine(e.Data, Color.Red);
            else
            {
                Action<string> consoleAction =
                    AnsiConsole == null ? (Action<string>)(msg => Console.WriteLine(msg)) : msg => AnsiConsole.WriteLine(msg);

                consoleAction(e.Data);
            }
        }

        /// <summary>
        /// Gets the executing string.
        /// </summary>
        /// <param name="info">The information.</param>
        private static void GetExecutingString(ProcessStartInfo info)
        {
            Console.WriteLine($"Executing: {info.FileName} {info.Arguments} at '{info.WorkingDirectory}'");
        }

        /// <summary>
        /// Creates the process.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="workingDir">The working dir.</param>
        /// <param name="outputHandler">The output handler.</param>
        private static void CreateProcess(string fileName, string arguments, string workingDir, DataReceivedEventHandler outputHandler)
        {
            CreateProcess(fileName, arguments, workingDir, null, outputHandler);
        }

        /// <summary>
        /// Creates the process.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="workingDir">The working dir.</param>
        /// <param name="continueFunc">The continue function.</param>
        /// <param name="outputHandler">The output handler.</param>
        /// <exception cref="ArgumentNullException">
        /// fileName
        /// or
        /// arguments
        /// or
        /// workingDir
        /// </exception>
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

                // process.OutputDataReceived += outputHandler ?? ((sender, e) => ProcessOnErrorDataReceived(sender, e, false));
                process.ErrorDataReceived += (sender, e) => ProcessOnErrorDataReceived(sender, e, outputHandler != null);
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
            }
        }

        /// <summary>
        /// Checks for application.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="appName">Name of the application.</param>
        /// <returns></returns>
        private static bool CheckForApp(ref string path, string appName)
        {
            if (!File.Exists(path))
            {
                Console.Write($"{appName} not found (at '{path}')... Please, specify its path (file path) manually: ", Color.Red);
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
    }
}