using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bridges.Models
{
    public class SharedData : INotifyPropertyChanged
    {
        private string _gameStatus = "";
        private bool _showMissing = false;
        private (int, int, int, int) _dimensions = (-1, -1, -1, -1);

        public List<Island> Islands { get; } = new List<Island>();
        public List<Bridge> Bridges { get; } = new List<Bridge>();

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

        public (int, int, int, int) GetDimensions
        {
            get
            {
                return _dimensions;
            }
        }

        public (int, int, int) SetDimensions
        {
            set
            {
                if (value.Item1 <= 0 ||  value.Item2 <= 0 || value.Item3 <= 0) _dimensions = (-1, -1, -1, -1);
                else _dimensions = (value.Item1, value.Item2, value.Item3, value.Item1 * value.Item2 / value.Item3);
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
