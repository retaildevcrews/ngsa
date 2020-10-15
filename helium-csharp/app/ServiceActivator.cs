﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Helium
{
    /// <summary>
    /// Add static service resolver to use when dependencies injection is not available
    /// </summary>
    public static class ServiceActivator
    {
        private static IServiceProvider serviceProvider;

        /// <summary>
        /// Configure ServiceActivator with full serviceProvider
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider</param>
        public static void Configure(IServiceProvider serviceProvider)
        {
            ServiceActivator.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Create a scope where use this ServiceActivator
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider</param>
        /// <returns>IServiceScope</returns>
        public static IServiceScope GetScope(IServiceProvider serviceProvider = null)
        {
            IServiceProvider provider = serviceProvider ?? ServiceActivator.serviceProvider;
            return provider?
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
        }
    }
}
