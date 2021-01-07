// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Ngsa.Middleware;

namespace Ngsa.App
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
        public static List<string> CombineEnvVarsWithCommandLine(string[] args)
        {
            if (args == null)
            {
                args = Array.Empty<string>();
            }

            List<string> cmd = new List<string>(args);

            cmd.AddFromEnvironment("--data-service", "-s");
            cmd.AddFromEnvironment("--log-level", "-l");

            IsLogLevelSet = cmd.Contains("--log-level") || cmd.Contains("-l");

            return cmd;
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
            root.AddOption(new Option<string>(new string[] { "-s", "--data-service" }, () => "http://localhost:4122", "Data Service URL"));
            root.AddOption(new Option<LogLevel>(new string[] { "-l", "--log-level" }, "Log Level"));
            root.AddOption(new Option<bool>(new string[] { "-d", "--dry-run" }, "Validates configuration"));

            return root;
        }

        /// <summary>
        /// Run the app
        /// </summary>
        /// <param name="dataService">Data Service URL (default: http://localhost:4122)</param>
        /// <param name="logLevel">Log Level</param>
        /// <param name="dryRun">Dry Run flag</param>
        /// <returns>status</returns>
        public static async Task<int> RunApp(string dataService, LogLevel logLevel, bool dryRun)
        {
            try
            {
                Region = Environment.GetEnvironmentVariable("Region");
                Zone = Environment.GetEnvironmentVariable("Zone");
                PodType = Environment.GetEnvironmentVariable("PodType");

                if (string.IsNullOrEmpty(PodType))
                {
                    PodType = "Ngsa.App";
                }

                if (string.IsNullOrEmpty(Region))
                {
                    Region = "dev";
                }

                if (string.IsNullOrEmpty(Zone))
                {
                    Zone = "dev";
                }

                // setup ctl c handler
                ctCancel = SetupCtlCHandler();

                AppLogLevel = logLevel;
                DataService = dataService;

                // set the logger info
                RequestLogger.CosmosName = string.Empty;
                RequestLogger.PodType = PodType;
                RequestLogger.Region = Region;
                RequestLogger.Zone = Zone;

                // remove prefix and suffix
                RequestLogger.DataService = DataService;
                RequestLogger.DataService = RequestLogger.DataService.Replace("https://", string.Empty).Replace("http://", string.Empty);

                // add pod, region, zone info to logger
                Logger.EnrichLog();

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
                if (Logger != null)
                {
                    Logger.LogError($"Exception: {ex}");
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
            Console.WriteLine($"Version            {Ngsa.Middleware.VersionExtension.Version}");
            Console.WriteLine($"Log Level          {AppLogLevel}");

            // always return 0 (success)
            return 0;
        }
    }
}
