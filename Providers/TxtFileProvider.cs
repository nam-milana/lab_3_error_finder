using System.IO;
using ErrorFinder.Interfaces;

namespace ErrorFinder.Providers;

/// <summary>
/// Провайдер для получения списка .txt файлов из указанной папки и её поддиректорий.
/// </summary>
public class TxtFileProvider : IFileListProvider
{
    /// <summary>
    /// Асинхронно получает список всех .txt файлов в указанной папке и её поддиректориях.
    /// </summary>
    /// <param name="rootPath"> Корневая папка для поиска. </param>
    /// <param name="ct"> Токен отмены операции. </param>
    /// <returns> Список путей к .txt файлам. </returns>
    public Task<List<string>> GetFilesAsync(string rootPath, CancellationToken ct)
    {
        return Task.Run(
            () => Directory.GetFiles(rootPath, "*.txt", SearchOption.AllDirectories).ToList(),
            ct
        );
    }
}
