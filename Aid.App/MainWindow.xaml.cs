using Aid.Shared;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aid.App
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private IAsyncRelayCommand InitializeCommand { get; }
        public MainWindow()
        {
            InitializeComponent();

            //string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            //Console.WriteLine(appDataPath);

            InitializeCommand = new AsyncRelayCommand(InitializeAsync);

            Requester Requester = Requester.Instance;
            Requester.InitialzeAfterSolutionReady(@"D:\Repos\VisualStudio\VSExtensions\Aid.App");

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCommand.Execute(null);
        }

        private async Task InitializeAsync()
        {
            Requester Requester = Requester.Instance;
            await Requester.InitializeAsync();
        }

    }
}
