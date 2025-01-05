using Bridges.Drawables;
using Bridges.Models;

namespace Bridges
{
    public partial class MainPage : ContentPage
    {
        private GameManager _gameManager;
        private Field _field;
        private CreateGame _createGame;
        private SharedData _sharedData;
        private System.Timers.Timer _timer;

        public MainPage(GameManager gameManager, Field field, CreateGame createGame, SharedData sharedData)
        {
            InitializeComponent();
            BindingContext = sharedData;
            GraphicsView.Drawable = field;

            _gameManager = gameManager;
            _field = field;
            _createGame = createGame;
            _sharedData = sharedData;

            _gameManager.GraphicsView = GraphicsView;
            _sharedData.ShowMissing = ShowMissing.IsChecked;

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += NextBridge_Clicked;
        }

        private async void CreateGame_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(_createGame);
        }

        private async void LoadGame_Clicked(object sender, EventArgs e)
        {
            var answer = await _gameManager.LoadGame();
            if (answer != "OK") await DisplayAlert("Information", answer, "OK");
        }

        private async void SaveGame_Clicked(object sender, EventArgs e)
        {
            if (await _gameManager.SaveGame()) await DisplayAlert("Information", "Game saved!", "OK");
        }

        private void ResetGame_Clicked(object sender, EventArgs e)
        {
            _gameManager.RemoveAllBridges();
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
