using System;
using System.Windows.Input;

namespace AspectFix.Core
{
    public class RelayCommand : ICommand
    {
        private Action<object> _execute;
        private Func<object, bool> _canExecute;
        
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this._execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this._canExecute = canExecute ?? (o => true);
        }
        
        public bool CanExecute(object parameter) => _canExecute == null || this._canExecute(parameter);
        
        public void Execute(object parameter) => this._execute(parameter);
    }
}