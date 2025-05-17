using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FileStorageService.Data;
using FileStorageService.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Xunit;

namespace FileStorageService.Tests
{
    public class FilesControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly FilesController _controller;
        private readonly string _storePath;

        public FilesControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);
            _controller = new FilesController(_dbContext);
            _storePath = Path.Combine(Directory.GetCurrentDirectory(), "StoredFiles");
            if (!Directory.Exists(_storePath))
            {
                Directory.CreateDirectory(_storePath);
            }
        }

        public void Dispose()
        {
            // cleanup files
            if (Directory.Exists(_storePath))
            {
                Directory.Delete(_storePath, true);
            }
            _dbContext?.Dispose();
        }

        [Fact]
        public async Task Upload_NullFile_ReturnsBadRequest()
        {
            var result = await _controller.Upload(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_EmptyFile_ReturnsBadRequest()
        {
            var emptyFile = new FormFile(Stream.Null, 0, 0, null, "empty.txt");
            var result = await _controller.Upload(emptyFile);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Upload_NewFile_SavesMetadataAndReturnsId()
        {
            var content = "Hello world";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var file = new FormFile(stream, 0, stream.Length, "file", "test.txt");

            var result = await _controller.Upload(file) as OkObjectResult;
            Assert.NotNull(result);
            var idProperty = result.Value!.GetType().GetProperty("id");
            Assert.NotNull(idProperty);
            var id = (Guid)idProperty.GetValue(result.Value)!;
            // verify metadata
            var saved = _dbContext.Files.FirstOrDefault(f => f.Id == id);
            Assert.NotNull(saved);
            Assert.Equal("test.txt", saved.Name);
            // verify file saved
            var savedFilePath = Path.Combine(_storePath, saved.Location);
            Assert.True(File.Exists(savedFilePath));
        }

        [Fact]
        public async Task Upload_DuplicateFile_ReturnsExistingId()
        {
            var content = "Duplicate content";
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream1 = new MemoryStream(bytes);
            var file1 = new FormFile(stream1, 0, bytes.Length, "file", "dup.txt");

            var result1 = await _controller.Upload(file1) as OkObjectResult;
            Assert.NotNull(result1);
            var idProperty1 = result1.Value!.GetType().GetProperty("id");
            Assert.NotNull(idProperty1);
            var id1 = (Guid)idProperty1.GetValue(result1.Value)!;

            // second upload with same content but different original name
            var stream2 = new MemoryStream(bytes);
            var file2 = new FormFile(stream2, 0, bytes.Length, "file", "dup2.txt");

            var result2 = await _controller.Upload(file2) as OkObjectResult;
            Assert.NotNull(result2);
            var idProperty2 = result2.Value!.GetType().GetProperty("id");
            Assert.NotNull(idProperty2);
            var id2 = (Guid)idProperty2.GetValue(result2.Value)!;

            Assert.Equal(id1, id2);
            // убедиться, что запись метаданных не дублируется
            Assert.Equal(1, _dbContext.Files.Count(f => f.Id == id1));
        }

        [Fact]
        public async Task Download_NotFoundId_ReturnsNotFound()
        {
            var result = await _controller.Download(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Download_FileMissing_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _dbContext.Files.Add(new FileMetadata { Id = id, Name = "x.txt", Hash = "h", Location = "missing.txt", UploadedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            var result = await _controller.Download(id);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Download_ExistingFile_ReturnsFile()
        {
            var id = Guid.NewGuid();
            var fileName = id + ".txt";
            var fullPath = Path.Combine(_storePath, fileName);
            var content = "Sample content";
            await File.WriteAllTextAsync(fullPath, content);
            _dbContext.Files.Add(new FileMetadata { Id = id, Name = "sample.txt", Hash = "h", Location = fileName, UploadedAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            var result = await _controller.Download(id) as FileContentResult;
            Assert.NotNull(result);
            var returned = System.Text.Encoding.UTF8.GetString(result.FileContents);
            Assert.Equal(content, returned);
            Assert.Equal("sample.txt", result.FileDownloadName);
        }
    }
}
