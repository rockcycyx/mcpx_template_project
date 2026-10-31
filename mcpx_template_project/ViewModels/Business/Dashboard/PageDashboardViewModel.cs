using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using mcpx_template_project.Converters.Plc;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Machine;
using mcpx_template_project.Service.Business.Plc;
using MachineEntity = mcpx_template_project.Domain.Entities.Business.Machine.Machine;
using MachinePlcEntity = mcpx_template_project.Domain.Entities.Business.Machine.MachinePlc;

namespace mcpx_template_project.ViewModels.Business.Dashboard
{
    /// <summary>
    /// 生产看板视图模型：KPI 指标卡 + 三块图表。
    ///   1. 近 7 日产量趋势（示例数据，待历史产量表落地后替换为真实查询）；
    ///   2. 机台实时状态分布（饼图，数据来自 PLC 缓存，真实）；
    ///   3. 各机台今日产量（条形图，数据来自 PLC 缓存，真实）。
    /// 数据获取方式与首页一致：仅读通讯缓存，不主动请求 PLC，DispatcherTimer 周期刷新。
    /// </summary>
    public partial class PageDashboardViewModel : ObservableObject
    {
        private readonly IMachineService _machineService;
        private readonly IPlcObjectService _plcObjectService;
        private readonly IPlcCommunicationService _plcService;
        private readonly IDialogService _dialogService;
        private readonly DispatcherTimer _timer;

        /// <summary>machine.id → comm_setting_id 映射（定位每台机台的 PLC 缓存）</summary>
        private readonly Dictionary<long, long> _machineSettingMap = new();

        /// <summary>机台列表缓存（避免每秒查库）</summary>
        private List<MachineEntity> _machines = new();

        // ---------- KPI 指标 ----------

        [ObservableProperty] private string _statusText = "正在加载机台...";
        [ObservableProperty] private string _todayProduction = "0";
        [ObservableProperty] private string _totalProduction = "0";
        [ObservableProperty] private string _runningCount = "0 / 0";
        [ObservableProperty] private string _avgEffectivePower = "0 kW";

        // ---------- 图表数据 ----------

        /// <summary>近 7 日产量趋势（柱：日产量；线：目标产量，示例数据）</summary>
        public ObservableCollection<ISeries> TrendSeries { get; } = new();

        /// <summary>机台实时状态分布（饼图，真实缓存数据）</summary>
        public ObservableCollection<ISeries> StatePieSeries { get; } = new();

        /// <summary>各机台今日产量（横向条形图，真实缓存数据）</summary>
        public ObservableCollection<ISeries> MachineProductionSeries { get; } = new();

        // 实时图表的可变数据容器（原地更新 Values，避免每 2s 重建 Series 触发入场动画）
        private readonly List<PieSeries<double>> _statePieItems = new();
        private readonly List<RowSeries<double>> _machineProductionItems = new();

        public Axis[] TrendXAxes { get; } =
        {
            new Axis
            {
                Labels = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" },
                LabelsPaint = new SolidColorPaint(new SKColor(128, 128, 128)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220))
            }
        };

