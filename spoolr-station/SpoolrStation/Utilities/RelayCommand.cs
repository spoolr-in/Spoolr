using System.Windows.Input;

namespace SpoolrStation.Utilities;

/// <summary>
/// A command that can always execute and relays its logic to delegates
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        try
        {
            System.Console.WriteLine($"=== RELAYCOMMAND: Executing command ===");
            _execute();
            System.Console.WriteLine($"=== RELAYCOMMAND: Command executed successfully ===");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"=== RELAYCOMMAND EXCEPTION ===");
            System.Console.WriteLine($"Exception Type: {ex.GetType().Name}");
            System.Console.WriteLine($"Exception Message: {ex.Message}");
            System.Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            System.Console.WriteLine($"==============================");
            throw;
        }
    }
}

/// <summary>
/// A command that can always execute and relays its logic to delegates with a parameter
/// </summary>
/// <typeparam name="T">Type of the command parameter</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke((T?)parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute((T?)parameter);
    }
}
