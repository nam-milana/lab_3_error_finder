namespace ErrorFinder.Interfaces;

/// <summary>
/// Интерфейс провайдера для получения списка файлов для обработки.
/// </summary>
public interface IFileListProvider
{
    /// <summary>
    /// Асинхронно получает список путей к файлам.
    /// </summary>
    /// <param name="rootPath">Корневой путь для поиска.</param>
    /// <param name="ct">Токен для отмены операции.</param>
    /// <returns>Список строк, содержащий полные пути к файлам.</returns>
    Task<List<string>> GetFilesAsync(string rootPath, CancellationToken ct);
}
