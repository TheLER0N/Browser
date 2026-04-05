using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GhostBrowser.ViewModels
{
    /// <summary>
    /// Асинхронная реализация ICommand.
    /// 
    /// Зачем: RelayCommand принимает только Action, что вынуждает использовать
    /// async void (неперехватываемые исключения). AsyncRelayCommand принимает
    /// Func&lt;object?, Task&gt; — исключения логируются, кнопка блокируется
    /// на время выполнения.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            // Блокируем повторное выполнение, пока команда ещё выполняется
            if (_isExecuting) return false;
            return _canExecute?.Invoke(parameter) ?? true;
        }

        /// <summary>
        /// Выполняет команду асинхронно.
        /// Исключения перехватываются и логируются — не крашат приложение.
        /// </summary>
        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute(parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand error: {ex.Message}");
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
