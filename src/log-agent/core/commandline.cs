// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace LogAgent
{
    /// <summary>
    /// Main application class
    /// system.commandline code
    /// </summary>
    public sealed partial class App
    {
        /// <summary>
        /// Process the command line and run the app
        /// </summary>
        /// <returns>int 0 == success</returns>
        public static async Task<int> RunApp(string[] args)
        {
            // create root command
            RootCommand root = new RootCommand
            {
                Name = "logagent",
                Description = "Sample Log Analytics Agent",
                TreatUnmatchedTokensAsErrors = true,
            };

            // add the options
            root.AddOption(new Option<string>(new string[] { "--workspace-id", "-w" }, "Log Analytics Workspace") { IsRequired = true });
            root.AddOption(new Option<string>(new string[] { "--shared-key", "-k" }, "Log Analytics Key") { IsRequired = true });
            root.AddOption(new Option<string>(new string[] { "--log-name", "-n" }, "Log Analytics Log Name") { IsRequired = true });
            root.AddOption(new Option<int>(new string[] { "--delay", "-d" }, () => 10, "Delay in seconds"));
            root.AddOption(new Option<bool>(new string[] { "--dry-run", "-r" }, "Validates configuration"));

            root.Handler = CommandHandler.Create<string, string, string, int, bool>(AppWorker);

            // merge enviroment variables
            string[] cmd = CombineEnvVarsWithCommandLine(args);

            // run the app
            return await root.InvokeAsync(cmd).ConfigureAwait(false);
        }

        /// <summary>
        /// Run the app
        /// </summary>
        /// <param name="workspaceId">Log Analytics Workspace</param>
        /// <param name="sharedKey">Log Analytics Key</param>
        /// <param name="logName">Log Analytics Log Name</param>
        /// <param name="delay">delay in seconds between log checks</param>
        /// <param name="dryRun">Dry Run flag</param>
        /// <returns>status</returns>
        public static async Task<int> AppWorker(string workspaceId, string sharedKey, string logName, int delay, bool dryRun)
        {
            try
            {
                // show startup messages
                DisplayAsciiArt();

                // enforce delay min / max
                delay = delay < 5 ? 5 : delay > 60 ? 60 : delay;

                // set values from command line
                Config.WorkspaceId = workspaceId;
                Config.SharedKey = sharedKey;
                Config.LogName = logName;
                Config.Delay = delay;

                // dry run
                if (dryRun)
                {
                    return DoDryRun();
                }

                // setup ctl c handler
                SetupCtlCHandler();

                // run log loop until ctl-c or stop event
                await LogLoop(logName).ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                // end app on error
                Console.WriteLine($"Error in Main() {ex.Message}");

                return -1;
            }
        }

        /// <summary>
        /// Combine env vars and command line values
        /// </summary>
        /// <param name="args">command line args</param>
        /// <returns>string[]</returns>
        public static string[] CombineEnvVarsWithCommandLine(string[] args)
        {
            if (args == null)
            {
                args = Array.Empty<string>();
            }

            List<string> cmd = new List<string>(args);

            // add --worspace-id value from environment or default
            if (!cmd.Contains("--workspace-id") && !cmd.Contains("-w"))
            {
                string val = Environment.GetEnvironmentVariable("WorkspaceId");

                if (!string.IsNullOrEmpty(val))
                {
                    cmd.Add("--workspace-id");
                    cmd.Add(val);
                }
            }

            // add --shared-key value from environment or default
            if (!cmd.Contains("--shared-key") && !cmd.Contains("-k"))
            {
                string val = Environment.GetEnvironmentVariable("SharedKey");

                if (!string.IsNullOrEmpty(val))
                {
                    cmd.Add("--shared-key");
                    cmd.Add(val);
                }
            }

            // add --log-name value from environment or default
            if (!cmd.Contains("--log-name") && !cmd.Contains("-n"))
            {
                string val = Environment.GetEnvironmentVariable("LogName");

                if (!string.IsNullOrEmpty(val))
                {
                    cmd.Add("--log-name");
                    cmd.Add(val);
                }
            }

            // add --delay value from environment or default
            if (!cmd.Contains("--delay") && !cmd.Contains("-d"))
            {
                string val = Environment.GetEnvironmentVariable("Delay");

                if (!string.IsNullOrEmpty(val))
                {
                    cmd.Add("--delay");
                    cmd.Add(val);
                }
            }

            return cmd.ToArray();
        }

        /// <summary>
        /// Display the ASCII art file if it exists
        /// </summary>
        private static void DisplayAsciiArt()
        {
            const string file = "./core/ascii-art.txt";

            if (File.Exists(file))
            {
                Console.WriteLine(File.ReadAllText(file));
            }

            // print version info
            Console.WriteLine($"Version: {Version}\n\n");
        }

        /// <summary>
        /// Display the dry run message
        /// </summary>
        /// <returns>0 (success)</returns>
        private static int DoDryRun()
        {
            // display config
            Console.WriteLine("Dry Run:");
            Console.WriteLine($"  Node Name       {Config.NodeName}");
            Console.WriteLine($"  Pod Name        {Config.PodName}");
            Console.WriteLine($"  Pod Namespace   {Config.PodNamespace}");
            Console.WriteLine($"  Log Name        {Config.LogName}");
            Console.WriteLine($"  Log Name        {Config.LogName}");
            Console.WriteLine($"  Workspace       {Config.WorkspaceId}");
            Console.WriteLine($"  Shared Key      len({Config.SharedKey.Length})");
            Console.WriteLine($"  Delay           {Config.Delay} (seconds)");

            // always return 0 (success)
            return 0;
        }

        /// <summary>
        /// Creates a ctl-c handler
        /// </summary>
        private static void SetupCtlCHandler()
        {
            // ctl-c handler
            Console.CancelKeyPress += async (sender, e) =>
            {
                Console.WriteLine("Ctl-C Pressed - Starting shutdown ...");

                // signal the threads
                e.Cancel = true;
                ctCancel.Cancel();

                // give the app a chance to finish
                await Task.Delay(200).ConfigureAwait(false);

                // end the app
                Environment.Exit(0);
            };
        }
    }
}
