using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Core.Plc;
using mcpx_template_project.Domain.Entities.Business.Recipe;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.CommSetting;
using mcpx_template_project.Service.Business.Plc;
using mcpx_template_project.Service.Business.Recipe;
using RecipeEntity = mcpx_template_project.Domain.Entities.Business.Recipe.Recipe;

namespace mcpx_template_project.ViewModels.Business.EquipmentInfo.PNSelect
{
    /// <summary>
    /// PNSelect（料号/配方选择）页面视图模型。
    /// 新增/编辑/删除只操作数据库（通过 <see cref="RecipeEditViewModel"/> 弹窗），
    /// 仅「写入配方」触发 PLC 写入（通过通用 <see cref="IPlcObjectService"/>）；
    /// 右侧实时读回 PLC 实际值。
    /// </summary>
    /// <author>Rudeus</author>
    public partial class PagePNSelectViewModel : ObservableObject
    {
        private readonly IRecipeService _recipeService;
        private readonly IPlcObjectService _plcObjectService;
        private readonly IPlcCommunicationService _plcService;
        private readonly ICommSettingService _commSettingService;
        private readonly IDialogService _dialogService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly DispatcherTimer _timer;

        /// <summary>配方属性访问器（取属性值构造读回目标值）</summary>
        private readonly PlcPointAccessor<RecipeEntity> _accessor = new();

        // ============ 列表 ============

        public ObservableCollection<RecipeEntity> Recipes { get; } = new();

        [ObservableProperty] private string _keyword = string.Empty;

        [ObservableProperty] private string _summary = string.Empty;

        [ObservableProperty] private RecipeEntity? _selectedRecipe;

        // ============ 写入 PLC ============

        public ObservableCollection<CommSettingVO> ConnectedSettings { get; } = new();

        [ObservableProperty] private CommSettingVO? _selectedSetting;

