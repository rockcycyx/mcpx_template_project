using System.Windows;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern.Controls;

namespace mcpx_template_project.Views.Controls
{
    /// <summary>
    /// 为 iNKORE 的 ToggleSwitch 附加 Command 绑定能力。
    /// iNKORE ToggleSwitch 继承自 Control，本身没有 Command/CommandParameter。
    ///
    /// 重要：这里刻意不订阅 Toggled 事件。Toggled 在 IsOn 发生任何变化时都会触发，
    /// 包括 DataGrid 行虚拟化滚动/刷新时绑定重新赋值这类"程序化"变更——那会导致
    /// 滚动列表时误触发 SwitchStatus 之类的命令，把数据库里的状态批量翻转。
    /// 因此改为只响应真实的用户交互（鼠标抬起 / 空格键抬起）。
    /// </summary>
    public static class ToggleSwitchCommandBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(ToggleSwitchCommandBehavior),
                new PropertyMetadata(null, OnCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(object),
                typeof(ToggleSwitchCommandBehavior),
                new PropertyMetadata(null));

        public static ICommand? GetCommand(DependencyObject obj) =>
            (ICommand?)obj.GetValue(CommandProperty);

        public static void SetCommand(DependencyObject obj, ICommand? value) =>
            obj.SetValue(CommandProperty, value);

        public static object? GetCommandParameter(DependencyObject obj) =>
            obj.GetValue(CommandParameterProperty);

        public static void SetCommandParameter(DependencyObject obj, object? value) =>
            obj.SetValue(CommandParameterProperty, value);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ToggleSwitch toggle)
                return;

            // 防止重复订阅：先移除旧的处理器
            toggle.PreviewMouseLeftButtonUp -= OnUserMouseUp;
            toggle.KeyUp -= OnUserKeyUp;

            if (e.NewValue is ICommand)
            {
                // Preview（隧道）事件先于控件自身处理，此时绑定数据尚未被程序化改动，
                // 命令读到的是列表 VO 里的原始状态，翻转方向一定正确
                toggle.PreviewMouseLeftButtonUp += OnUserMouseUp;
                toggle.KeyUp += OnUserKeyUp;
            }
        }

        private static void OnUserMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
                ExecuteCommand(toggle);
        }

        private static void OnUserKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is ToggleSwitch toggle && e.Key == Key.Space)
                ExecuteCommand(toggle);
        }

        private static void ExecuteCommand(ToggleSwitch toggle)
        {
            var command = GetCommand(toggle);
            var parameter = GetCommandParameter(toggle);

            if (command != null && command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
    }
}
