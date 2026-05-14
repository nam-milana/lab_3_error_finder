using System.Windows.Input;

namespace ErrorFinder.ViewModels;

/// <summary>
/// Универсальная реализация интерфейса ICommand для делегирования логики методам ViewModel.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Func<Task>? _executeAsync;
    private readonly Action? _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Инициализирует синхронную команду.
    /// </summary>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Инициализирует асинхронную команду.
    /// </summary>
    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Определяет, может ли команда быть выполнена в данный момент.
    /// </summary>
    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

    /// <summary>
    /// Выполняет логику команды. Поддерживает как синхронные, так и асинхронные делегаты.
    /// </summary>
    public async void Execute(object? parameter)
    {
        if (_executeAsync != null)
            await _executeAsync();
        else
            _execute?.Invoke();
    }

    /// <summary>
    /// Принудительно уведомляет систему о необходимости перепроверить возможность выполнения команд.
    /// </summary>
    public static void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

    /// <summary>
    /// Событие, возникающее при изменении условий выполнения команды.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
