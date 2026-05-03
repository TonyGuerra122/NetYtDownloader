using System.Windows.Input;

namespace GUI.Commands;

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?>? _execute;
    private readonly Func<T?, Task>? _executeAsync;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke((T?)parameter) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (_executeAsync is not null)
            await _executeAsync((T?)parameter);
        else
            _execute?.Invoke((T?)parameter);
    }

    public event EventHandler? CanExecuteChanged;
}