using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SQL_Export.Src
{
	class RelayCommand : ICommand
	{
		private Action<object> _methodToExecute;
        private Func<object, bool> _canExecuteMethod;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove {  CommandManager.RequerySuggested -= value;}
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute)
        {
			_methodToExecute = execute;
			_canExecuteMethod = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecuteMethod == null || _canExecuteMethod(parameter);
        }

        public void Execute(object? parameter)
        {
			_methodToExecute(parameter);
        }

    }
}
