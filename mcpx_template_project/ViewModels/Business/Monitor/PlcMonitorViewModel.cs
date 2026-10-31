using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Plc;

namespace mcpx_template_project.ViewModels.Business.Monitor
{
    /// <summary>
    /// 实时监控视图模型：通过 DispatcherTimer 周期性批量读取所有已连接通讯设定的PLC点位，
    /// 将暂存数据实时展示在界面上。支持按「通讯设定 / 区域 / 设备代号 / PLC地址」筛选查询，
    /// 点开行详情可展开查看该区域点位下每个 PLC 地址的当前值。
    /// </summary>
    /// <author>Rudeus</author>
    public partial class PlcMonitorViewModel : ObservableObject
    {
        private readonly IPlcCommunicationService _plcService;
        private readonly IDialogService _dialogService;
        private readonly DispatcherTimer _timer;

        /// <summary>全量点位（与通讯引擎暂存缓存共享对象引用）</summary>
        private readonly List<PointValueVO> _allPoints = new();

        /// <summary>过滤后展示的点位集合（绑定到 DataGrid）</summary>
        public ObservableCollection<PointValueVO> Points { get; } = new();

        /// <summary>当前选中的点位单头（右侧子值明细面板绑定其 Values）</summary>
        [ObservableProperty]
        private PointValueVO? _selectedPoint;

        /// <summary>是否正在监控中</summary>
        [ObservableProperty]
        private bool _isMonitoring;

        /// <summary>刷新周期（毫秒），默认1000ms</summary>
        [ObservableProperty]
        private int _refreshIntervalMs = 1000;

        /// <summary>已连接的通讯设定数量</summary>
        [ObservableProperty]
        private int _connectedCount;

        /// <summary>点位总数</summary>
        [ObservableProperty]
        private int _pointCount;

        /// <summary>在线点位数</summary>
        [ObservableProperty]
        private int _onlineCount;

        /// <summary>最近刷新时间</summary>
        [ObservableProperty]
        private string _lastRefreshTime = "尚未刷新";

        // ---------- 查询筛选条件 ----------

        /// <summary>通讯设定名称筛选（模糊匹配）</summary>
        [ObservableProperty]
        private string _filterSettingName = string.Empty;

        /// <summary>区域筛选（AI / AO / DI / DO / 全部）</summary>
        [ObservableProperty]
        private string _filterAreaType = "全部";

        /// <summary>设备代号筛选（模糊匹配）</summary>
        [ObservableProperty]
        private string _filterDeviceCode = string.Empty;

        /// <summary>PLC地址筛选（模糊匹配）</summary>
        [ObservableProperty]
        private string _filterPlcAddress = string.Empty;

        public PlcMonitorViewModel(IPlcCommunicationService plcService, IDialogService dialogService)
        {
            _plcService = plcService;
            _dialogService = dialogService;

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(RefreshIntervalMs)
            };
            _timer.Tick += OnTimerTick;
        }

