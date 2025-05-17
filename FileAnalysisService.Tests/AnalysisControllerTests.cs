using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileAnalysisService.Controllers;
using FileAnalysisService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Models;
using Xunit;

namespace FileAnalysisService.Tests
{
    public class AnalysisControllerTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IHttpClientFactory> _httpFactory;
        private readonly Guid _fileId;
        private readonly byte[] _fileBytes;
        private readonly byte[] _cloudImage;
        private readonly Guid _cloudId;
        private readonly AnalysisController _controller;

        public AnalysisControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _fileId = Guid.NewGuid();
            var text = "Para1\n\nPara2";
            _fileBytes = Encoding.UTF8.GetBytes(text);

            _cloudImage = new byte[] { 1, 2, 3 };
            _cloudId = Guid.NewGuid();

            // Setup fake HttpClients
            var storageHandler = new FakeHandler(request =>
            {
                if (request.Method == HttpMethod.Get)
                {
                    // differentiate file vs cloud by id in path
                    var idInPath = request.RequestUri?.AbsolutePath.Split('/').Last();
                    if (idInPath == _cloudId.ToString())
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new ByteArrayContent(_cloudImage)
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
                            }
                        };
                    }
                    // return file bytes for initial file fetch
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(_fileBytes)
                        {
                            Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") }
                        }
                    };
                }
                // POST upload cloud image
                var json = JsonContent.Create(new { id = _cloudId }).ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });
            var cloudHandler = new FakeHandler(request =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(_cloudImage)
                    {
                        Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
                    }
                }
            );
            var storageClient = new HttpClient(storageHandler)
            {
                BaseAddress = new Uri("http://storage")
            };
            var wordCloudClient = new HttpClient(cloudHandler)
            {
                BaseAddress = new Uri("http://wordcloud")
            };

            _httpFactory = new Mock<IHttpClientFactory>();
            _httpFactory.Setup(f => f.CreateClient("FileStorage")).Returns(storageClient);
            _httpFactory.Setup(f => f.CreateClient("WordCloud")).Returns(wordCloudClient);

            _controller = new AnalysisController(_dbContext, _httpFactory.Object);
        }

        [Fact]
        public async Task Analyze_HappyPath_ReturnsCorrectResultAndSaves()
        {
            // Act
            var result = await _controller.Analyze(_fileId) as OkObjectResult;
            Assert.NotNull(result);
            var analysis = Assert.IsType<AnalysisResult>(result.Value);

            // Text "Para1\n\nPara2" has 2 paragraphs, 2 words (Para1 and Para2), 11 chars
            Assert.Equal(2, analysis.ParagraphCount);
            Assert.Equal(2, analysis.WordCount);
            Assert.Equal(_fileBytes.Length, analysis.CharacterCount);
            Assert.Equal(_cloudId.ToString(), analysis.WordCloudLocation);

            // Verify saved in DB
            var saved = _dbContext.AnalysisResults.FirstOrDefault(r => r.FileId == _fileId);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task Analyze_ExistingResult_ReturnsExisting()
        {
            // Arrange
            var existing = new AnalysisResult { Id = Guid.NewGuid(), FileId = _fileId, ParagraphCount = 1, WordCount = 1, CharacterCount = 1, WordCloudLocation = "x", AnalyzedAt = DateTime.UtcNow };
            _dbContext.AnalysisResults.Add(existing);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Analyze(_fileId) as OkObjectResult;
            Assert.NotNull(result);
            var returned = Assert.IsType<AnalysisResult>(result.Value);
            Assert.Equal(existing.Id, returned.Id);
        }

        [Fact]
        public async Task GetAnalysis_NotFound_ReturnsNotFound()
        {
            var result = await _controller.GetAnalysis(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetAnalysis_Found_ReturnsOk()
        {
            var existing = new AnalysisResult { Id = Guid.NewGuid(), FileId = _fileId, ParagraphCount = 0, WordCount = 0, CharacterCount = 0, WordCloudLocation = "x", AnalyzedAt = DateTime.UtcNow };
            _dbContext.AnalysisResults.Add(existing);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetAnalysis(_fileId) as OkObjectResult;
            Assert.NotNull(result);
            var returned = Assert.IsType<AnalysisResult>(result.Value);
            Assert.Equal(existing.Id, returned.Id);
        }

        [Fact]
        public async Task GetCloud_HappyPath_ReturnsImage()
        {
            var result = await _controller.GetCloud(_cloudId.ToString()) as FileContentResult;
            Assert.NotNull(result);
            Assert.Equal(_cloudImage, result.FileContents);
            Assert.Equal("image/png", result.ContentType);
        }

        [Fact]
        public async Task GetCloud_NotFound_ReturnsStatusCode()
        {
            // Setup factory to return failure
            var badHandler = new FakeHandler(request => new HttpResponseMessage(HttpStatusCode.NotFound));
            var badClient = new HttpClient(badHandler) { BaseAddress = new Uri("http://storage") };
            _httpFactory.Setup(f => f.CreateClient("FileStorage")).Returns(badClient);

            var result = await _controller.GetCloud("whatever");
            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.NotFound, status.StatusCode);
        }

        // Helper handler
        private class FakeHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
            public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_responder(request));
        }
    }
}
