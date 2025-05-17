using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FileAnalysisService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileAnalysisService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public AnalysisController(ApplicationDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("{fileId}")]
        public async Task<IActionResult> Analyze(Guid fileId)
        {
            // Check existing result
            var existing = await _dbContext.AnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);
            if (existing != null)
                return Ok(existing);

            // Download file from FileStorageService
            var storageClient = _httpClientFactory.CreateClient("FileStorage");
            // Получение файла по id из FileStorageService
            var fileResponse = await storageClient.GetAsync($"/api/files/{fileId}");
            if (!fileResponse.IsSuccessStatusCode)
                return StatusCode((int)fileResponse.StatusCode, "Не удалось получить файл");

            var bytes = await fileResponse.Content.ReadAsByteArrayAsync();
            var text = Encoding.UTF8.GetString(bytes);

            // Analyze text
            var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var words = text.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            var chars = text.Length;

            // Generate word cloud image
            var cloudClient = _httpClientFactory.CreateClient("WordCloud");
            var payload = new { text = string.Join(' ', words) };
            HttpResponseMessage cloudResponse;
            try
            {
                cloudResponse = await cloudClient.PostAsJsonAsync("", payload);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при обращении к WordCloud API", exception = ex.Message });
            }
            if (!cloudResponse.IsSuccessStatusCode)
            {
                var errorBody = await cloudResponse.Content.ReadAsStringAsync();
                return StatusCode((int)cloudResponse.StatusCode, new { message = "Ошибка генерации облака слов", status = cloudResponse.StatusCode, details = errorBody });
            }
            var imageBytes = await cloudResponse.Content.ReadAsByteArrayAsync();

            // Upload image to FileStorageService
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(imageBytes), "file", $"cloud_{fileId}.png");
            HttpResponseMessage uploadResponse;
            try
            {
                // Сохранение изображения облака слов
                uploadResponse = await storageClient.PostAsync("/api/files", form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при сохранении изображения в FileStorageService", exception = ex.Message });
            }
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var uploadError = await uploadResponse.Content.ReadAsStringAsync();
                return StatusCode((int)uploadResponse.StatusCode, new { message = "Не удалось сохранить изображение", status = uploadResponse.StatusCode, details = uploadError });
            }
            var resultObj = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
            var cloudId = resultObj.GetProperty("id").GetGuid();

            // Save analysis result
            var analysis = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                ParagraphCount = paragraphs.Length,
                WordCount = words.Length,
                CharacterCount = chars,
                WordCloudLocation = cloudId.ToString(),
                AnalyzedAt = DateTime.UtcNow
            };
            _dbContext.AnalysisResults.Add(analysis);
            await _dbContext.SaveChangesAsync();

            return Ok(analysis);
        }

        [HttpGet("{fileId}")]
        public async Task<IActionResult> GetAnalysis(Guid fileId)
        {
            var analysis = await _dbContext.AnalysisResults.FirstOrDefaultAsync(r => r.FileId == fileId);
            if (analysis == null)
                return NotFound();
            return Ok(analysis);
        }

        [HttpGet("cloud/{cloudId}")]
        public async Task<IActionResult> GetCloud(string cloudId)
        {
            var storageClient = _httpClientFactory.CreateClient("FileStorage");
            // Получение картинки облака слов
            var response = await storageClient.GetAsync($"/api/files/{cloudId}");
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Не удалось получить облако слов");
            var data = await response.Content.ReadAsByteArrayAsync();
            return File(data, "image/png");
        }
    }
}
