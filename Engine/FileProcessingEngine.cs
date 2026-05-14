using System.Collections.Concurrent;
using ErrorFinder.Interfaces;
using ErrorFinder.Models;

namespace ErrorFinder.Engine;

/// <summary>
/// Многопоточный движок для параллельной обработки файлов.
/// </summary>
/// <param name="workerCount">Количество параллельных потоков. Если 0, используется количество ядер процессора.</param>
public class FileProcessingEngine(int workerCount = 0)
{
    private readonly int _workerCount = workerCount > 0 ? workerCount : Environment.ProcessorCount;

    /// <summary>
    /// Запускает асинхронный процесс обработки файлов.
    /// </summary>
    /// <param name="fileProvider">Провайдер для получения списка путей к файлам.</param>
    /// <param name="rootPath">Корневая папка для поиска.</param>
    /// <param name="processor">Логика обработки конкретного файла.</param>
    /// <param name="progress">Объект для уведомления UI о прогрессе.</param>
    /// <param name="cancellationToken">Токен для прерывания операции.</param>
    public async Task RunAsync(
        IFileListProvider fileProvider,
        string rootPath,
        IFileProcessor processor,
        IProgress<ProgressInfo> progress,
        CancellationToken cancellationToken
    )
    {
        // Получаем список файлов асинхронно, не блокируя вызывающий поток
        List<string> allFiles = await fileProvider
            .GetFilesAsync(rootPath, cancellationToken)
            .ConfigureAwait(false);

        int total = allFiles.Count;
        // Используем потокобезопасную очередь для распределения файлов между воркерами
        var queue = new ConcurrentQueue<string>(allFiles);
        int processed = 0;

        var workers = new Task[_workerCount];
        for (int i = 0; i < _workerCount; i++)
        {
            workers[i] = Task.Run(
                async () =>
                {
                    while (
                        !cancellationToken.IsCancellationRequested
                        && queue.TryDequeue(out string? currentFile)
                    )
                    {
                        // Сообщаем UI, что файл взят в работу
                        progress.Report(
                            new ProgressInfo
                            {
                                CurrentFile = currentFile,
                                ProcessedCount = processed,
                                TotalCount = total,
                                Status = ProcessingStatus.Processing,
                            }
                        );

                        try
                        {
                            await processor
                                .ProcessAsync(currentFile, progress, cancellationToken)
                                .ConfigureAwait(false);

                            // Task.Yield() позволяет другим задачам в пуле потоков
                            // выполниться, предотвращая монополизацию ресурсов одним воркером.
                            await Task.Yield();
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }

                        // Атомарное увеличение счетчика для предотвращения Race Condition (состояния гонки)
                        int newCount = Interlocked.Increment(ref processed);

                        progress.Report(
                            new ProgressInfo
                            {
                                CurrentFile = currentFile,
                                ProcessedCount = newCount,
                                TotalCount = total,
                                Status = ProcessingStatus.Done,
                            }
                        );
                    }
                },
                cancellationToken
            );
        }

        // Ждем завершения всех воркеров
        await Task.WhenAll(workers);
    }
}
