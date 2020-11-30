// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace CSE.NextGenSymmetricApp
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
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

            // add --log-level value from environment or default
            if (!cmd.Contains("--log-level") && !cmd.Contains("-l"))
            {
                string logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");

                cmd.Add("--log-level");
                cmd.Add(string.IsNullOrEmpty(logLevel) ? "Warning" : logLevel);
                App.IsLogLevelSet = !string.IsNullOrEmpty(logLevel);
            }
            else
            {
                App.IsLogLevelSet = true;
            }

            return cmd.ToArray();
        }

        /// <summary>
        /// Build the RootCommand for parsing
        /// </summary>
        /// <returns>RootCommand</returns>
        public static RootCommand BuildRootCommand()
        {
            RootCommand root = new RootCommand
            {
                Name = "ngsa",
                Description = "Next Gen Symmetric App",
                TreatUnmatchedTokensAsErrors = true,
            };

            // add the options
            root.AddOption(new Option<LogLevel>(new string[] { "-l", "--log-level" }, "Log Level"));
            root.AddOption(new Option<bool>(new string[] { "-d", "--dry-run" }, "Validates configuration"));

            return root;
        }

        /// <summary>
        /// Run the app
        /// </summary>
        /// <param name="secretsVolume">k8s Secrets Volume Path</param>
        /// <param name="logLevel">Log Level</param>
        /// <param name="dryRun">Dry Run flag</param>
        /// <param name="inMemory">Use in-memory DB</param>
        /// <returns>status</returns>
        public static async Task<int> RunApp(string secretsVolume, LogLevel logLevel, bool dryRun, bool inMemory)
        {
            try
            {
                Region = Environment.GetEnvironmentVariable("Region");
                Zone = Environment.GetEnvironmentVariable("Zone");
                PodType = Environment.GetEnvironmentVariable("PodType");

                if (string.IsNullOrEmpty(PodType))
                {
                    PodType = "ngsa";
                }

                // setup ctl c handler
                ctCancel = SetupCtlCHandler();

                AppLogLevel = logLevel;

                // build the host
                host = BuildHost();

                if (host == null)
                {
                    return -1;
                }

                // don't start the web server
                if (dryRun)
                {
                    return DoDryRun();
                }

                // log startup messages
                LogStartup();

                // start the webserver
                Task w = host.RunAsync();

                // this doesn't return except on ctl-c
                await w.ConfigureAwait(false);

                // if not cancelled, app exit -1
                return ctCancel.IsCancellationRequested ? 0 : -1;
            }
            catch (Exception ex)
            {
                // end app on error
                if (logger != null)
                {
                    logger.LogError($"Exception: {ex}");
                }
                else
                {
                    Console.WriteLine($"Error in Main() {ex.Message}");
                }

                return -1;
            }
        }

        /// <summary>
        /// Display the dry run message
        /// </summary>
        /// <param name="authType">authentication type</param>
        /// <returns>0</returns>
        private static int DoDryRun()
        {
            Console.WriteLine($"Version            {Middleware.VersionExtension.Version}");
            Console.WriteLine($"Log Level          {AppLogLevel}");

            // always return 0 (success)
            return 0;
        }
    }
}
