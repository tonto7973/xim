using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
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
            using ISimulation simulation = Simulation.Create();
            IApiSimulator someApi = simulation
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
                using var client = new HttpClient();
                Task<HttpResponseMessage> sendTask = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{someApi.Location}/books/8794"));
                await Task.Delay(150);
                var sw = new Stopwatch();
                sw.Start();
                someApi.Abort();
                sw.Stop();
                AggregateException exception = await sendTask.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);

                someApi.ShouldSatisfyAllConditions(
                    () => exception.ShouldNotBeNull(),
                    () => sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2))
                );
            }
            finally
            {
                await someApi.StopAsync();
            }
        }

        [Test]
        public async Task TestSimpleHttpApiSimulator()
        {
            using ISimulation simulation = Simulation.Create();
            IApiSimulator someApi = simulation.AddApi()
                .AddHandler<int>("GET /books/{id}", id => ApiResponse.Ok(new { Title = $"Ya{id}" }))
                .AddHandler<int>("PUT /books/{id}", _ => ApiResponse.BadRequest())
                .Build();

            await someApi.StartAsync();

            try
            {
                using var client = new HttpClient();
                HttpRequestMessage[] requests = new[] {
                            new HttpRequestMessage(HttpMethod.Get, $"{someApi.Location}/books/8794"),
                            new HttpRequestMessage(HttpMethod.Put, $"{someApi.Location}/books/8794")
                            {
                                Content = new StringContent(
                                    "{\"title\":\"Book 8794\",\"author\":\"Joe Black\"}",
                                    Encoding.UTF8,
                                    "application/json")
                            }
                        };
                requests[0].Headers.Add("x-ms-name", "antal, 32");
                HttpResponseMessage[] responses = await Task.WhenAll(requests.Select(client.SendAsync));
                var content = JObject.Parse(await responses[0].Content.ReadAsStringAsync());

                responses.ShouldSatisfyAllConditions(
                    () => responses[0].StatusCode.ShouldBe(HttpStatusCode.OK),
                    () => content["title"].ShouldBe("Ya8794"),
                    () => responses[1].StatusCode.ShouldBe(HttpStatusCode.BadRequest)
                );
            }
            finally
            {
                await someApi.StopAsync();
            }

            someApi.ReceivedApiCalls.Count.ShouldBe(2);
            someApi.ReceivedApiCalls.ShouldContain(apiCall => apiCall.Request.Body.ReadAsString() == "{\"title\":\"Book 8794\",\"author\":\"Joe Black\"}");
        }

        [TestCase("application/xml")]
        [TestCase("text/xml")]
        public async Task TestSimpleHttpApiSimulatorWithXmlSupport(string mediaType)
        {
            using ISimulation simulation = Simulation.Create();
            IApiSimulator someApi = simulation.AddApi()
                .AddHandler<int>("GET /books/{id}", id => ApiResponse.Ok(new Book { Title = $"Ya{id}", Id = id }))
                .SetXmlSettings(new XmlWriterSettings {
                    Indent = false,
                    Encoding = Encoding.UTF8
                })
                .Build();

            await someApi.StartAsync();
            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{someApi.Location}/books/2234");
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));

                HttpResponseMessage response = await client.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();

                response.ShouldSatisfyAllConditions(
                    () => content.ShouldBe("<?xml version=\"1.0\" encoding=\"utf-8\"?><Book><Id>2234</Id><Title>Ya2234</Title></Book>"),
                    () => response.StatusCode.ShouldBe(HttpStatusCode.OK),
                    () => response.Content.Headers.ContentType.MediaType.ShouldBe(mediaType)
                );
            }
            finally
            {
                await someApi.StopAsync();
            }
        }

        [Test]
        public async Task TestAzureBlobStorageRestApiOverSsl()
        {
            X509Certificate2 testCertificate = TestCertificate.Find();
            if (testCertificate == null)
            {
                Assert.Inconclusive("The test SSL certificate is not available.");
            }

            using ISimulation simulation = Simulation.Create();
            const string bookContents = "title: Hello world!";
            var sampleFileStream = new MemoryStream(Encoding.ASCII.GetBytes(bookContents));

            IApiSimulator azureBlobApi = simulation.AddApi()
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

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("mycontainer1");
                var containerExists = await container.ExistsAsync();

                var ms = new MemoryStream();
                CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference("books.txt");
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

    public class Book
    {
        public int Id { get; set; }

        public string Title { get; set; }
    }
}