        [ObservableProperty] private string _writeStatus = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanWrite))]
        private bool _isWriting;

        /// <summary>是否可执行写入（IsWriting 取反）</summary>
        public bool CanWrite => !IsWriting && SelectedRecipe != null && SelectedSetting != null;

        /// <summary>写入结果三态：null=未写入，true=成功，false=失败（驱动圆点颜色）</summary>
        [ObservableProperty] private bool? _writeSuccessFlag;

        /// <summary>写入结果文字（未写入 / 配方「XXX」写入成功 / 写入失败）</summary>
        [ObservableProperty] private string _writeSuccessText = "未写入配方";

        // ============ 实时读回 ============

        public ObservableCollection<RecipeFieldReadback> Readbacks { get; } = new();

        [ObservableProperty] private bool _isMonitoring;

        [ObservableProperty] private string _readbackStatus = "尚未写入配方";

        public PagePNSelectViewModel(
            IRecipeService recipeService,
            IPlcObjectService plcObjectService,
            IPlcCommunicationService plcService,
            ICommSettingService commSettingService,
            IDialogService dialogService,
            IViewModelFactory viewModelFactory)
        {
            _recipeService = recipeService;
            _plcObjectService = plcObjectService;
            _plcService = plcService;
            _commSettingService = commSettingService;
            _dialogService = dialogService;
            _viewModelFactory = viewModelFactory;

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;

            _ = LoadAsync();
        }

        // ============ 列表加载 ============

        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                var list = await _recipeService.QueryAllAsync(Keyword);
                Recipes.Clear();
                foreach (var item in list)
                    Recipes.Add(item);
                Summary = $"共 {list.Count} 条配方";

                await LoadConnectedSettingsAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载配方失败：" + ex.Message);
            }
        }

        /// <summary>加载已连接的通讯设定列表（写入目标 PLC）</summary>
        private async Task LoadConnectedSettingsAsync()
        {
            try
            {
                var connectedIds = _plcService.GetConnectedSettingIds().ToHashSet();
                var all = await _commSettingService.QueryAllAsync();

                ConnectedSettings.Clear();
                foreach (var s in all.Where(s => connectedIds.Contains(s.Id)))
                    ConnectedSettings.Add(s);

                if (SelectedSetting == null || !connectedIds.Contains(SelectedSetting.Id))
                    SelectedSetting = ConnectedSettings.FirstOrDefault();
            }
            catch
            {
                // 忽略通讯设定加载失败，不阻断配方列表展示
            }
        }

        // ============ CRUD（仅数据库，通过弹窗） ============

        /// <summary>选中配方时，刷新读回列表（目标值取自该配方）</summary>
        partial void OnSelectedRecipeChanged(RecipeEntity? value)
        {
            if (value != null)
            {
                BuildReadbacks(value);
                if (IsMonitoring)
                    _timer.Start();
            }
            OnPropertyChanged(nameof(CanWrite));
        }

        /// <summary>选中目标 PLC 时刷 新写入按钮可用性</summary>
        partial void OnSelectedSettingChanged(CommSettingVO? value)
        {
            OnPropertyChanged(nameof(CanWrite));
        }

        /// <summary>打开新增配方弹窗</summary>
        [RelayCommand]
        private void AddRecipe()
        {
            var editVm = _viewModelFactory.Create<RecipeEditViewModel>();
            OpenEditWindow(editVm);
        }

        /// <summary>打开编辑配方弹窗</summary>
        [RelayCommand]
        private void EditRecipe(RecipeEntity? recipe)
        {
            if (recipe == null) return;
            var editVm = _viewModelFactory.Create<RecipeEditViewModel>(recipe);
            OpenEditWindow(editVm);
        }

        private async void OpenEditWindow(RecipeEditViewModel editVm)
        {
            var window = new Views.Pages.Business.EquipmentInfo.PNSelect.RecipeEditWindow(editVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                var saved = await editVm.SaveAsync();
                if (saved != null)
                {
                    await LoadAsync();
                    SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == saved.Id);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteRecipe(RecipeEntity? recipe)
        {
            if (recipe == null) return;

            try
            {
                if (!_dialogService.Confirm($"确定删除配方 \"{recipe.RecipeName}\" 吗？", "确认删除"))
                    return;

                await _recipeService.DeleteAsync(recipe.Id);
                _dialogService.ShowSuccess("删除成功");

                if (SelectedRecipe?.Id == recipe.Id)
                    SelectedRecipe = null;

                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除失败：" + ex.Message);
            }
        }

        // ============ 写入 PLC ============

        [RelayCommand]
        private async Task WriteToPlcAsync()
        {
            if (SelectedRecipe == null)
            {
                _dialogService.ShowWarning("请先在左侧选择一个配方");
                return;
            }

            if (SelectedSetting == null)
            {
                _dialogService.ShowWarning("请先在「通讯设定」中启动连线，并选择一个已连接的目标 PLC");
                return;
            }

            var recipe = SelectedRecipe;

            IsWriting = true;
            WriteStatus = $"正在写入配方 \"{recipe.RecipeName}\" 到 {SelectedSetting.Name} ...";

            try
            {
                var result = await _plcObjectService.WriteAsync<RecipeEntity>(SelectedSetting.Id, recipe);
                WriteStatus = result.Message;
                WriteSuccessFlag = result.Success;
                // 成功文案使用左侧列表当前选中的配方名（而非本次局部变量）
                var name = SelectedRecipe?.RecipeName ?? recipe.RecipeName;
                WriteSuccessText = result.Success
                    ? $"配方「{name}」写入成功"
                    : $"配方「{name}」写入失败";

                if (result.Success)
                    _dialogService.ShowSuccess(result.Message);
                else
                    _dialogService.ShowError(result.Message);

                // 生成读回列表（目标值 = 本次写入的配方值），并启动实时刷新
                BuildReadbacks(recipe);
                StartMonitor();
            }
            catch (Exception ex)
            {
                WriteStatus = "写入异常：" + ex.Message;
                WriteSuccessFlag = false;
                var name = SelectedRecipe?.RecipeName ?? recipe.RecipeName;
                WriteSuccessText = $"配方「{name}」写入失败";
                _dialogService.ShowError("写入失败：" + ex.Message);
            }
            finally
            {
                IsWriting = false;
            }
        }

        [RelayCommand]
        private void RefreshConnections() => _ = LoadConnectedSettingsAsync();

        [RelayCommand]
        private void StartMonitor()
        {
            if (SelectedSetting == null) return;
            IsMonitoring = true;
            _timer.Start();
        }

        [RelayCommand]
        private void StopMonitor()
        {
            IsMonitoring = false;
            _timer.Stop();
        }

        // ============ 读回刷新 ============

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (SelectedSetting == null || Readbacks.Count == 0)
                return;

            // 从底层读回「属性名 → 当前值」原始字典
            var current = _plcObjectService.Read<RecipeEntity>(SelectedSetting.Id);

            foreach (var rb in Readbacks)
            {
                rb.CurrentValue = current.TryGetValue(rb.PropertyName, out var v) ? v : string.Empty;
                rb.Matched = !string.IsNullOrEmpty(rb.TargetValue)
                    && string.Equals(Normalize(rb.CurrentValue), Normalize(rb.TargetValue), StringComparison.OrdinalIgnoreCase);
            }

            // 注意：写入成功状态只由 WriteToPlcAsync 的结果一次性决定，
            // 不再用回复位（瞬态握手信号，握手结束即复归 0）反复刷新，避免「成功」被刷成「失败」。

            var matched = Readbacks.Count(r => r.Matched);
            ReadbackStatus = $"[{DateTime.Now:HH:mm:ss}] 读回 {Readbacks.Count} 点 · {matched} 点已匹配";
        }

        private void BuildReadbacks(RecipeEntity recipe)
        {
            Readbacks.Clear();
            foreach (var read in _accessor.Plan.Reads)
            {
                Readbacks.Add(new RecipeFieldReadback
                {
                    PropertyName = read.PropertyName,
                    DisplayName = read.DisplayName ?? read.PropertyName,
                    TargetValue = FormatTarget(_accessor.GetValue(recipe, read.PropertyName))
                });
            }
            ReadbackStatus = "读回列表已就绪（目标值取自当前配方）";
        }

        /// <summary>格式化目标值（用于显示与匹配比较）</summary>
        private static string FormatTarget(object? value)
        {
            if (value is double d)
                return d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            if (value is int i)
                return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is Enum en)
                return System.Convert.ToInt32(en).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>归一化数值文本，便于目标值与读回值比较</summary>
        private static string Normalize(string text)
        {
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            return text.Trim();
        }
    }
}
