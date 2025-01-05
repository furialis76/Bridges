namespace Bridges
{
    public partial class App : Application
    {
        private MainPage _mainPage;

        public App(MainPage mainPage)
        {
            InitializeComponent();
            _mainPage = mainPage;
            if (Application.Current != null) Application.Current.UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(_mainPage))
            {
                Title = "Bridges Sascha Matzke",
                Width = 500,
                Height = 600
            };
        }
    }
}