using MSHU.CarWash.UWP.Common;
using MSHU.CarWash.UWP.Views;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MSHU.CarWash.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Holds a reference to the AuthenticationManager.
        /// </summary>
        public static AuthenticationManager AuthenticationManager = new AuthenticationManager();

        public static readonly int MAX_RESERVATIONS_PER_DAY = 6;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnAppResuming;
            this.UnhandledException += OnUnhandledException;

            // integrate with HockeyApp
            Microsoft.HockeyApp.HockeyClient.Current.Configure("0205bd2e73b44b9f9f4dc8d0f5dc95e4");
        }


        /// <summary>
        /// Handling unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var message = string.Format("Unexpected error occured: {0}\nError report is sent, application is closing.", e.Message);
            
            //Report to HockeyApp
            Diagnostics.ReportError(message);
            
            // UI code must run on UI thread
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(message);
                await dialog.ShowAsync();
                App.Current.Exit();
            });
        }

        /// <summary>
        /// Triggers when 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnAppResuming(object sender, object e)
        {
            //after resuming we are using the same logic as start up
            await ExtendedSplash.ShowMainPage();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            if (e.PreviousExecutionState != ApplicationExecutionState.Running)
            {
                bool loadState = (e.PreviousExecutionState == ApplicationExecutionState.Terminated);
                ExtendedSplash extendedSplash = new ExtendedSplash(e.SplashScreen, loadState);
                Window.Current.Content = extendedSplash;
            }

            Window.Current.Activate();
            this.SetupTitleBar();
            this.SetupSystemTray();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        /// <summary>
        /// Set up the title bar style
        /// </summary>
        private void SetupTitleBar()
        {
            // Setting the color of the title bar for the desktop app
            var color = (Color)Current.Resources["VeryDarkBlue"];
            var foregroundcolor = (Color)Current.Resources["LightGrey"];
            var foregroundinactivecolor = (Color)Current.Resources["LightGrey"];
            var hovercolor = (Color)Current.Resources["DarkBlue"];
            var pressedcolor = (Color)Current.Resources["LightBlue"];
            var titlebar = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TitleBar;
            titlebar.BackgroundColor = color;
            titlebar.ForegroundColor = foregroundcolor;
            titlebar.ButtonBackgroundColor = color;
            titlebar.ButtonForegroundColor = foregroundcolor;
            titlebar.ButtonHoverBackgroundColor = hovercolor;
            titlebar.ButtonHoverForegroundColor = foregroundcolor;
            titlebar.ButtonPressedForegroundColor = foregroundcolor;
            titlebar.ButtonPressedBackgroundColor = pressedcolor;

            //// Set inactive title bar color
            titlebar.InactiveBackgroundColor = color;
            titlebar.InactiveForegroundColor = foregroundinactivecolor;
            titlebar.ButtonInactiveBackgroundColor = color;
            titlebar.ButtonInactiveForegroundColor = foregroundinactivecolor;
        }

        private void SetupSystemTray()
        {

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.BackgroundOpacity = 1;
                statusBar.BackgroundColor = (Color)Current.Resources["VeryDarkBlue"];
                statusBar.ForegroundColor = Colors.White;
            }
        }

    }
}
