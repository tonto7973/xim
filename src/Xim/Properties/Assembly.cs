using System.Runtime.CompilerServices;

#if SIGNED

[assembly: InternalsVisibleTo("Xim.Simulators.Api, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100f92fe42d0a4d4a" +
    "0cc9e41744c7a8c761aa9946760030a06b0d9fd800a633e8bef7d201b1c4f745709fdf292f4147" +
    "e73adfe7b011f03e8a31023d9e07d79c319dcb38c158321a14b040b655de88bf6d60f4942d5ff3" +
    "84e70f607e2050a9ae2bf639fa26fd129f955f9b6523b3e662a4ce741f7762ab758507cadd2b0e" +
    "0d3dd3e0")]

[assembly: InternalsVisibleTo("Xim.Simulators.ServiceBus, PublicKey=" +
    "0024000004800000940000000602000000240000525341310004000001000100f92fe42d0a4d4a" +
    "0cc9e41744c7a8c761aa9946760030a06b0d9fd800a633e8bef7d201b1c4f745709fdf292f4147" +
    "e73adfe7b011f03e8a31023d9e07d79c319dcb38c158321a14b040b655de88bf6d60f4942d5ff3" +
    "84e70f607e2050a9ae2bf639fa26fd129f955f9b6523b3e662a4ce741f7762ab758507cadd2b0e" +
    "0d3dd3e0")]

#else

[assembly: InternalsVisibleTo("Xim.Simulators.Api")]
[assembly: InternalsVisibleTo("Xim.Simulators.ServiceBus")]
[assembly: InternalsVisibleTo("Xim.Tests")]
[assembly: InternalsVisibleTo("Xim.Simulators.Api.Tests")]
[assembly: InternalsVisibleTo("Xim.Simulators.ServiceBus.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

#endif