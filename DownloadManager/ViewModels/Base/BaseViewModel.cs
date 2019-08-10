using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DownloadManager.ViewModels
{
    class BaseViewModel : INotifyPropertyChanged
    {
        public virtual event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnProeprtyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual bool SetProperty<T>(ref T source, T newValue, [CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(source, newValue))
                return false;
            source = newValue;
            OnProeprtyChanged(name);
            return true;


        }
    }
}
