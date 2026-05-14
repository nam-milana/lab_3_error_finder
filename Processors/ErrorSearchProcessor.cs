using System.IO;
using ErrorFinder.Interfaces;
using ErrorFinder.Models;

namespace ErrorFinder.Processors;

/// <summary>
/// Реализация процессора, который ищет в текстовом файле ключевое слово "error".
/// </summary>
public class ErrorSearchProcessor : IFileProcessor
{
    private const string SearchWord = "error";
    private static readonly StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Выполняет построчное сканирование файла.
    /// </summary>
    public async Task ProcessAsync(
        string filePath,
        IProgress<ProgressInfo> progress,
        CancellationToken ct
    )
    {
        bool found = false;

        // Используем StreamReader для построчного чтения, чтобы не загружать огромные файлы целиком в память
        using (var reader = new StreamReader(filePath))
        {
            while (true)
            {
                string? line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                    break; // конец файла

                if (line.Contains(SearchWord, Comparison))
                {
                    found = true;
                    break;
                }

                // Проверка на отмену после каждой прочитанной строки
                ct.ThrowIfCancellationRequested();
            }
        }

        if (found)
        {
            progress.Report(
                new ProgressInfo
                {
                    CurrentFile = filePath,
                    ErrorFound = true,
                    Status = ProcessingStatus.ErrorFound,
                }
            );
        }
    }
}
