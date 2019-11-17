
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Usability", Scope = "type", Target = "~T:Xim.Simulators.Api.Headers")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "Usability", Scope = "type", Target = "~T:Xim.Simulators.Api.Headers")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Usability", Scope = "type", Target = "~T:Xim.Simulators.Api.Routing.Error")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "False positive", Scope = "member", Target = "~M:Xim.Simulators.Api.Body.FromString(System.String,System.Text.Encoding,System.String)~Xim.Simulators.Api.Body{System.IO.Stream}")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception handled", Scope = "member", Target = "~M:Xim.Simulators.Api.Routing.Route`1.TryBind(System.Type,System.String,System.Object@)~System.Boolean")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0067:Dispose objects before losing scope", Justification = "False positive", Scope = "member", Target = "~M:Xim.Simulators.Api.ApiSimulatorOwinMiddleware.InvokeAsync(Microsoft.AspNetCore.Http.HttpContext,Microsoft.AspNetCore.Http.RequestDelegate)~System.Threading.Tasks.Task")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by simulation", Scope = "member", Target = "~M:Xim.Simulators.Api.ApiBuilder.Build~Xim.Simulators.Api.IApiSimulator")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive", Scope = "member", Target = "~M:Xim.Simulators.Api.ApiSimulatorOwinMiddleware.InvokeAsync(Microsoft.AspNetCore.Http.HttpContext,Microsoft.AspNetCore.Http.RequestDelegate)~System.Threading.Tasks.Task")]