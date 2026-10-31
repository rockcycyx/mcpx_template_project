using System;
using System.Windows;
using System.Windows.Controls;

namespace mcpx_template_project.ViewModels.Admin
{
    public static class PasswordBoxHelper
    {
        // 绑定密码附加属性
        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, OnBindPasswordPropertyChanged)
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                });

        // 标记防止双向绑定死循环
        private static bool _isSyncing;

        public static string GetBindPassword(DependencyObject obj) => (string)obj.GetValue(BindPasswordProperty);
        public static void SetBindPassword(DependencyObject obj, string value) => obj.SetValue(BindPasswordProperty, value);

        // 外部VM赋值 → 同步到PasswordBox
        private static void OnBindPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox pwdBox || _isSyncing)
                return;

            // 同步到 PasswordBox，即使为空字符串也执行
            string newPassword = e.NewValue?.ToString() ?? string.Empty;
            if (pwdBox.Password != newPassword)
            {
                pwdBox.Password = newPassword;
            }
        }

        // 控件加载时绑定PasswordChanged事件
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnAttachChanged));

        public static bool GetAttach(DependencyObject obj) => (bool)obj.GetValue(AttachProperty);
        public static void SetAttach(DependencyObject obj, bool value) => obj.SetValue(AttachProperty, value);

        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox pwdBox)
            {
                if ((bool)e.NewValue)
                    pwdBox.PasswordChanged += OnPasswordBoxPasswordChanged;
                else
                    pwdBox.PasswordChanged -= OnPasswordBoxPasswordChanged;
            }
        }

        // PasswordBox输入 → 同步到附加属性（回传给VM）
        private static void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox pwdBox) return;

            _isSyncing = true;
            SetBindPassword(pwdBox, pwdBox.Password);
            _isSyncing = false;
        }
    }
}
