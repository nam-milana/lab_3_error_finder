using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ErrorFinder.Engine;
using ErrorFinder.Interfaces;
using ErrorFinder.Models;
using Microsoft.Win32;

namespace ErrorFinder.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly FileProcessingEngine _engine;
    private readonly IFileListProvider _fileProvider;
    private readonly IFileProcessor _processor;
    private CancellationTokenSource? _cts;

    public ObservableCollection<string> ProcessingLog { get; } = [];
    public ObservableCollection<string> ErrorFiles { get; } = [];

    public MainViewModel(
        IFileListProvider fileProvider,
        IFileProcessor processor,
        FileProcessingEngine engine
    )
    {
        _fileProvider = fileProvider;
        _processor = processor;
        _engine = engine;

        StartCommand = new RelayCommand(
            StartAsync,
            () => !IsRunning && !string.IsNullOrEmpty(SelectedFolder)
        );
        CancelCommand = new RelayCommand(Cancel, () => IsRunning);
        SelectFolderCommand = new RelayCommand(SelectFolder);
    }

    private string _selectedFolder = "";

    /// <summary>
    /// Путь к выбранной пользователем папке.
    /// </summary>
    /// <remarks>
    /// При изменении этого свойства автоматически обновляется состояние команды StartCommand, чтобы предотвратить запуск без выбора папки.
    /// </remarks>
    public string SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            _selectedFolder = value;
            OnPropertyChanged();
            RelayCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _isRunning;

    /// <summary>
    /// Флаг, указывающий, запущена ли сейчас обработка.
    /// </summary> <remarks>
    /// При изменении этого свойства автоматически обновляется состояние команд StartCommand и CancelCommand,
    /// чтобы предотвратить некорректные действия (например, запуск при уже запущенной обработке
    /// или отмену при остановленной обработке).
    /// </remarks>
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            OnPropertyChanged();
            RelayCommand.RaiseCanExecuteChanged();
        }
    }

    private string _status = "";

    /// <summary>
    /// Общий текстовый статус текущей операции.
    /// </summary> <remarks>
    /// Это свойство предназначено для отображения общего состояния процесса (например, "Поиск файлов...", "Обработка...", "Завершено", "Отменено" или сообщение об ошибке).
    /// Оно обновляется в ключевых точках процесса, чтобы пользователь всегда был информирован о том, что происходит.
    /// </remarks>
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    private double _percent;

    /// <summary>
    /// Процент выполнения (0-100) для ProgressBar.
    /// </summary>
    public double Percent
    {
        get => _percent;
        set
        {
            _percent = value;
            OnPropertyChanged();
        }
    }

    private string _currentFile = "";

    /// <summary>
    /// Имя файла, который обрабатывается в данный момент.
    /// </summary>
    public string CurrentFile
    {
        get => _currentFile;
        set
        {
            _currentFile = value;
            OnPropertyChanged();
        }
    }

    public ICommand StartCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectFolderCommand { get; }

    /// <summary>
    /// Основной метод запуска обработки.
    /// </summary>
    private async Task StartAsync()
    {
        PrepareForStart();
        _cts = new CancellationTokenSource();

        // Пункт В: Декомпозиция логики прогресса
        var progress = new Progress<ProgressInfo>(HandleProgress);

        try
        {
            await _engine.RunAsync(_fileProvider, SelectedFolder, _processor, progress, _cts.Token);
            Status = "✅ Завершено";
        }
        catch (OperationCanceledException)
        {
            Status = "❌ Отменено пользователем";
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            CleanupCts();
        }
    }

    /// <summary>
    /// Обработка прогресса обработки.
    /// </summary>
    /// <param name="info"> Информация о прогрессе обработки. </param>
    private void HandleProgress(ProgressInfo info)
    {
        CurrentFile = info.CurrentFile;
        Percent = info.Percent;

        if (info.ErrorFound && !string.IsNullOrEmpty(info.CurrentFile))
        {
            if (!ErrorFiles.Contains(info.CurrentFile))
                ErrorFiles.Add(info.CurrentFile);
        }

        if (info.Status == ProcessingStatus.Done || info.Status == ProcessingStatus.ErrorFound)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string mark = info.ErrorFound ? "⚠️ ОШИБКА" : "✓";

            if (ProcessingLog.Count > 200)
                ProcessingLog.RemoveAt(0);

            ProcessingLog.Add($"[{time}] {mark} {info.CurrentFile}");
        }
    }

    /// <summary>
    /// Подготавливает состояние ViewModel перед запуском нового процесса поиска.
    /// </summary>
    private void PrepareForStart()
    {
        IsRunning = true;
        ErrorFiles.Clear();
        ProcessingLog.Clear();
        Percent = 0;
        Status = "Поиск файлов...";
    }

    /// <summary>
    /// Инициирует отмену текущей асинхронной операции через токен.
    /// </summary>
    private void Cancel() => _cts?.Cancel();

    /// <summary>
    /// Безопасно очищает ресурсы токена отмены.
    /// </summary>
    private void CleanupCts()
    {
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Вызывает стандартное диалоговое окно Windows для выбора директории.
    /// </summary>
    private void SelectFolder()
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
            SelectedFolder = dialog.FolderName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Уведомляет UI о том, что значение свойства изменилось (необходимо для WPF Binding).
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Освобождает ресурсы при уничтожении ViewModel.
    /// </summary>
    public void Dispose()
    {
        CleanupCts();
        GC.SuppressFinalize(this);
    }
}
