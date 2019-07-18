using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Console = Colorful.Console;

namespace ComposerUpdater
{
    internal class Program
    {
        private static Color PromptColor => Color.DodgerBlue;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            if (!EnableConsoleColors())
                return;

            string workingDir = Path.GetDirectoryName(Environment.CurrentDirectory);
            string gitPath = @"C:\Program Files\Git\mingw64\bin\git.exe";

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

            Console.Write("Do you want to update all the composer modules? [y/N]: ");
            if (Console.ReadLine().ToLowerInvariant() == "n")
            {
                AnyKeyToExit();
                return;
            }

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
                    string wDir = Path.GetDirectoryName(file);
                    CreateProcess(phpPath, $@"{composerPath} install --ansi", wDir, () =>
                    {
                        string _wDir = wDir.ToLowerInvariant();

                        if (!_wDir.Contains(match))
                        {
                            Console.WriteLineFormatted("Skipped process at '{0}'",
                                PromptColor, Color.Yellow, wDir);

                            return true;
                        }

                        if (Regex.Matches(_wDir.Substring(_wDir.IndexOf(match)), @"\\").Count > slashLimit)
                        {
                            Console.WriteLineFormatted("Skipped sub-composer at '{0}'...", PromptColor, Color.White, wDir);
                            return true;
                        }

                        return false;
                    }, null);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex, Color.Red);

                AnyKeyToExit();
                return;
            }

            Console.WriteLine("All composer packages installed succesfully!", Color.Lime);

            AnyKeyToExit();
        }

        /// <summary>
        /// Press any the key to exit.
        /// </summary>
        private static void AnyKeyToExit()
        {
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
            System.Console.WriteLine(e.Data);
        }

        /// <summary>
        /// Processes the error data received.
        /// </summary>
        /// <param name="e">The <see cref="DataReceivedEventArgs"/> instance containing the event data.</param>
        /// <param name="displayRed">if set to <c>true</c> [display red].</param>
        private static void ProcessOnErrorDataReceived(DataReceivedEventArgs e, bool displayRed)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine();
                return;
            }

            if (displayRed)
            {
                Console.WriteLine(e.Data, Color.Red);
            }
            else
            {
                System.Console.WriteLine(e.Data);
            }
        }

        /// <summary>
        /// Gets the executing string.
        /// </summary>
        /// <param name="info">The information.</param>
        private static void GetExecutingString(ProcessStartInfo info)
        {
            Console.WriteLineFormatted("Executing: '{0}' at '{1}'", PromptColor, Color.White, $"{info.FileName} {info.Arguments}", info.WorkingDirectory);
        }

        /// <summary>
        /// Creates the process.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="workingDir">The working dir.</param>
        private static void CreateProcess(string fileName, string arguments, string workingDir)
        {
            CreateProcess(fileName, arguments, workingDir, null);
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
            Func<bool> continueFunc, DataReceivedEventHandler outputHandler)
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
                if (continueFunc?.Invoke() == true)
                    return;

                GetExecutingString(process.StartInfo);

                process.OutputDataReceived += outputHandler ?? ((sender, e) => ProcessOnErrorDataReceived(e, false));
                process.ErrorDataReceived += (sender, e) => ProcessOnErrorDataReceived(e, outputHandler != null);
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

        #region "Interoperability"

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr handle, out int mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int handle);

        public static bool EnableConsoleColors()
        {
            var handle = GetStdHandle(-11);
            int mode;
            return GetConsoleMode(handle, out mode) &&
                SetConsoleMode(handle, mode | 0x4);
        }

        #endregion "Interoperability"
    }
}