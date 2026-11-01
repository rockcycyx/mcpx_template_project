using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.Business.CommSetting;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.CommSetting;
using mcpx_template_project.Service.Business.Plc;

namespace mcpx_template_project.ViewModels.Business
{
    /// <summary>
    /// 单身区域页签：AI / AO / DI / DO 各一个，承载该区域类型的明细行。
    /// </summary>
    public class AreaTabViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string AreaType { get; set; } = string.Empty;
        public ObservableCollection<CommSettingAreaVO> Areas { get; } = new();
    }

    /// <summary>
    /// 通讯设定列表视图模型（主从布局：列表 + 选中项的区域配置内联编辑，生命周期不随窗口关闭而消亡）
    /// </summary>
    public partial class CommSettingListViewModel : ObservableObject
    {
        private readonly ICommSettingService _service;
        private readonly IDialogService _dialogService;
        private readonly IPlcCommunicationService _plcService;

        public ObservableCollection<CommSettingVO> Settings { get; } = new();

        /// <summary>名称/IP 搜索关键字</summary>
        [ObservableProperty]
        private string _keyword = string.Empty;

        /// <summary>列表底部统计文案</summary>
        [ObservableProperty]
        private string _summary = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        /// <summary>当前选中的通讯设定（驱动下方区域明细面板）</summary>
        [ObservableProperty]
        private CommSettingVO? _selectedSetting;

        /// <summary>McpX 设备前缀选项（常用 D/W/B/M/X/Y/F 靠前）</summary>
        public IReadOnlyList<string> PrefixOptions { get; } = BuildPrefixOptions();

        /// <summary>选中项的区域配置页签（AI/AO/DI/DO）</summary>
        public List<AreaTabViewModel> AreaTabs { get; } = new()
        {
            new AreaTabViewModel { Title = "AI", AreaType = "AI" },
            new AreaTabViewModel { Title = "AO", AreaType = "AO" },
            new AreaTabViewModel { Title = "DI", AreaType = "DI" },
            new AreaTabViewModel { Title = "DO", AreaType = "DO" },
        };

        /// <summary>请求打开编辑窗口（参数为 null 表示新增，否则为编辑单头）</summary>
        public event Action<CommSettingVO?>? RequestEdit;

        public CommSettingListViewModel(ICommSettingService service, IDialogService dialogService, IPlcCommunicationService plcService)
        {
            _service = service;
            _dialogService = dialogService;
            _plcService = plcService;
            _ = LoadAsync();
        }

        partial void OnSelectedSettingChanged(CommSettingVO? value) => RebuildAreaTabs();

        private void RebuildAreaTabs()
        {
            foreach (var tab in AreaTabs)
                tab.Areas.Clear();

            if (SelectedSetting == null) return;

            foreach (var a in SelectedSetting.Areas)
            {
                var tab = AreaTabs.FirstOrDefault(t => t.AreaType == a.AreaType);
                tab?.Areas.Add(a);
            }
        }

        [RelayCommand]
        public async Task LoadAsync(long? selectId = null)
        {
            try
            {
                IsLoading = true;
                long? keepId = selectId ?? SelectedSetting?.Id;

                var list = await _service.QueryAllAsync(Keyword);
                Settings.Clear();
                foreach (var item in list)
                {
                    // 连线状态为纯内存态：从通讯引擎实时回填
                    item.Status = _plcService.IsConnected(item.Id) ? "1" : "0";
                    Settings.Add(item);
                }
                Summary = $"共 {list.Count} 条通讯设定";

                SelectedSetting = keepId.HasValue
                    ? Settings.FirstOrDefault(s => s.Id == keepId.Value)
                    : Settings.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载通讯设定失败：" + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddSetting() => RequestEdit?.Invoke(null);

        [RelayCommand]
        private void EditSetting(CommSettingVO? item)
        {
            if (item != null) RequestEdit?.Invoke(item);
        }

        [RelayCommand]
        private async Task DeleteSetting(CommSettingVO? item)
        {
            if (item == null) return;
            try
            {
                if (!_dialogService.Confirm($"确定删除通讯设定 \"{item.Name}\" 吗？", "确认删除"))
                    return;

                await _service.DeleteAsync(item.Id);
                _dialogService.ShowSuccess("删除成功");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task ToggleRun(CommSettingVO? item)
        {
            if (item == null) return;

            // 连线状态为纯内存态（由通讯引擎维护），不再写数据库 Status 字段。
            var isStart = !_plcService.IsConnected(item.Id);

            try
            {
                if (isStart)
                {
                    // 启动连线：通过通讯引擎建立与三菱Q系列PLC的连接（含区域点位批量读取初始化）
                    var ok = await _plcService.ConnectAsync(item);
                    if (!ok)
                    {
                        _dialogService.ShowError($"PLC连接失败：\n\n{_plcService.LastError}");
                        return;
                    }
                    item.Status = "1";
                    _dialogService.ShowSuccess($"通讯设定 \"{item.Name}\" 连接成功");
                }
                else
                {
                    // 停止连线：断开PLC连接（释放TCP资源），暂存数据标记为离线
                    _plcService.Disconnect(item.Id);
                    item.Status = "0";
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("切换连线状态失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private void AddArea(AreaTabViewModel? tab)
        {
            if (tab == null || SelectedSetting == null) return;
            tab.Areas.Add(new CommSettingAreaVO
            {
                AreaType = tab.AreaType,
                Quantity = 1,
                Sort = tab.Areas.Count + 1
            });
        }

        [RelayCommand]
        private void RemoveArea(CommSettingAreaVO? area)
        {
            if (area == null) return;
            foreach (var tab in AreaTabs)
            {
                if (tab.Areas.Remove(area)) break;
            }
        }

        [RelayCommand]
        private async Task SaveAreas()
        {
            if (SelectedSetting == null) return;

            try
            {
                var areas = new List<CommSettingArea>();
                foreach (var tab in AreaTabs)
                {
                    foreach (var a in tab.Areas)
                    {
                        areas.Add(new CommSettingArea
                        {
                            Id = a.Id,
                            SettingId = a.SettingId,
                            AreaType = tab.AreaType,
                            Name = a.Name,
                            Position = a.Position,
                            Quantity = a.Quantity,
                            Sort = a.Sort
                        });
                    }
                }

                await _service.SaveAreasAsync(SelectedSetting.Id, areas);
                _dialogService.ShowSuccess("保存区域配置成功");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("保存区域配置失败：" + ex.Message);
            }
        }

        private static IReadOnlyList<string> BuildPrefixOptions()
        {
            var common = new[] { "D", "W", "B", "M", "X", "Y", "F" };
            var all = Enum.GetNames(typeof(McpXLib.Enums.Prefix));
            return common.Concat(all.Where(x => !common.Contains(x))).ToList();
        }
    }
}
