using MSHU.CarWash.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MSHU.CarWash.UWP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : BasePage
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Initializes the ViewModel property to an instance of MainViewModel.
        /// </summary>
        protected override void InitializePage()
        {
            ViewModel = new MainViewModel();
            ((MainViewModel)ViewModel).UserAuthenticated += ViewModel_UserAuthenticated;

            base.InitializePage();
        }

        private void ViewModel_UserAuthenticated(object sender, System.EventArgs e)
        {
            // Create the app shell that will host the entire application content.
            AppShell shell = new AppShell();
            Window.Current.Content = shell;
            Window.Current.Activate();
            //Navigate(typeof(HomePage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }
}
