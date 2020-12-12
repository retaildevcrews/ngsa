// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp.Extensions;
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
        ///   env vars take precedent
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

            // add values from environment
            cmd.AddFromEnvironment("--in-memory");
            cmd.AddFromEnvironment("--no-cache");
            cmd.AddFromEnvironment("--perf-cache");
            cmd.AddFromEnvironment("--secrets-volume");
            cmd.AddFromEnvironment("--log-level", "-l");

            // was log level set
            App.IsLogLevelSet = cmd.Contains("--log-level") || cmd.Contains("-l");

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
                Name = "DataService",
                Description = "NGSA Data Service",
                TreatUnmatchedTokensAsErrors = true,
            };

            // add the options
            root.AddOption(new Option<bool>(new string[] { "--in-memory" }, "Use in-memory database"));
            root.AddOption(new Option<bool>(new string[] { "--no-cache" }, "Don't cache results"));
            root.AddOption(new Option<int>(new string[] { "--perf-cache" }, "Cache only when load exceeds value"));
            root.AddOption(new Option<string>(new string[] { "--secrets-volume" }, () => "secrets", "Secrets Volume Path"));
            root.AddOption(new Option<LogLevel>(new string[] { "-l", "--log-level" }, () => LogLevel.Warning, "Log Level"));
            root.AddOption(new Option<bool>(new string[] { "-d", "--dry-run" }, "Validates configuration"));

            // validate dependencies
            root.AddValidator(ValidateDependencies);

            return root;
        }

        /// <summary>
        /// Run the app
        /// </summary>
        /// <param name="secretsVolume">k8s Secrets Volume Path</param>
        /// <param name="logLevel">Log Level</param>
        /// <param name="dryRun">Dry Run flag</param>
        /// <param name="inMemory">Use in-memory DB</param>
        /// <param name="noCache">don't cache results</param>
        /// <param name="perfCache">cache results under load</param>
        /// <returns>status</returns>
        public static async Task<int> RunApp(string secretsVolume, LogLevel logLevel, bool dryRun, bool inMemory, bool noCache, int perfCache)
        {
            try
            {
                // assign command line values
                InMemory = inMemory;
                NoCache = noCache;
                PerfCache = perfCache;

                Region = Environment.GetEnvironmentVariable("Region");
                Zone = Environment.GetEnvironmentVariable("Zone");
                PodType = Environment.GetEnvironmentVariable("PodType");

                if (string.IsNullOrEmpty(PodType))
                {
                    PodType = "ngsa-ds";
                }

                if (inMemory)
                {
                    Secrets = new Secrets
                    {
                        UseInMemoryDb = true,
                        AppInsightsKey = string.Empty,
                        CosmosCollection = "movies",
                        CosmosDatabase = "imdb",
                        CosmosKey = "in-memory",
                        CosmosServer = "in-memory",
                    };
                }
                else
                {
                    Secrets = Secrets.GetSecretsFromVolume(secretsVolume);

                    // set the Cosmos server name for logging
                    CosmosName = Secrets.CosmosServer.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);

                    // todo - get this from cosmos query
                    CosmosQueryId = "todo";

                    int ndx = CosmosName.IndexOf('.', StringComparison.OrdinalIgnoreCase);

                    if (ndx > 0)
                    {
                        CosmosName = CosmosName.Remove(ndx);
                    }
                }

                // setup ctl c handler
                ctCancel = SetupCtlCHandler();

                AppLogLevel = logLevel;

                // load the cache
                CacheDal = new DataAccessLayer.InMemoryDal();

                // create the cosomos data access layer
                if (App.Secrets.UseInMemoryDb)
                {
                    CosmosDal = CacheDal;
                }
                else
                {
                    CosmosDal = new DataAccessLayer.CosmosDal(new Uri(Secrets.CosmosServer), Secrets.CosmosKey, Secrets.CosmosDatabase, Secrets.CosmosCollection);
                }

                // build the host
                host = BuildHost();

                if (host == null)
                {
                    return -1;
                }

                // display dry run message
                if (dryRun)
                {
                    return DoDryRun();
                }

                // log startup messages
                LogStartup();

                // start the webserver
                Task w = host.RunAsync();

                // start request count timer
                Middleware.Logger.StartCounterTime(10000, 5000);

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

        // validate combinations of parameters
        private static string ValidateDependencies(CommandResult result)
        {
            string msg = string.Empty;

            try
            {
                int? perfCache = !(result.Children.FirstOrDefault(c => c.Symbol.Name == "perf-cache") is OptionResult perfCacheRes) ? null : perfCacheRes.GetValueOrDefault<int?>();
                bool inMemory = !(result.Children.FirstOrDefault(c => c.Symbol.Name == "in-memory") is OptionResult inMemoryRes) ? false : inMemoryRes.GetValueOrDefault<bool>();
                bool noCache = !(result.Children.FirstOrDefault(c => c.Symbol.Name == "no-cache") is OptionResult noCacheRes) ? false : noCacheRes.GetValueOrDefault<bool>();
                string secrets = !(result.Children.FirstOrDefault(c => c.Symbol.Name == "secrets-volume") is OptionResult secretsRes) ? string.Empty : secretsRes.GetValueOrDefault<string>();

                // validate secrets volume
                if (string.IsNullOrWhiteSpace(secrets))
                {
                    msg += "--secrets-volume cannot be empty\n";
                }

                try
                {
                    // validate secrets-volume exists
                    if (!Directory.Exists(secrets))
                    {
                        msg += $"--secrets-volume ({secrets}) does not exist\n";
                    }
                }
                catch (Exception ex)
                {
                    msg += $"--secrets-volume exception: {ex.Message}\n";
                }

                // invalid combination
                if (inMemory && noCache)
                {
                    msg += "--in-memory and --no-cache are exclusive\n";
                }

                if (perfCache != null)
                {
                    // validate perfCache > 0
                    if (perfCache < 1)
                    {
                        msg += "--perf-cache must be > 0\n";
                    }

                    // invalid combination
                    if (inMemory)
                    {
                        msg += "--perf-cache and --in-memory are exclusive\n";
                    }

                    // invalid combination
                    if (noCache)
                    {
                        msg += "--perf-cache and --no-cache are exclusive\n";
                    }
                }
            }
            catch
            {
                // system.commandline will catch and display parse exceptions
            }

            // return error message(s) or string.empty
            return msg;
        }

        // Display the dry run message
        private static int DoDryRun()
        {
            Console.WriteLine($"Version            {Middleware.VersionExtension.Version}");
            Console.WriteLine($"Log Level          {AppLogLevel}");
            Console.WriteLine($"In Memory          {InMemory}");
            Console.WriteLine($"No Cache           {NoCache}");
            Console.WriteLine($"Perf Cache         {PerfCache}");
            Console.WriteLine($"Secrets Volume     {Secrets.Volume}");
            Console.WriteLine($"Use in memory DB   {Secrets.UseInMemoryDb}");
            Console.WriteLine($"Cosmos Server      {Secrets.CosmosServer}");
            Console.WriteLine($"Cosmos Database    {Secrets.CosmosDatabase}");
            Console.WriteLine($"Cosmos Collection  {Secrets.CosmosCollection}");
            Console.WriteLine($"Cosmos Key         Length({Secrets.CosmosKey.Length})");
            Console.WriteLine($"App Insights Key   Length({Secrets.AppInsightsKey.Length})");

            // always return 0 (success)
            return 0;
        }
    }
}
