using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using mcpx_template_project.Converters.Plc;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Domain.VO.Machine;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.CommSetting;
using mcpx_template_project.Service.Business.Machine;
using mcpx_template_project.Service.Business.Plc;
using mcpx_template_project.Service.Business.Secs;
using MachinePlcEntity = mcpx_template_project.Domain.Entities.Business.Machine.MachinePlc;
using SecsInstanceEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsInstance;

namespace mcpx_template_project.ViewModels.Business
{
    /// <summary>
    /// 首页视图模型：展示所有机台的实时状态卡片。
    /// 设计要点：
    ///   1. 仅从通讯缓存读取机台 PLC 值（通过 <see cref="IPlcObjectService.Read{T}"/>），
    ///      绝不主动向 PLC 发请求——PLC 扫描由后台 <c>MachinePlcPollService</c> 统一完成；
    ///   2. DispatcherTimer 周期刷新，把缓存文本值转换为强类型后更新卡片；
    ///   3. 多机台：遍历 machine 表，每台一张卡片。
    /// </summary>
    public partial class PageHomeViewModel : ObservableObject
    {
        private readonly IMachineService _machineService;
        private readonly IPlcObjectService _plcObjectService;
        private readonly IPlcCommunicationService _plcService;
        private readonly ICommSettingService _commSettingService;
        private readonly ISecsInstanceService _secsInstanceService;
        private readonly ISecsGemManager _secsGemManager;
        private readonly ISecsStateService _secsStateService;
        private readonly IDialogService _dialogService;
        private readonly DispatcherTimer _timer;

        /// <summary>机台卡片集合（绑定首页 UI）</summary>
        public ObservableCollection<MachineCardVO> Machines { get; } = new();

        /// <summary>首页状态栏文本</summary>
        [ObservableProperty]
        private string _statusText = "正在加载机台...";

        public PageHomeViewModel(
            IMachineService machineService,
            IPlcObjectService plcObjectService,
            IPlcCommunicationService plcService,
            ICommSettingService commSettingService,
            ISecsInstanceService secsInstanceService,
            ISecsGemManager secsGemManager,
            ISecsStateService secsStateService,
            IDialogService dialogService)
        {
            _machineService = machineService;
            _plcObjectService = plcObjectService;
            _plcService = plcService;
            _commSettingService = commSettingService;
            _secsInstanceService = secsInstanceService;
            _secsGemManager = secsGemManager;
            _secsStateService = secsStateService;
            _dialogService = dialogService;

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (_, _) => RefreshFromCache();

            _ = LoadMachinesAsync();
        }

        /// <summary>加载机台列表（初始化卡片），并启动周期刷新。</summary>
        private async Task LoadMachinesAsync()
        {
            try
            {
                var machines = await _machineService.QueryAllAsync();
                var settings = await LoadSettingsMapAsync();

                Machines.Clear();
                foreach (var m in machines)
                {
                    settings.TryGetValue(m.CommSettingId, out var settingName);

                    var secs = _secsBySettingMap.TryGetValue(m.CommSettingId, out var s) ? s : null;

                    Machines.Add(new MachineCardVO
                    {
                        Id = m.Id,
                        Name = string.IsNullOrWhiteSpace(m.Name) ? $"机台 #{m.Id}" : m.Name,
                        CommSettingName = settingName ?? $"通讯设定 #{m.CommSettingId}",
                        HasSecsInstance = secs != null,
                        SecsInstanceName = secs?.Name ?? string.Empty
                    });
                }

                StatusText = Machines.Count == 0
                    ? "暂无机台，请在数据库中配置 machine 表"
                    : $"共 {Machines.Count} 台机台 · 正在监控";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载机台失败：" + ex.Message);
                StatusText = "加载机台失败";
            }

            // 无论是否有机台，都启动周期刷新（新增机台后会自动出现）
            _timer.Start();
        }

