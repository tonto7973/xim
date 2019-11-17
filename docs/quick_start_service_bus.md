# Quick Start (Service Bus)

**Before you start**  
If you intend to use Xim Service Bus Simulator with [Microsoft.Azure.ServiceBus](https://www.nuget.org/packages/Microsoft.Azure.ServiceBus), you will need to install a SSL localhost development certificate to your CA root store and user store. You can use the .pfx certificate / tool located [here](../tools/Xim.Tests.Setup/).

### 1. Add required namespaces:

```csharp
using Xim;
using Xim.Simulators.ServiceBus;
```

### 2. Initialise simulation:

```csharp
var simulation = Simulation.Create();
```

### 3. Create Service Bus simulator:

```csharp
var busSimulator = simulation
    .AddServiceBus()
    .SetCertificate(localhostCertificate)
    .AddQueue("sb-sample-queue")
    .Build();
```

### 4. Run the simulator and test your code:

```csharp
await busSimulator.StartAsync();
try
{
    // run / test your code or application here
}
finally
{
    await busSimulator.StopAsync();
}
```