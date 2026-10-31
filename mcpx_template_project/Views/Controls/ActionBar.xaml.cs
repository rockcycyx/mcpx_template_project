using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace mcpx_template_project.Views.Controls
{
    public partial class ActionBar : UserControl
    {
        public ActionBar()
        {
            InitializeComponent();
        }

        public ICommand AddCommand
        {
            get => (ICommand)GetValue(AddCommandProperty);
            set => SetValue(AddCommandProperty, value);
        }

        public static readonly DependencyProperty AddCommandProperty =
            DependencyProperty.Register(nameof(AddCommand), typeof(ICommand), typeof(ActionBar), new PropertyMetadata(null));

        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(ActionBar), new PropertyMetadata(null));

        public ICommand ImportCommand
        {
            get => (ICommand)GetValue(ImportCommandProperty);
            set => SetValue(ImportCommandProperty, value);
        }

        public static readonly DependencyProperty ImportCommandProperty =
            DependencyProperty.Register(nameof(ImportCommand), typeof(ICommand), typeof(ActionBar), new PropertyMetadata(null));

        public ICommand ExportCommand
        {
            get => (ICommand)GetValue(ExportCommandProperty);
            set => SetValue(ExportCommandProperty, value);
        }

        public static readonly DependencyProperty ExportCommandProperty =
            DependencyProperty.Register(nameof(ExportCommand), typeof(ICommand), typeof(ActionBar), new PropertyMetadata(null));
    }
}