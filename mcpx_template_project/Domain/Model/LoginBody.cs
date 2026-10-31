using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mcpx_template_project.Domain.Model
{
    /// <summary>
        /// 用户登录模型
        /// </summary>
        public partial class LoginBody : ObservableObject
        {
            /// <summary>
            /// 工号
            /// </summary>
            [ObservableProperty]
            private string _employeeId = string.Empty;

            /// <summary>
            /// 密码
            /// </summary>
            [ObservableProperty]
            private string _password = string.Empty;
        }
}
