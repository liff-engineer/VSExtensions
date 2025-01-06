using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Aid.Shared
{
    /// <summary>
    /// CommandPalette.xaml 的交互逻辑
    /// </summary>
    public partial class CommandPalette : UserControl
    {
        private CommandPaletteViewModel viewModel = new CommandPaletteViewModel();
        public CommandPalette()
        {
            InitializeComponent();
            DataContext = viewModel;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ListView.ItemsSource);
            view.Filter = CommandFilter;

            Loaded += CommandPalette_Loaded;
        }

        private bool CommandFilter(object item)
        {
            if (String.IsNullOrEmpty(Filter.Text))
                return true;
            else if (item is CommandDescriptor command)
            {
                if (command.Label.IndexOf(Filter.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
                if (command.Group.IndexOf(Filter.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        private void CommandPalette_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.InitializeCommand.Execute(null);
        }

        private void Filter_TextChanged(object sender, TextChangedEventArgs e)
        {
            //CollectionViewSource.GetDefaultView(ListView.ItemsSource).Refresh();
            //筛选时,默认选中第一项
            ICollectionView view = CollectionViewSource.GetDefaultView(ListView.ItemsSource);
            view.Refresh();
            if (!view.IsEmpty)
            {
                view.MoveCurrentToFirst();
                view.Refresh();
                if (view.CurrentItem != null)
                {
                    ListView.SelectedItem = view.CurrentItem;
                }
            }
        }

        private void Filter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter)
            {
                ListView.Focus();
                e.Handled = false;
                return;
            }
        }

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = false;
                return;
            }
            if (e.Key != Key.Enter)
            {
                Filter.Focus();
            }
            else
            {
                ExecuteCommand();
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExecuteCommand();
        }

        private void ExecuteCommand()
        {
            if (ListView.SelectedItem is CommandDescriptor command)
            {
                viewModel.SelectedCommand = command;
                viewModel.ExecuteCommand.Execute(null);
            }
        }
    }
}
