using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;
using mcpx_template_project.Core.Logging;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Helper;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.Aspect.Attributes
{
    /// <summary>
    /// 日志拦截特性
    /// </summary>
    /// <author>Rudeus</author>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property,
                AllowMultiple = false, Inherited = true)]
    public sealed class LogAttribute : OnMethodBoundaryAspect
    {
        // 模块标题
        public string Title { get; set; } = "";
        // 业务类型
        public BusinessType BusinessType { get; set; } = BusinessType.Other;
        // 操作类型
        public OperatorType OperatorType { get; set; } = OperatorType.Desktop;
        // 是否保存请求参数
        public bool IsSaveRequestData { get; set; } = true;
        // 是否保存返回结果
        public bool IsSaveResponseData { get; set; } = true;
        // 排除的参数名（逗号分隔）
        public string ExcludeParamNames { get; set; } = "";

        private string _methodName = string.Empty;
        private string _className = string.Empty;
        private Stopwatch? _stopwatch;

        /// <summary>
        /// 序列化选项：中文等非 ASCII 字符不转义，直接以原文写入数据库
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 日志内容最大长度（防止极端情况下无限膨胀）
        /// </summary>
        private const int MaxLogContentLength = 100_000;

        /// <summary>
        /// 通用接口入口切面
        /// </summary>
        public override void OnEntry(MethodExecutionArgs args)
        {
            _className = args.Method.DeclaringType?.Name ?? "Unknown";
            _methodName = args.Method.Name;

            var parameters = args.Method.GetParameters()
                .Select((p, i) => $"{p.Name}={args.Arguments[i] ?? "null"}")
                .ToList();

            var logService = AppHelper.GetService<ILogService>();
            logService?.LogInformation($"[AOP-ENTER] {_className}.{_methodName}({string.Join(", ", parameters)})");

            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// 通用接口出口切面 —— 构建操作日志并异步入库
        /// </summary>
        public override void OnExit(MethodExecutionArgs args)
        {
            _stopwatch?.Stop();
            var elapsed = _stopwatch?.ElapsedMilliseconds ?? 0;

            var logService = AppHelper.GetService<ILogService>();
            logService?.LogInformation($"[AOP-EXIT] {_className}.{_methodName} - 耗时: {elapsed}ms, 返回值: {args.ReturnValue}");

            // 构建操作日志并异步保存
            var operLog = BuildOperLog(args, elapsed, null);
            SaveOperLogFireAndForget(operLog);
        }

        /// <summary>
        /// 通用异常捕获 —— 构建带错误信息的操作日志并异步入库
        /// </summary>
        public override void OnException(MethodExecutionArgs args)
        {
            _stopwatch?.Stop();
            var elapsed = _stopwatch?.ElapsedMilliseconds ?? 0;

            var logService = AppHelper.GetService<ILogService>();
            logService?.LogError(args.Exception, $"[AOP-EXCEPTION] {_className}.{_methodName} - 耗时: {elapsed}ms");

            // 构建带异常的操作日志
            var operLog = BuildOperLog(args, elapsed, args.Exception);
            SaveOperLogFireAndForget(operLog);
        }

        /// <summary>
        /// 构建 SysOperLog 对象
        /// </summary>
        private SysOperLog BuildOperLog(MethodExecutionArgs args, long elapsed, Exception? ex)
        {
            var operLog = new SysOperLog
            {
                Title = Title,
                BusinessType = (int)BusinessType,
                OperatorType = (int)OperatorType,
                Method = $"{_className}.{_methodName}()",
                CostTime = elapsed,
                Status = ex != null ? (int)BusinessStatus.Fail : (int)BusinessStatus.Success
            };

            // 异常信息
            if (ex != null)
            {
                operLog.ErrorMsg = Truncate(ex.Message, MaxLogContentLength);
            }

            // 请求参数
            if (IsSaveRequestData)
            {
                operLog.OperParam = GetRequestParams(args);
            }

            // 返回结果（异步方法的 ReturnValue 是内部状态机类型，跳过序列化）
            if (IsSaveResponseData && ex == null && args.ReturnValue != null)
            {
                var returnValue = args.ReturnValue;
                var typeName = returnValue.GetType().FullName ?? string.Empty;

                if (!typeName.Contains("AsyncStateMachine")
                    && !typeName.Contains("VoidTaskResult"))
                {
                    try
                    {
                        operLog.JsonResult = Truncate(JsonSerializer.Serialize(returnValue, SerializerOptions), MaxLogContentLength);
                    }
                    catch
                    {
                        operLog.JsonResult = Truncate(returnValue.ToString() ?? string.Empty, MaxLogContentLength);
                    }
                }
            }

            return operLog;
        }

        /// <summary>
        /// 获取并序列化请求参数（排除指定参数名）
        /// </summary>
        private string GetRequestParams(MethodExecutionArgs args)
        {
            var excludeSet = new HashSet<string>(
                ExcludeParamNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim()));

            var parameters = args.Method.GetParameters();
            var paramDict = new Dictionary<string, object?>();

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramName = parameters[i].Name;
                if (!string.IsNullOrEmpty(paramName) && !excludeSet.Contains(paramName))
                {
                    paramDict[paramName] = args.Arguments[i];
                }
            }

            return Truncate(JsonSerializer.Serialize(paramDict, SerializerOptions), MaxLogContentLength);
        }

        /// <summary>
        /// 异步入库（fire-and-forget，不阻塞主流程）
        /// </summary>
        private void SaveOperLogFireAndForget(SysOperLog operLog)
        {
            var operLogService = AppHelper.GetService<IOperLogService>();
            if (operLogService == null) return;

            _ = operLogService.SaveOperLogAsync(operLog);
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}