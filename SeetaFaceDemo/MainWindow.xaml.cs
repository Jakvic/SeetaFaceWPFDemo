using System;
using System.Windows;


namespace SeetaFaceDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _mainWindowModel?.Stop();
        }

        private MainWindowModel? _mainWindowModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _mainWindowModel= DataContext as MainWindowModel;

            if (_mainWindowModel is not null)
            {
                _mainWindowModel.OnCloseRequestEvent += OnCloseRequestEvent;
            }
        }

        private void OnCloseRequestEvent(object? sender, EventArgs e)
        {
            _mainWindowModel?.Stop();
        }
       
    }
}