using Bridges.Models;

namespace Bridges;

public partial class CreateGame : ContentPage
{
	private GameManager _gameManager;
	private SharedData _sharedData;
    private int _maxIslands;
    private int _width = 0;
    private int _height = 0;
	private int _count = 0;

    public CreateGame(GameManager gameManager, SharedData sharedData)
	{
		InitializeComponent();
		_gameManager = gameManager;
		_sharedData = sharedData;
	}

    private void Dimensions_Changed(object sender, TextChangedEventArgs e)
    {
		var widthText = Width.Text;
		var heightText = Height.Text;
        int.TryParse(widthText, out _width);
		int.TryParse(heightText, out _height);
		DimensionAlert.Text = "";
		Islands.IsEnabled = false;
        Islands.Placeholder = "";
        if (_width < 4 || _width > 25 || _height < 4 || _height > 25)
		{
            DimensionAlert.Text = "Width and Height must contain a value from 4 to 25!";
            Islands.IsEnabled = false;
            Islands.Placeholder = "";
			_width = 0;
			_height = 0;
		}
		else
		{
			_maxIslands = _width * _height / 5;
			Islands.IsEnabled = true;
			Islands.Placeholder = $"Number from 2 to {_maxIslands}";
		}
		if (widthText == "" && heightText == "")
		{
            DimensionAlert.Text = "";
            Islands.IsEnabled = false;
            Islands.Placeholder = "";
            _width = 0;
            _height = 0;
        }
    }

    private void Islands_Changed(object sender, TextChangedEventArgs e)
	{
		var islandsText = Islands.Text;
		int.TryParse(islandsText, out _count);
		IslandsAlert.Text = "";
		if (_count < 2 || _count > _maxIslands)
		{
			IslandsAlert.Text = $"Islands must contain a value from 2 to {_maxIslands}!";
			_count = 0;

		}
		if (islandsText == "")
		{
			IslandsAlert.Text = "";
			_count = 0;
		}

    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
		await Navigation.PopAsync();
    }

    private async void OK_Clicked(object sender, EventArgs e)
    {
		if (Automatic.IsChecked)
		{
			_gameManager.CreateGame(_sharedData.ShowMissing);
			await Navigation.PopAsync();
		}
		else
		{
			if (_width > 0 && _height > 0)
			{
				if (ManualIslandCount.IsChecked)
				{
					if (_count > 0)
					{
						_gameManager.CreateGame(_sharedData.ShowMissing, _width, _height, _count);
						await Navigation.PopAsync();
					}
				}
				else
				{
					_gameManager.CreateGame(_sharedData.ShowMissing, _width, _height);
					await Navigation.PopAsync();
				}
			}
		}
    }
}