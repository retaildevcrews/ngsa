// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Reflection;

namespace LogAgent
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
        // cache version info as it doesn't change
        private static string version = string.Empty;

        /// <summary>
        /// Gets the app version
        /// </summary>
        public static string Version
        {
            get
            {
                if (string.IsNullOrEmpty(version))
                {
                    if (Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof(AssemblyInformationalVersionAttribute)) is AssemblyInformationalVersionAttribute v)
                    {
                        version = v.InformationalVersion;
                    }
                }

                return version;
            }
        }
    }
}
