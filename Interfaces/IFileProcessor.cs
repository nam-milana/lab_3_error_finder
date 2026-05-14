using ErrorFinder.Models;

namespace ErrorFinder.Interfaces;

/// <summary>
/// Интерфейс для логики обработки отдельного файла.
/// </summary>
public interface IFileProcessor
{
    /// <summary>
    /// Выполняет асинхронную обработку указанного файла.
    /// </summary>
    /// <param name="filePath">Путь к обрабатываемому файлу.</param>
    /// <param name="progress">Механизм для передачи данных о прогрессе в UI.</param>
    /// <param name="ct">Токен отмены.</param>
    Task ProcessAsync(string filePath, IProgress<ProgressInfo> progress, CancellationToken ct);
}
