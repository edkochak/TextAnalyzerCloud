using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FileStorageService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileStorageService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly string _storagePath;

        public FilesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            // Папка для хранения файлов
            _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "StoredFiles");
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        /// <summary>
        /// Загружает файл, сохраняет метаданные и возвращает идентификатор файла.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] Microsoft.AspNetCore.Http.IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            // Вычисляем SHA256 хеш
            string hash;
            using (var sha = SHA256.Create())
            using (var stream = file.OpenReadStream())
            {
                var bytes = await sha.ComputeHashAsync(stream);
                hash = BitConverter.ToString(bytes).Replace("-", string.Empty);
            }

            // Проверяем, есть ли уже файл с таким хешем
            var existing = await _dbContext.Files.FirstOrDefaultAsync(f => f.Hash == hash);
            if (existing != null)
            {
                return Ok(new { id = existing.Id });
            }

            // Сохраняем файл на диск
            var id = Guid.NewGuid();
            var fileName = id + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(_storagePath, fileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            // Сохраняем метаданные
            var metadata = new FileMetadata
            {
                Id = id,
                Name = file.FileName,
                Hash = hash,
                Location = fileName,
                UploadedAt = DateTime.UtcNow
            };
            _dbContext.Files.Add(metadata);
            await _dbContext.SaveChangesAsync();

            return Ok(new { id });
        }

        /// <summary>
        /// Получает файл по идентификатору.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var meta = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (meta == null)
                return NotFound();

            var filePath = Path.Combine(_storagePath, meta.Location);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var content = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(content, "application/octet-stream", meta.Name);
        }
    }
}
