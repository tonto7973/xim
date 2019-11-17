# Quick Start (Http Api)

### 1. Add required namespaces:

```csharp
using Xim;
using Xim.Simulators.Api;
```

### 2. Initialise simulation:

```csharp
using var simulation = Simulation.Create();
```

### 3. Create Http Api simulator:

```csharp
var apiSimulator = simulation
    .AddApi()
    .AddHandler("HEAD /devaccount1/books", ApiResponse.Ok())
    .AddHandler("GET /devaccount1/books/sample.json", _ => {
        var headers = Headers.FromString("x-ms-blob-type: BlockBlob");
        var body = Body.FromStream(new MemoryStream(byteArray));
        return ApiResponse.Ok(headers, body);
    })
    .Build();
```

### 4. Run the simulator and test your code:

```csharp
await apiSimulator.StartAsync();
try
{
    // run / test your code or application here
}
finally
{
    await apiSimulator.StopAsync();
}
```
