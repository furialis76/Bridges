using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bridges.Models
{
    public class SharedData : INotifyPropertyChanged
    {
        private string _gameStatus = "";
        private bool _showMissing = false;
        private (int, int) _dimensions = (0, 0);

        public string GameStatus
        {
            get { return _gameStatus; }
            set
            {
                _gameStatus = value;
                OnPropertyChanged();
            }
        }

        public bool ShowMissing
        {
            get { return _showMissing; }
            set
            {
                _showMissing = value;
                OnPropertyChanged();
            }
        }

        public (int, int) Dimensions
        {
            get { return _dimensions; }
            set
            {
                _dimensions = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
