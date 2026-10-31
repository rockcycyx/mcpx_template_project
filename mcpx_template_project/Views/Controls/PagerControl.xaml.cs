using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using mcpx_template_project.Domain.Model;

namespace mcpx_template_project.Views.Controls
{
    /// <summary>
    /// PagerControl.xaml 的交互逻辑
    /// </summary>
    public partial class PagerControl : UserControl, System.ComponentModel.INotifyPropertyChanged
    {
        public PagerControl()
        {
            InitializeComponent();
            PageSizeOptions = new List<int> { 10, 20, 30, 50 };
        }

        // PageIndex: 1-based
        public static readonly DependencyProperty PageIndexProperty =
            DependencyProperty.Register(nameof(PageIndex), typeof(int), typeof(PagerControl),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPageParamsChanged));

        public int PageIndex
        {
            get => (int)GetValue(PageIndexProperty);
            set => SetValue(PageIndexProperty, value);
        }

        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(PagerControl),
                new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPageParamsChanged));

        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }

        public static readonly DependencyProperty TotalItemsProperty =
            DependencyProperty.Register(nameof(TotalItems), typeof(int), typeof(PagerControl),
                new FrameworkPropertyMetadata(0, OnTotalItemsChanged));

        public int TotalItems
        {
            get => (int)GetValue(TotalItemsProperty);
            set => SetValue(TotalItemsProperty, value);
        }

        public IEnumerable<int> PageSizeOptions
        {
            get => (IEnumerable<int>)GetValue(PageSizeOptionsProperty);
            set => SetValue(PageSizeOptionsProperty, value);
        }

        public static readonly DependencyProperty PageSizeOptionsProperty =
            DependencyProperty.Register(nameof(PageSizeOptions), typeof(IEnumerable<int>), typeof(PagerControl), new PropertyMetadata(null));

        public ICommand? PageChangedCommand
        {
            get => (ICommand?)GetValue(PageChangedCommandProperty);
            set => SetValue(PageChangedCommandProperty, value);
        }

        public static readonly DependencyProperty PageChangedCommandProperty =
            DependencyProperty.Register(nameof(PageChangedCommand), typeof(ICommand), typeof(PagerControl), new PropertyMetadata(null));

        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);

        private static void OnTotalItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (PagerControl)d;
            ctl.CoercePageIndex();
            ctl.RaisePropertyChanged(nameof(TotalPages));
        }

        private static void OnPageParamsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (PagerControl)d;
            ctl.CoercePageIndex();
            ctl.RaisePropertyChanged(nameof(TotalPages));
        }

        private void CoercePageIndex()
        {
            if (TotalPages > 0 && PageIndex > TotalPages) PageIndex = TotalPages;
            if (PageIndex < 1) PageIndex = 1;
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (PageIndex > 1)
            {
                PageIndex--;
                InvokePageChangedCommand();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (PageIndex < TotalPages)
            {
                PageIndex++;
                InvokePageChangedCommand();
            }
        }

        private void Goto_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(GotoBox.Text, out int target))
            {
                if (target < 1) target = 1;
                if (TotalPages > 0 && target > TotalPages) target = TotalPages;
                PageIndex = target;
                InvokePageChangedCommand();
            }
        }

        private void InvokePageChangedCommand()
        {
            if (PageChangedCommand != null && PageChangedCommand.CanExecute(null))
            {
                var param = new PagingParameters
                {
                    PageIndex = PageIndex,
                    PageSize = PageSize
                };
                PageChangedCommand.Execute(param);
            }
        }

        private void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        private void PageSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 当用户切换每页大小时，通知外部重新加载（通常会回到第一页，由 ViewModel 处理）
            InvokePageChangedCommand();
            RaisePropertyChanged(nameof(TotalPages));
        }
    }
}
