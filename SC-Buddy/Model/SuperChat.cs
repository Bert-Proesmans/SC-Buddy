using SC_Buddy.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SC_Buddy.Model
{
    record SuperChat(
        Color HeaderBackground,
        Color? TextBackground,
        Color TextForeground,
        Valuta MinimumValuta);

    class SuperChatVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private Brush _profileBackground = new SolidColorBrush(Color.FromRgb(120, 144, 156));
        private Brush _headerBackground = new SolidColorBrush(SuperChatData.RED.HeaderBackground);
        private Brush? _textBackground = null;
        private Brush _textForeground = new SolidColorBrush(SuperChatData.RED.TextForeground);
        private string _name = "[Name]";
        private string _amount = "[Amount]";
        private string _message = "[Message]";
        private DirectionOfValuta? _directionOfValuta = null;

        public Brush ProfileBackground
        {
            get => _profileBackground;
            set => SetAndNotify(ref _profileBackground, value);
        }
        public Brush HeaderBackground
        {
            get => _headerBackground;
            set => SetAndNotify(ref _headerBackground, value);
        }
        public Brush? TextBackground
        {
            get => _textBackground;
            set => SetAndNotify(ref _textBackground, value);
        }
        public Brush TextForeground
        {
            get => _textForeground;
            set
            {
                SetAndNotify(ref _textForeground, value);
                NotifyOfPropertyChange(nameof(TextForegroundTransparent));
            }
        }
        public Brush TextForegroundTransparent
        {
            get
            {
                var copy = _textForeground.Clone();
                copy.Opacity = 0.70;
                return copy;
            }
        }
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }
        public string Amount
        {
            get => _amount;
            set => SetAndNotify(ref _amount, value);
        }
        public string Message
        {
            get => _message;
            set => SetAndNotify(ref _message, value);
        }

        public DirectionOfValuta? DirectionOfValuta
        {
            get => _directionOfValuta;
            set => SetAndNotify(ref _directionOfValuta, value);
        }

        private void SetAndNotify<T>(ref T oldValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue)) return;

            oldValue = newValue;
            NotifyOfPropertyChange(propertyName);
        }

        private void NotifyOfPropertyChange([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
