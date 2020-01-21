using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace FFBase
{
    public interface ICommandable
    {
        Dictionary<string, FFUICommand> Commands { get; set; }
    }

    public static class FFUICommandStack
    {

    }

    public class FFUICommand: ICommand
    {
        public object Source { get; set; }

        public Action Redo { get; set; }

        public Action Undo { get; set; }

        public string CommandInfo { get; set; }

        public FFUICommand()
        {

        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}
