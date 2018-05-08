// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1652:EnableXmlDocumentationOutput", Justification = "Reviewed.")]

[assembly: SuppressMessage("FxCop.Reliability", "CA2007:Do not directly await a Task", Justification = "Reviewed.")]
[assembly: SuppressMessage("FxCop.Naming", "CA1707:Identifiers should not contain underscores", Justification = "Reviewed.")]
[assembly: SuppressMessage("FxCop.Performance", "CA1822:Mark members as static", Justification = "Reviewed.")]

// TODO: fix this issues and remove this suppression
[assembly: SuppressMessage("FxCop.Globalization", "CA1305:Specify IFormatProvider", Justification = "Temporary.")]
[assembly: SuppressMessage("FxCop.Globalization", "CA1304:Specify CultureInfo", Justification = "Temporary.")]