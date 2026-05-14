namespace ErrorFinder.Models;

/// <summary>
/// Перечисление возможных статусов обработки файла.
/// </summary>
public enum ProcessingStatus
{
    Pending,
    Processing,
    Done,
    ErrorFound,
    Canceled,
}

/// <summary>
/// Информация о текущем прогрессе для передачи в UI.
/// </summary>
public class ProgressInfo
{
    public string CurrentFile { get; set; } = "";
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public double Percent => TotalCount == 0 ? 0 : 100.0 * ProcessedCount / TotalCount;
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public bool ErrorFound { get; set; }
}
