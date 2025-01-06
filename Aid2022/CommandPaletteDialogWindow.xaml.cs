using Microsoft.VisualStudio.PlatformUI;

namespace Aid
{
    public partial class CommandPaletteDialogWindow : DialogWindow
    {
        public CommandPaletteDialogWindow()
        {
            this.HasMinimizeButton = true;
            this.HasMaximizeButton = true;
            InitializeComponent();

            Loaded += CommandPaletteDialogWindow_Loaded;
        }

        private void CommandPaletteDialogWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CommandPalette.Filter.Focus();
        }
    }
}
