using Xamarin.Forms;

namespace TkXamListViewIssue
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            DataSource = new AppDataSource();

            MainPage = new AppShell();
        }

        public new static App Current => (App)Application.Current;

        public readonly AppDataSource DataSource;

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
