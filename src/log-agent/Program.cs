// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace LogAgent
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "not localized")]
    /// <summary>
    /// Main application class
    /// entry point
    /// </summary>
    public sealed partial class App
    {
        // config variables
        private static readonly Config Config = new Config();

        // cancellation token
        private static readonly CancellationTokenSource ctCancel = new CancellationTokenSource();

        // current error count
        private static int errorCount;

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">command line</param>
        /// <returns></returns>
        private static async Task<int> Main(string[] args)
        {
            // use system.commandline to run the app
            return await RunApp(args).ConfigureAwait(false);
        }
    }
}
