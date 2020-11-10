// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogAnalytics.Client;

namespace LogAgent
{
    /// <summary>
    /// Main application class
    /// log processing
    /// </summary>
    public sealed partial class App
    {
        // setup and run log loop
        private static async Task<int> LogLoop(string logName)
        {

            // don't do anything if k8s variables not set
            // todo - should we fail or should we just not do any logging?
            if (string.IsNullOrEmpty(Config.NodeName) ||
                string.IsNullOrEmpty(Config.PodName) ||
                string.IsNullOrEmpty(Config.PodIP) ||
                string.IsNullOrEmpty(Config.PodNamespace))
            {
                Console.WriteLine("Error: Environment Variables Not Set\n  NodeName\n  PodName\n  PodNamespace\n  PodIP");
                Console.WriteLine("\nLogs are not being processed");

                // do nothing
                await Task.Delay(-1).ConfigureAwait(false);

                return 0;
            }

            // startup message
            Console.WriteLine($"Logging to: {logName}\nDelay: {Config.Delay}s\n");

            // run a loop forever
            return await LogLoopWorker(Config.LogName).ConfigureAwait(false);
        }

        // run the log loop
        private static async Task<int> LogLoopWorker(string logName)
        {
            // create client
            LogAnalyticsClient LogClient = new LogAnalyticsClient(Config.WorkspaceId, Config.SharedKey);

            // track log items
            // todo - make this extensible
            List<NgsaLog> ngsaList = new List<NgsaLog>();

            HashSet<string> hash = new HashSet<string>();

            NgsaLog ngsaLog;

            // check logs every delay seconds
            while (true)
            {
                // stop signal received
                if (ctCancel.IsCancellationRequested)
                {
                    return 0;
                }

                // get the latest logs
                string s = GetLogs();

                if (string.IsNullOrEmpty(s))
                {
                    // no logs read
                    Console.WriteLine($"Log count: 0");
                }
                else
                {
                    string[] logs = s.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (string logline in logs)
                    {
                        // don't duplicate log entries
                        if (!hash.Contains(logline))
                        {
                            hash.Add(logline);

                            ngsaLog = CreateLogItem(logline);

                            // add to list
                            if (ngsaLog != null && ngsaLog.Category != "Ignore")
                            {
                                ngsaList.Add(ngsaLog);
                            }
                        }
                    }

                    // send to Log Analytics
                    if (ngsaList.Count > 0)
                    {
                        await LogClient.SendLogEntries<NgsaLog>(ngsaList, logName).ConfigureAwait(false);
                        Console.WriteLine($"Log count: {ngsaList.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"Log count: {ngsaList.Count}");
                    }

                    // reload the hash with the last logs
                    hash.Clear();

                    foreach (string logline in logs)
                    {
                        hash.Add(logline);
                    }

                    // clear the list
                    ngsaList.Clear();
                }

                // sleep for Delay seconds
                await Task.Delay(Config.Delay * 1000).ConfigureAwait(false);
            }
        }

        // create an NgsaLog from a log line
        private static NgsaLog CreateLogItem(string logline)
        {
            NgsaLog logItem = null;

            string[] log = logline.Split('\t');

            if (log.Length == 13 || log.Length == 12)
            {
                // common parsing
                logItem = new NgsaLog
                {
                    NodeName = Config.NodeName,
                    PodName = Config.PodName,
                    PodNamespace = Config.PodNamespace,
                    Region = Config.Region,
                    Zone = Config.Zone,
                    Date = DateTime.TryParse(log[0], out DateTime dt) ? dt : DateTime.UtcNow,
                };

                // webv log
                if (log.Length == 12)
                {
                    // create a new log and add to list
                    logItem.PodType = "webv";
                    logItem.UserAgent = "webv/host";

                    logItem.Server = log[1];
                    logItem.StatusCode = int.TryParse(log[2], out int sc) ? sc : 200;
                    logItem.ErrorCount = int.TryParse(log[3], out int ec) ? ec : 0;
                    logItem.Duration = double.TryParse(log[4], out double d) ? d : 0;
                    logItem.ContentLength = long.TryParse(log[5], out long l) ? l : 0;
                    logItem.CorrelationVector = log[6];
                    logItem.Tag = log[7] == "-" ? string.Empty : log[7];
                    logItem.Quartile = int.TryParse(log[8], out int q) ? q : 0;
                    logItem.Category = log[9];
                    logItem.Verb = log[10];
                    logItem.Path = log[11];
                }
                // ngsa log
                else if (log.Length == 13)
                {
                    // create a new log and add to list
                    logItem.StatusCode = int.TryParse(log[1], out int sc) ? sc : 200;
                    logItem.Duration = double.TryParse(log[2], out double d) ? d : 0;
                    logItem.Verb = log[3];
                    logItem.Path = log[4];
                    logItem.CorrelationVector = log[5];
                    logItem.Host = log[6];
                    logItem.ClientIP = log[7];
                    logItem.CosmosName = log[8];
                    logItem.CosmosQueryId = log[9];
                    logItem.UserAgent = log[10];
                    logItem.Region = log[11];
                    logItem.Zone = log[12];

                    SetNgsaLogValues(logItem);
                }
            }

            return logItem;
        }

        // get the logs using kubectl logs
        private static string GetLogs()
        {
            string logs = string.Empty;

            // have to use full name due to conflicts with system.commandline
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            // get the logs since last query
            // todo - look at using the k8s API instead
            p.StartInfo.FileName = "kubectl";
            p.StartInfo.Arguments = $"logs {Config.PodName} -c app --since {Config.Delay + 30}s";

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.EnableRaisingEvents = true;

            // redirect stdout
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += (sender, data) =>
            {
                // append each line from stdout
                logs += data.Data + "\n";
            };

            try
            {
                // run the task
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();

                // reset error count
                errorCount = 0;
            }
            // kubectl not installed
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("Fatal Error: kubectl not found");
                Environment.Exit(-1);
            }
            // ignore and try again
            catch (Exception ex)
            {
                errorCount++;

                // exit if too many errors in a row
                if (errorCount >= 10)
                {
                    Console.WriteLine($"Fatal Error: GetLogs {ex.Message}");
                    Environment.Exit(-1);
                }
                else
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            finally
            {
                p.Dispose();
            }

            // return the log text
            return logs;
        }

        // set ngsa values based on log type
        private static void SetNgsaLogValues(NgsaLog nl)
        {
            nl.PodType = string.IsNullOrEmpty(nl.CosmosName) ? "ngsa-memory" : "ngsa-cosmos";

            // ignore anything unknown
            nl.Category = "Ignore";
            nl.Quartile = 0;

            // TODO - use a configmap
            if (nl.Path.StartsWith("/api/actors/", StringComparison.OrdinalIgnoreCase))
            {
                nl.Category = "DirectRead";
                nl.Quartile = nl.Duration > 160 ? 4 : nl.Duration > 80 ? 3 : nl.Duration > 40 ? 2 : 1;
            }
            //else if (nl.Path.StartsWith("/api/actors?", StringComparison.OrdinalIgnoreCase))
            //{
            //    nl.Category = "Ignore";
            //    nl.Quartile = nl.Duration > 400 ? 4 : nl.Duration > 200 ? 3 : nl.Duration > 100 ? 2 : 1;
            //}
            //else if (nl.Path.StartsWith("/api/actors", StringComparison.OrdinalIgnoreCase))
            //{
            //    nl.Category = "Ignore";
            //    nl.Quartile = nl.Duration > 160 ? 4 : nl.Duration > 80 ? 3 : nl.Duration > 40 ? 2 : 1;
            //}
            //else if (nl.Path.StartsWith("/api/genres", StringComparison.OrdinalIgnoreCase))
            //{
            //    nl.Category = "Ignore";
            //    nl.Quartile = nl.Duration > 160 ? 4 : nl.Duration > 80 ? 3 : nl.Duration > 40 ? 2 : 1;
            //}
            else if (nl.Path.StartsWith("/api/movies/", StringComparison.OrdinalIgnoreCase))
            {
                nl.Category = "DirectRead";
                nl.Quartile = nl.Duration > 160 ? 4 : nl.Duration > 80 ? 3 : nl.Duration > 40 ? 2 : 1;
            }
            else if (nl.Path.StartsWith("/api/movies?", StringComparison.OrdinalIgnoreCase))
            {
                if (nl.Path.Contains("genre=", StringComparison.OrdinalIgnoreCase))
                {
                    nl.Category = nl.Path.Contains("pagesize=10", StringComparison.OrdinalIgnoreCase) ? "Genre10" : "Genre100";
                }
                else if (nl.Path.Contains("rating=", StringComparison.OrdinalIgnoreCase))
                {
                    nl.Category = nl.Path.Contains("pagesize=10", StringComparison.OrdinalIgnoreCase) ? "Rating10" : "Rating100";
                }
                else if (nl.Path.Contains("year=", StringComparison.OrdinalIgnoreCase))
                {
                    nl.Category = nl.Path.Contains("pagesize=10", StringComparison.OrdinalIgnoreCase) ? "Year10" : "Year100";
                }
            }
            //else if (nl.Path.StartsWith("/api/movies", StringComparison.OrdinalIgnoreCase))
            //{
            //    nl.Category = "Ignore";
            //    nl.Quartile = nl.Duration > 160 ? 4 : nl.Duration > 80 ? 3 : nl.Duration > 40 ? 2 : 1;
            //}
            //else if (nl.Path.StartsWith("/api/featured", StringComparison.OrdinalIgnoreCase))
            //{
            //    nl.Category = "Ignore";
            //    nl.Quartile = nl.Duration > 400 ? 4 : nl.Duration > 200 ? 3 : nl.Duration > 100 ? 2 : 1;
            //}
            //else if (nl.Path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            //{
            //    nl.Category = "Ignore";
            //    nl.Quartile = nl.Duration > 160 ? 4 : nl.Duration > 80 ? 3 : nl.Duration > 40 ? 2 : 1;
            //}
            //else if (nl.Path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase))
            //{
            //    nl.Category = "Ignore";
            //    nl.Quartile = nl.Duration > 1600 ? 4 : nl.Duration > 800 ? 3 : nl.Duration > 400 ? 2 : 1;
            //}
        }
    }
}
