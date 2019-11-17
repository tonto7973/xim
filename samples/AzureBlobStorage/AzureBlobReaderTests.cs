using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AzureBlobStorageSample.Azure;
using NUnit.Framework;
using Xim;
using Xim.Simulators.Api;

namespace AzureBlobStorageSample.Tests
{
    public class AzureBlobReaderTests
    {
        private ISimulation _simulation;

        [SetUp]
        public void SetUp()
        {
            _simulation = Simulation.Create();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _simulation.StopAllAsync();
            _simulation.Dispose();
        }

        [Test]
        public async Task ReadFileAsync_Throws_WhenBlobContainerDoesNotExist()
        {
            var azureBlobApi = _simulation
                .AddApi()
                .AddHandler("HEAD /devaccount1/magazines", ApiResponse.NotFound())
                .AddHandler("GET /authors/{id}", ctx => {
                    return ApiResponse.Ok();
                })
                .AddHandler<int>("GET /authors/{id}", id => {
                    return ApiResponse.Ok();
                })
                .AddHandler<(int Id, bool Disabled)>("PUT /authors/{id}/{disable}", dto => {
                    return ApiResponse.Ok();
                })
                .Build();
            await azureBlobApi.StartAsync();
            var testSettings = new AzureBlobSettings
            {
                ConnectionString = $"DefaultEndpointsProtocol=http;AccountName=devaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{azureBlobApi.Port}/devaccount1;",
                ContainerName = "magazines"
            };
            var blobReader = new AzureBlobReader(testSettings);

            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await blobReader.ReadFileAsync("abc.json"));
            Assert.AreEqual("Container 'magazines' does not exist.", exception.Message);
        }

        [Test]
        public async Task ReadFileAsync_ReadsFile_WhenBlobExists()
        {
            var testData = Encoding.ASCII.GetBytes("{\"id\":325}");
            var azureBlobApi = _simulation
                .AddApi()
                .AddHandler("HEAD /devaccount1/books", ApiResponse.Ok())
                // see https://docs.microsoft.com/en-us/rest/api/storageservices/get-blob
                .AddHandler("GET /devaccount1/books/sample.json", _ => {
                    var headers = Headers.FromString("x-ms-blob-type: BlockBlob");
                    var body = Body.FromStream(new MemoryStream(testData));
                    return ApiResponse.Ok(headers, body);
                })
                .Build();
            await azureBlobApi.StartAsync();
            var testSettings = new AzureBlobSettings
            {
                ConnectionString = $"DefaultEndpointsProtocol=http;AccountName=devaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{azureBlobApi.Port}/devaccount1;",
                ContainerName = "books"
            };
            var blobReader = new AzureBlobReader(testSettings);

            var blobFile = await blobReader.ReadFileAsync("sample.json");

            Assert.AreEqual(testData, blobFile.ToArray());
        }

        [Test]
        public async Task ReadFilePartAsync_ReadsFilePart_WhenBlobExists()
        {
            const string testString = " {\"id\":325}";
            const int offset = 2;
            const int length = 4;
            var testData = Encoding.ASCII.GetBytes(testString);
            var expectedData = new ReadOnlySpan<byte>(testData).Slice(offset, length).ToArray();
            var azureBlobApi = _simulation
                .AddApi()
                .AddHandler("HEAD /devaccount1/books", ApiResponse.Ok())
                // see https://docs.microsoft.com/en-us/rest/api/storageservices/get-blob
                // and https://docs.microsoft.com/en-us/rest/api/storageservices/specifying-the-range-header-for-blob-service-operations
                .AddHandler("GET /devaccount1/books/sample.json", ctx => {
                    var headers = Headers.FromString("x-ms-blob-type: BlockBlob");
                    var range = (string)ctx.Request.Headers["x-ms-range"]
                                     ?? ctx.Request.Headers["Range"];
                    var data = testData;
                    if (!string.IsNullOrEmpty(range))
                    {
                        var bytes = range.Split('=')[1].Split('-');
                        var start = int.Parse(bytes[0]);
                        if (!int.TryParse(bytes[1], out var end))
                            end = testData.Length - 1;
                        var count = end - start + 1;
                        data = new ReadOnlySpan<byte>(testData).Slice(start, count).ToArray();
                        headers["Content-Range"] = $"bytes {start}-{end}/{count}";
                        var body = Body.FromStream(new MemoryStream(data));
                        return new ApiResponse(206, "Partial Content", headers: headers, body: body);
                    }
                    return ApiResponse.Ok(headers, Body.FromStream(new MemoryStream(testData)));
                })
                .Build();
            await azureBlobApi.StartAsync();
            var testSettings = new AzureBlobSettings
            {
                ConnectionString = $"DefaultEndpointsProtocol=http;AccountName=devaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:{azureBlobApi.Port}/devaccount1;",
                ContainerName = "books"
            };
            var blobReader = new AzureBlobReader(testSettings);

            var blobData = await blobReader.ReadFilePartAsync("sample.json", offset, length);

            Assert.AreEqual(expectedData, blobData);
        }
    }
}