// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;

namespace CSE.NextGenSymmetricApp.Extensions
{
    public static class CommandLineExtensions
    {
        // add from environment variables
        public static void AddFromEnvironment(this List<string> cmd, string key, string shortKey = "")
        {
            // command line takes precedence
            if (!cmd.Contains(key) && (string.IsNullOrEmpty(shortKey) || !cmd.Contains(shortKey)))
            {
                // convert key to ENV_KEY_FORMAT
                string envKey = key.ToUpperInvariant().Replace("--", string.Empty).Replace('-', '_');

                // get the value
                string value = Environment.GetEnvironmentVariable(envKey);

                // add the value (validation is handled by system.commandline)
                if (!string.IsNullOrEmpty(value))
                {
                    cmd.Add(key);
                    cmd.Add(value);
                }
            }
        }
    }
}
