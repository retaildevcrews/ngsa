// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "need to catch all exceptions")]
[assembly: SuppressMessage("Design", "CA1303:Do not pass literals as localized parameters", Justification = "method name for logging")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "search string has to be lower case", Scope = "namespaceanddescendants", Target = "Ngsa.App.DataAccessLayer")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "IDispose", Scope = "type", Target = "~T:Ngsa.App.App")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "IDispose", Scope = "namespaceanddescendants", Target = "Ngsa.App.DataAccessLayer")]
[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "needed for json deserialization", Scope = "namespaceanddescendants", Target = "Ngsa.App.Model")]
