using System;
using System.Windows;

namespace SC_Buddy.UI
{
    /// <summary>
    /// Interaction logic for ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow : Window
    {
        class ExceptionVM
        {
            public ExceptionVM(Exception e)
            {
                Exception = e;
                ExceptionType = e.GetType().FullName;
            }

            public Exception Exception { get; }
            public string? ExceptionType { get; }
        }

        private readonly Application _application;
        
        public ExceptionWindow(Application a, Exception e)
        {
            _application = a;
            DataContext = new ExceptionVM(e);

            InitializeComponent();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) => _application.Shutdown();

        private void Window_Closed(object sender, EventArgs e) => _application.Shutdown();
    }
}