        /// <summary>从缓存刷新所有机台卡片的 PLC 值（不主动读 PLC）。</summary>
        private void RefreshFromCache()
        {
            if (Machines.Count == 0) return;

            foreach (var card in Machines)
            {
                var settingId = ResolveSettingId(card);
                if (settingId <= 0)
                {
                    card.IsConnected = false;
                    continue;
                }

                card.IsConnected = _plcService.IsConnected(settingId);

                // 刷新 SECS/GEM 连接状态（独立于 PLC 连接，只查通讯引擎内存态）
                RefreshSecsState(card);

                if (!card.IsConnected) continue;

                // 从缓存读回「属性名 → 文本值」原始字典
                var current = _plcObjectService.Read<MachinePlcEntity>(settingId);

                // 转换为强类型并写入卡片
                card.MachineState = ToEnum<MachineState>(current, "MachineState");
                card.ObjectId = GetText(current, "Obj   ectId");
                card.LotId = GetText(current, "LotId");
                card.CurrentBatchCount = ToInt(current, "CurrentBatchCount");
                card.TargetBatchCount = ToInt(current, "TargetBatchCount");
                card.BlueLamp = ToEnum<BlueLampState>(current, "BlueLamp");
                card.Buzzer = ToEnum<BuzzerState>(current, "Buzzer");
                card.EffectivePower = ToFloat(current, "EffectivePower");
                card.TotalProduction = ToFloat(current, "TotalProduction");
                card.DailyProduction = ToFloat(current, "DailyProduction");
                card.CutterUseCount = ToInt(current, "CutterUseCount");
                card.UpperRollerTimeStage1 = ToInt(current, "UpperRollerTimeStage1");
                card.LowerRollerTimeStage1 = ToInt(current, "LowerRollerTimeStage1");
                card.UpperRollerTimeStage2 = ToInt(current, "UpperRollerTimeStage2");
                card.LowerRollerTimeStage2 = ToInt(current, "LowerRollerTimeStage2");
                card.FilmBarUseCount = ToInt(current, "FilmBarUseCount");

                card.LastRefresh = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        /// <summary>根据卡片反查 comm_setting_id（机台表主键即 machine.id，comm_setting_id 由后台联表维护）。</summary>
        private long ResolveSettingId(MachineCardVO card)
        {
            // 卡片 Id 是 machine.id；comm_setting_id 需从机台表查。
            // 为省一次查询，缓存机台 id → comm_setting_id 映射。
            if (_machineSettingMap.TryGetValue(card.Id, out var sid))
                return sid;
            return 0;
        }

        private readonly Dictionary<long, long> _machineSettingMap = new();

        /// <summary>comm_setting_id → 关联的 secs_instance（用于机台卡片显示 SECS/GEM 连接状态）</summary>
        private readonly Dictionary<long, SecsInstanceEntity> _secsBySettingMap = new();

        /// <summary>刷新卡片的 SECS/GEM 连接状态（从通讯引擎内存态查询，不落库）。</summary>
        private void RefreshSecsState(MachineCardVO card)
        {
            if (!card.HasSecsInstance)
            {
                card.SecsCommunicating = false;
                card.SecsStateText = "无 SECS 实例";
                return;
            }

            // 通过 comm_setting_id 反查 secs_instance，再按实例 ID 查询引擎连接状态
            if (!_machineSettingMap.TryGetValue(card.Id, out var settingId))
            {
                card.SecsCommunicating = false;
                card.SecsStateText = "未关联通讯";
                return;
            }

            if (!_secsBySettingMap.TryGetValue(settingId, out var secs))
            {
                card.SecsCommunicating = false;
                card.SecsStateText = "无 SECS 实例";
                return;
            }

            var deviceKey = secs.Id.ToString();
            if (_secsGemManager.IsCommunicating(deviceKey))
            {
                card.SecsCommunicating = true;
                card.SecsStateText = "已通信";
            }
            else if (_secsGemManager.IsRunning(deviceKey))
            {
                card.SecsCommunicating = false;
                card.SecsStateText = "监听中";
            }
            else
            {
                card.SecsCommunicating = false;
                card.SecsStateText = "未启动";
            }

            // 控制状态（GEM Control State）：由状态机服务按实例 ID 查询内存态
            card.SecsControlState = _secsStateService.GetControlState(secs.Id);
        }

        /// <summary>加载通讯设定名称映射（id → name）与机台映射（machine.id → comm_setting_id）。</summary>
        private async Task<Dictionary<long, string>> LoadSettingsMapAsync()
        {
            var nameMap = new Dictionary<long, string>();

            try
            {
                var settings = await _commSettingService.QueryAllAsync();
                foreach (var s in settings)
                    nameMap[s.Id] = s.Name;
            }
            catch
            {
                // 忽略，不阻断机台列表展示
            }

            try
            {
                _machineSettingMap.Clear();
                var machines = await _machineService.QueryAllAsync();
                foreach (var m in machines)
                    _machineSettingMap[m.Id] = m.CommSettingId;
            }
            catch
            {
                // 忽略
            }

            try
            {
                _secsBySettingMap.Clear();
                var secsInstances = await _secsInstanceService.QueryAllAsync();
                foreach (var s in secsInstances)
                {
                    if (s.CommSettingId.HasValue)
                        _secsBySettingMap[s.CommSettingId.Value] = s;
                }
            }
            catch
            {
                // 忽略，不阻断机台列表展示
            }

            return nameMap;
        }

        // ---------- 文本 → 强类型转换（宽松，失败返回默认值） ----------

        private static string GetText(IReadOnlyDictionary<string, string> dict, string key)
            => dict.TryGetValue(key, out var v) ? v : string.Empty;

        private static int ToInt(IReadOnlyDictionary<string, string> dict, string key)
        {
            if (!dict.TryGetValue(key, out var v)) return 0;
            var converted = PlcValueConverter.Convert(v, typeof(int));
            return converted is int i ? i : 0;
        }

        private static float ToFloat(IReadOnlyDictionary<string, string> dict, string key)
        {
            if (!dict.TryGetValue(key, out var v)) return 0f;
            var converted = PlcValueConverter.Convert(v, typeof(float));
            return converted is float f ? f : 0f;
        }

        private static T ToEnum<T>(IReadOnlyDictionary<string, string> dict, string key) where T : struct, Enum
        {
            if (!dict.TryGetValue(key, out var v)) return default;
            var converted = PlcValueConverter.Convert(v, typeof(T));
            return converted is T e ? e : default;
        }
    }
}
