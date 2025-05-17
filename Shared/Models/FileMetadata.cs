using System;

namespace Shared.Models
{
    /// <summary>
    /// Содержит информацию о загруженном файле.
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Уникальный идентификатор файла.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Оригинальное имя файла.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// SHA-256 хеш содержимого файла.
        /// </summary>
        public required string Hash { get; set; }

        /// <summary>
        /// Локация файла в хранилище (путь или ключ).
        /// </summary>
        public required string Location { get; set; }

        /// <summary>
        /// Время загрузки файла.
        /// </summary>
        public DateTime UploadedAt { get; set; }
    }
}