        /// <summary>刷新周期变化时同步 Timer Interval</summary>
        partial void OnRefreshIntervalMsChanged(int value)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(value, 100));
        }

        /// <summary>筛选条件变化时重新应用过滤</summary>
        partial void OnFilterSettingNameChanged(string value) => ApplyFilter();
        partial void OnFilterAreaTypeChanged(string value) => ApplyFilter();
        partial void OnFilterDeviceCodeChanged(string value) => ApplyFilter();
        partial void OnFilterPlcAddressChanged(string value) => ApplyFilter();

        /// <summary>
        /// 启动实时监控：开启 Timer 周期读取，立即执行一次刷新。
        /// </summary>
        [RelayCommand]
        private async Task StartMonitor()
        {
            if (IsMonitoring) return;
            IsMonitoring = true;
            _timer.Start();
            await RefreshAsync();
        }

        /// <summary>
        /// 停止实时监控：关闭 Timer。
        /// </summary>
        [RelayCommand]
        private void StopMonitor()
        {
            IsMonitoring = false;
            _timer.Stop();
        }

        /// <summary>
        /// 手动刷新一次（不影响 Timer 运行状态）。
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                var ids = _plcService.GetConnectedSettingIds();
                ConnectedCount = ids.Count;

                if (ids.Count == 0)
                {
                    LastRefreshTime = $"[{DateTime.Now:HH:mm:ss}] 暂无已连接的通讯设定";
                    return;
                }

                // 批量读取每个已连接通讯设定的所有点位
                foreach (var id in ids)
                {
                    await _plcService.ReadAllPointsAsync(id);
                }

                // 同步暂存缓存到全量集合（保留同一对象引用，仅做增删）
                var cached = _plcService.GetCachedPoints();
                SyncAllPoints(cached);

                ApplyFilter();

                PointCount = Points.Count;
                OnlineCount = Points.Count(p => p.IsOnline);
                LastRefreshTime = $"[{DateTime.Now:HH:mm:ss}] 刷新成功 · {PointCount} 点位 · {OnlineCount} 在线";
            }
            catch (Exception ex)
            {
                LastRefreshTime = $"[{DateTime.Now:HH:mm:ss}] 刷新失败：{ex.Message}";
            }
        }

        /// <summary>
        /// Timer Tick 回调：周期性刷新点位值。
        /// </summary>
        private async void OnTimerTick(object? sender, EventArgs e)
        {
            await RefreshAsync();
        }

        /// <summary>
        /// 将通讯引擎暂存缓存同步到全量集合：
        /// 保留同一对象引用（属性变化由 INPC 自动驱动），仅做增删操作。
        /// </summary>
        private void SyncAllPoints(List<PointValueVO> cached)
        {
            var cacheSet = new HashSet<PointValueVO>(cached);

            // 移除缓存中已不存在的点位
            for (int i = _allPoints.Count - 1; i >= 0; i--)
            {
                if (!cacheSet.Contains(_allPoints[i]))
                    _allPoints.RemoveAt(i);
            }

            // 添加缓存中新出现的点位
            var currentSet = new HashSet<PointValueVO>(_allPoints);
            foreach (var p in cached)
            {
                if (!currentSet.Contains(p))
                    _allPoints.Add(p);
            }
        }

        /// <summary>
        /// 根据当前筛选条件从全量集合中过滤出展示集合。
        /// </summary>
        private void ApplyFilter()
        {
            var setting = FilterSettingName?.Trim() ?? string.Empty;
            var area = FilterAreaType?.Trim() ?? string.Empty;
            var device = FilterDeviceCode?.Trim() ?? string.Empty;
            var plcAddr = FilterPlcAddress?.Trim() ?? string.Empty;

            IEnumerable<PointValueVO> filtered = _allPoints;

            if (!string.IsNullOrEmpty(setting))
                filtered = filtered.Where(p => p.SettingName?.Contains(setting, StringComparison.OrdinalIgnoreCase) == true);

            // 区域："全部"或空视为不筛选
            if (!string.IsNullOrEmpty(area) && !area.Equals("全部", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(p => p.AreaType?.Equals(area, StringComparison.OrdinalIgnoreCase) == true);

            if (!string.IsNullOrEmpty(device))
                filtered = filtered.Where(p =>
                    (p.DeviceCode?.Contains(device, StringComparison.OrdinalIgnoreCase) == true) ||
                    p.Values.Any(sub => sub.DeviceCode?.Contains(device, StringComparison.OrdinalIgnoreCase) == true));

            if (!string.IsNullOrEmpty(plcAddr))
                filtered = filtered.Where(p =>
                    (p.PlcAddress?.Contains(plcAddr, StringComparison.OrdinalIgnoreCase) == true) ||
                    p.Values.Any(sub => sub.PlcAddress?.Contains(plcAddr, StringComparison.OrdinalIgnoreCase) == true));

            var result = filtered.ToList();

            // 同步到展示集合（保留对象引用，仅增删）
            var resultSet = new HashSet<PointValueVO>(result);
            for (int i = Points.Count - 1; i >= 0; i--)
            {
                if (!resultSet.Contains(Points[i]))
                    Points.RemoveAt(i);
            }
            var currentSet = new HashSet<PointValueVO>(Points);
            foreach (var p in result)
            {
                if (!currentSet.Contains(p))
                    Points.Add(p);
            }

            // 同步选中项：若原选中项被过滤掉，回退到列表第一项
            if (SelectedPoint != null && !Points.Contains(SelectedPoint))
                SelectedPoint = Points.FirstOrDefault();
            else if (SelectedPoint == null)
                SelectedPoint = Points.FirstOrDefault();
        }

        /// <summary>
        /// 清空所有筛选条件。
        /// </summary>
        [RelayCommand]
        private void ClearFilter()
        {
            FilterSettingName = string.Empty;
            FilterAreaType = "全部";
            FilterDeviceCode = string.Empty;
            FilterPlcAddress = string.Empty;
        }

        /// <summary>
        /// 写入子值到 PLC：用户在右侧明细表格编辑某个子值后提交。
        /// 定位当前选中单头（SelectedPoint）确定目标通讯设定，按子值的设备代号+地址写值。
        /// </summary>
        [RelayCommand]
        private async Task WriteSubValueAsync(PointSubValueVO? sub)
        {
            if (sub == null) return;

            var point = SelectedPoint;
            if (point == null)
            {
                _dialogService.ShowWarning("请先在左侧选择要写入的点位");
                return;
            }

            var settingId = point.SettingId;
            var deviceCode = sub.DeviceCode ?? point.DeviceCode ?? string.Empty;
            var plcAddress = sub.PlcAddress ?? string.Empty;
            var text = sub.Value?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(deviceCode) || string.IsNullOrEmpty(plcAddress))
            {
                _dialogService.ShowWarning("设备代号或 PLC 地址为空，无法写入");
                return;
            }

            // 解析值类型：位设备写 bool，字设备写数值
            object value;
            try
            {
                if (PlcHelper.IsBitDevice(deviceCode))
                {
                    value = text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase)
                        || text.Equals("on", StringComparison.OrdinalIgnoreCase);
                }
                else if (ushort.TryParse(text, out var us))
                {
                    value = us;
                }
                else if (int.TryParse(text, out var iv))
                {
                    value = iv;
                }
                else if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dv))
                {
                    value = dv;
                }
                else
                {
                    _dialogService.ShowWarning($"无法解析输入值「{text}」为有效数值");
                    return;
                }
            }
            catch
            {
                _dialogService.ShowWarning($"无法解析输入值「{text}」");
                return;
            }

            var ok = await _plcService.WriteSinglePointAsync(settingId, deviceCode, plcAddress, value);
            if (!ok)
            {
                _dialogService.ShowError($"写入 {deviceCode}{plcAddress} 失败，请确认通讯连接正常");
                return;
            }

            // 写入成功后立即刷新该点位，让界面回显 PLC 真实值
            await RefreshAsync();
        }
    }
}
