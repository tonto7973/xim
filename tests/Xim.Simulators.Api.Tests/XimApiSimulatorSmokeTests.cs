using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xim.Simulators.Api.Tests
{
    [TestFixture]
    public class XimApiSimulatorSmokeTests
    {
        [Test]
        public async Task TestAbortSimpleHttpApiSimulator()
        {
            using (var simulation = Simulation.Create())
            {
                var someApi = simulation
                    .AddApi()
                    .SetDefaultHandler(async _ =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(6));
                        return ApiResponse.NotFound();
                    })
                    .Build();

                await someApi.StartAsync();
                try
                {
                    using (var client = new HttpClient())
                    {
                        var sendTask = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{someApi.Location}/books/8794"));
                        await Task.Delay(150);
                        var sw = new Stopwatch();
                        sw.Start();
                        someApi.Abort();
                        sw.Stop();
                        var exception = await sendTask.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);

                        someApi.ShouldSatisfyAllConditions(
                            () => exception.ShouldNotBeNull(),
                            () => sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2))
                        );
                    }
                }
                finally
                {
                    await someApi.StopAsync();
                }
            }
        }

        [Test]
        public async Task TestSimpleHttpApiSimulator()
        {
            using (var simulation = Simulation.Create())
            {
                var someApi = simulation.AddApi()
                    .AddHandler<int>("GET /books/{id}", id => ApiResponse.Ok(new { Title = $"Ya{id}" }))
                    .AddHandler<int>("PUT /books/{id}", _ => ApiResponse.BadRequest())
                    .Build();

                await someApi.StartAsync();

                try
                {
                    using (var client = new HttpClient())
                    {
                        var requests = new[] {
                            new HttpRequestMessage(HttpMethod.Get, $"{someApi.Location}/books/8794"),
                            new HttpRequestMessage(HttpMethod.Put, $"{someApi.Location}/books/8794")
                        };
                        requests[0].Headers.Add("x-ms-name", "antal, 32");
                        var responses = await Task.WhenAll(requests.Select(client.SendAsync));
                        var content = JObject.Parse(await responses[0].Content.ReadAsStringAsync());

                        responses.ShouldSatisfyAllConditions(
                            () => responses[0].StatusCode.ShouldBe(HttpStatusCode.OK),
                            () => content["title"].ShouldBe("Ya8794"),
                            () => responses[1].StatusCode.ShouldBe(HttpStatusCode.BadRequest)
                        );
                    }
                }
                finally
                {
                    await someApi.StopAsync();
                }
            }
        }

        [Test]
        public async Task TestAzureBlobStorageRestApiOverSsl()
        {
            var testCertificate = TestCertificate.Find();
            if (testCertificate == null)
            {
                Assert.Inconclusive("The test SSL certificate is not available.");
            }

            using (var simulation = Simulation.Create())
            {
                const string bookContents = "title: Hello world!";
                var sampleFileStream = new MemoryStream(Encoding.ASCII.GetBytes(bookContents));

                var azureBlobApi = simulation.AddApi()
                    .SetCertificate(testCertificate)
                    .AddHandler("HEAD /mystorage1/mycontainer1", ApiResponse.Ok())
                    .AddHandler("GET /mystorage1/mycontainer1/books.txt", new ApiResponse(500)) // 1st call - trigger retry policy
                    .AddHandler("GET /mystorage1/mycontainer1/books.txt", _ =>
                    {
                        var headers = Headers.FromString("x-ms-blob-type: BlockBlob");
                        var body = Body.FromStream(sampleFileStream);
                        return new ApiResponse(200, headers: headers, body: body);
                    })
                    .Build();

                await azureBlobApi.StartAsync();
                try
                {
                    var storageConnectionString = $"DefaultEndpointsProtocol=https;AccountName=mystorage1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=https://127.0.0.1:{azureBlobApi.Port}/mystorage1;";
                    var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                    var blobClient = storageAccount.CreateCloudBlobClient();
                    var container = blobClient.GetContainerReference("mycontainer1");
                    var containerExists = await container.ExistsAsync();

                    var ms = new MemoryStream();
                    var cloudBlockBlob = container.GetBlockBlobReference("books.txt");
                    await cloudBlockBlob.DownloadToStreamAsync(ms, null, null, null);
                    var cloudBlockContents = Encoding.ASCII.GetString(ms.ToArray());
                    var receivedApiCalls = azureBlobApi.ReceivedApiCalls.ToList();

                    cloudBlockContents.ShouldSatisfyAllConditions(
                        () => containerExists.ShouldBeTrue(),
                        () => cloudBlockContents.ShouldBe(bookContents),
                        () => receivedApiCalls[0].Action.ShouldStartWith("HEAD /mystorage1/mycontainer1"),
                        () => receivedApiCalls[1].Action.ShouldStartWith("GET /mystorage1/mycontainer1/books.txt"),
                        () => receivedApiCalls[1].Response.StatusCode.ShouldBe(500),
                        () => receivedApiCalls[2].Action.ShouldStartWith("GET /mystorage1/mycontainer1/books.txt"),
                        () => receivedApiCalls[2].Response.StatusCode.ShouldBe(200)
                    );
                }
                finally
                {
                    await azureBlobApi.StopAsync();
                }
            }
        }
    }
}