        public Axis[] TrendYAxes { get; } =
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(new SKColor(128, 128, 128)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220))
            }
        };

        public Axis[] MachineXAxes { get; } =
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(new SKColor(128, 128, 128)),
                SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220))
            }
        };

        public Axis[] MachineYAxes { get; } =
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(new SKColor(128, 128, 128)),
                SeparatorsPaint = null
            }
        };

        public PageDashboardViewModel(
            IMachineService machineService,
            IPlcObjectService plcObjectService,
            IPlcCommunicationService plcService,
            IDialogService dialogService)
        {
            _machineService = machineService;
            _plcObjectService = plcObjectService;
            _plcService = plcService;
            _dialogService = dialogService;

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += (_, _) => RefreshRealtimeCharts();
        }

        /// <summary>初始化：加载机台 + 构建趋势图（示例数据），并启动实时刷新。</summary>
        public async Task InitializeAsync()
        {
            try
            {
                _machines = await _machineService.QueryAllAsync();

                _machineSettingMap.Clear();
                foreach (var m in _machines)
                    _machineSettingMap[m.Id] = m.CommSettingId;

                StatusText = $"共 {_machines.Count} 台机台 · 实时数据 2s 刷新";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载看板数据失败：" + ex.Message);
                StatusText = "加载失败";
            }

            BuildTrendChart();
            RefreshRealtimeCharts();
            _timer.Start();
        }

        /// <summary>构建「近 7 日产量趋势」图表（示例数据，历史产量表落地后替换）。</summary>
        private void BuildTrendChart()
        {
            double[] daily = { 1200, 1350, 1100, 1580, 1420, 1680, 1530 };
            double[] target = { 1500, 1500, 1500, 1500, 1500, 1500, 1500 };

            TrendSeries.Clear();
            TrendSeries.Add(new ColumnSeries<double>
            {
                Name = "日产量",
                Values = daily,
                MaxBarWidth = 42,
                Fill = new SolidColorPaint(new SKColor(91, 141, 239)),
                DataLabelsPaint = new SolidColorPaint(new SKColor(96, 96, 96)),
                DataLabelsSize = 11
            });
            TrendSeries.Add(new LineSeries<double>
            {
                Name = "目标产量",
                Values = target,
                Stroke = new SolidColorPaint(new SKColor(229, 115, 115)) { StrokeThickness = 2 },
                GeometryFill = null,
                GeometryStroke = null,
                Fill = null,
                IsHoverable = false,
                DataLabelsPaint = null
            });
        }

        /// <summary>从 PLC 缓存刷新：KPI 指标 + 状态分布饼图 + 各机台今日产量条形图。</summary>
        private void RefreshRealtimeCharts()
        {
            if (_machines.Count == 0) return;

            var stateNames = new[] { "停机", "待机", "自动运转", "未知" };
            var stateColors = new[]
            {
                new SKColor(158, 158, 158),  // 停机 - 灰
                new SKColor(255, 167, 38),   // 待机 - 橙
                new SKColor(67, 160, 71),    // 运转 - 绿
                new SKColor(144, 164, 174)   // 未知（PLC 越界值）
            };
            var stateCount = new double[4];

            double todaySum = 0, totalSum = 0, powerSum = 0;
            var connectedMachines = 0;
            var machineNames = new List<string>();
            var machineToday = new List<double>();

            foreach (var machine in _machines)
            {
                var settingId = machine.CommSettingId;
                if (settingId <= 0 || !_plcService.IsConnected(settingId)) continue;

                var current = _plcObjectService.Read<MachinePlcEntity>(settingId);
                if (current == null) continue;

                connectedMachines++;

                // PLC 状态值可能越界（脏数据/未定义值），越界归入「未知」，避免数组越界
                var stateValue = ToInt(current, "MachineState");
                stateCount[stateValue is >= 0 and <= 2 ? stateValue : 3]++;

                var daily = ToFloat(current, "DailyProduction");
                var total = ToFloat(current, "TotalProduction");
                var power = ToFloat(current, "EffectivePower");

                todaySum += daily;
                totalSum += total;
                powerSum += power;

                machineNames.Add(string.IsNullOrWhiteSpace(machine.Name)
                    ? $"机台 #{machine.Id}"
                    : machine.Name);
                machineToday.Add(daily);
            }

            // KPI
            TodayProduction = todaySum.ToString("N0");
            TotalProduction = totalSum.ToString("N0");
            RunningCount = $"{(int)stateCount[2]} / {_machines.Count}";
            AvgEffectivePower = connectedMachines == 0
                ? "0 kW"
                : (powerSum / connectedMachines).ToString("F1") + " kW";

            // 状态分布饼图（真实数据；未连接的机台不计入）——原地更新 Values，避免重建 Series
            var pieCount = 0;
            for (var i = 0; i < stateCount.Length; i++)
            {
                if (stateCount[i] <= 0) continue;

                if (pieCount < _statePieItems.Count)
                {
                    var pie = _statePieItems[pieCount];
                    pie.Values = new[] { stateCount[i] };
                    pie.Name = stateNames[i];
                    pie.Fill = new SolidColorPaint(stateColors[i]);
                }
                else
                {
                    _statePieItems.Add(new PieSeries<double>
                    {
                        Name = stateNames[i],
                        Values = new[] { stateCount[i] },
                        Fill = new SolidColorPaint(stateColors[i]),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 13,
                        DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}"
                    });
                    StatePieSeries.Add(_statePieItems[^1]);
                }
                pieCount++;
            }

            // 多余的分片（状态数变少时）从集合移除，避免残留
            for (var i = StatePieSeries.Count - 1; i >= pieCount; i--)
                StatePieSeries.RemoveAt(i);
            if (_statePieItems.Count > pieCount)
                _statePieItems.RemoveRange(pieCount, _statePieItems.Count - pieCount);

            // 各机台今日产量条形图（真实数据）——原地更新 Values，避免重建 Series
            if (machineToday.Count > 0)
            {
                MachineYAxes[0].Labels = machineNames.ToArray();

                if (_machineProductionItems.Count == 0)
                {
                    _machineProductionItems.Add(new RowSeries<double>
                    {
                        Name = "今日产量",
                        MaxBarWidth = 24,
                        Fill = new SolidColorPaint(new SKColor(38, 166, 154)),
                        DataLabelsPaint = new SolidColorPaint(new SKColor(96, 96, 96)),
                        DataLabelsSize = 11,
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End
                    });
                    MachineProductionSeries.Add(_machineProductionItems[0]);
                }

                _machineProductionItems[0].Values = machineToday.ToArray();
            }
            else
            {
                // 没有机台数据时清空，避免残留旧柱
                if (_machineProductionItems.Count > 0)
                    _machineProductionItems[0].Values = Array.Empty<double>();
            }
        }

        // ---------- 缓存文本 → 强类型（与首页同一套宽松转换） ----------

        private static float ToFloat(IReadOnlyDictionary<string, string> dict, string key)
        {
            if (!dict.TryGetValue(key, out var v)) return 0f;
            var converted = PlcValueConverter.Convert(v, typeof(float));
            return converted is float f ? f : 0f;
        }

        private static int ToInt(IReadOnlyDictionary<string, string> dict, string key)
        {
            if (!dict.TryGetValue(key, out var v)) return -1;
            var converted = PlcValueConverter.Convert(v, typeof(int));
            return converted is int i ? i : -1;
        }
    }
}
