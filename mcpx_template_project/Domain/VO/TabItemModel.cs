using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    public partial class TabItemModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _component = string.Empty;

        [ObservableProperty]
        private Frame _frame = null!;

        [ObservableProperty]
        private bool _canClose = true;
    }
}
