using MSHU.CarWash.UWP.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MSHU.CarWash.UWP.Views
{
    public class BasePage : Page
    {
        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(BasePage), typeof(MainPage), new PropertyMetadata(null));

        public Bindable ViewModel
        {
            get { return (Bindable)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public BasePage()
        {
            InitializePage();
        }

        /// <summary>
        /// Sets the DataContext to the value of the ViewModel property.
        /// </summary>
        protected virtual void InitializePage()
        {
            DataContext = ViewModel;
        }

        protected void Navigate(Type targetPage)
        {
            Frame mainFrame = Window.Current.Content as Frame;
            mainFrame?.Navigate(targetPage);
        }
    }
}
