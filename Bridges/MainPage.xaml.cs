using Bridges.Models;
using System.Diagnostics;
using System.Timers;

namespace Bridges
{
    public partial class MainPage : ContentPage
    {
        private GameManager _gameManager;
        private CreateGame _createGame;
        private SharedData _sharedData;
        private System.Timers.Timer _timer;

        public MainPage(GameManager gameManager, CreateGame createGame, SharedData sharedData)
        {
            InitializeComponent();
            BindingContext = sharedData;
            _gameManager = gameManager;
            _gameManager.GraphicsView = GraphicsView;
            _createGame = createGame;
            _sharedData = sharedData;
            _sharedData.ShowMissing = ShowMissing.IsChecked;
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += NextBridge_Clicked;
        }

        private async void CreateGame_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(_createGame);
        }

        private async void SaveGame_Clicked(object sender, EventArgs e)
        {
            if (await _gameManager.SaveGame()) await DisplayAlert("Information", "Game saved!", "OK");
        }

        private void GraphicsView_Left(object sender, TappedEventArgs e)
        {
            if (e.GetPosition((View)sender) is Point point) _gameManager.ProcessClick(point.X, point.Y, "left");
        }

        private void GraphicsView_Right(object sender, TappedEventArgs e)
        {
            if (e.GetPosition((View)sender) is Point point) _gameManager.ProcessClick(point.X, point.Y, "right");
        }

        private void ShowMissing_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            _sharedData.ShowMissing = ShowMissing.IsChecked;
        }

        private void NextBridge_Clicked(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!_gameManager.NextBridge())
                {
                    _timer.Stop();
                    await DisplayAlert("Information", "No further bridge can be added with certainty in the current game state!", "OK");
                }
            });
        }

        private void AutoSolve_Clicked(object sender, EventArgs e)
        {
            if (_timer.Enabled) _timer.Stop();
            else _timer.Start();
        }

    }

}
