using System;

namespace Shared.Models
{
    /// <summary>
    /// Результат анализа файла.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Идентификатор анализа.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор связанного файла.
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// Количество абзацев.
        /// </summary>
        public int ParagraphCount { get; set; }

        /// <summary>
        /// Количество слов.
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// Количество символов.
        /// </summary>
        public int CharacterCount { get; set; }

        /// <summary>
        /// Локация картинки облака слов.
        /// </summary>
        public string WordCloudLocation { get; set; }

        /// <summary>
        /// Дата выполнения анализа.
        /// </summary>
        public DateTime AnalyzedAt { get; set; }
    }
}
