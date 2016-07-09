using System.ComponentModel;

namespace MSHU.CarWash.UWP.ViewModels
{
    /// <summary>
    /// The class implements the INotifyPropertyChanged interface and hence
    /// it can act as Binding source.
    /// The basic intent is to provide a common implementation that all viewmodel
    /// classes shall derive from.
    /// </summary>
    public class Bindable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
