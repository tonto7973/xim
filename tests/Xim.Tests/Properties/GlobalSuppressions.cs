
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1090:Call 'ConfigureAwait(false)'.", Justification = "Not required in unit tests")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RCS1210:Return Task.FromResult instead of returning null.", Justification = "Not important in unit tests")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0039:Use local function", Justification = "Would hide intended code verbosity")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "Memory leaks in unit tests not relevant")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "False positive - NSubstitute used")]