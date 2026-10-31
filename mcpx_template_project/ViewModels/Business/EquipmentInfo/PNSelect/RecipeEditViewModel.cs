using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Recipe;
using RecipeEntity = mcpx_template_project.Domain.Entities.Business.Recipe.Recipe;

namespace mcpx_template_project.ViewModels.Business.EquipmentInfo.PNSelect
{
    /// <summary>膜宽枚举选项（值 + 中文显示名）</summary>
    public record MembraneWidthOption(MembraneWidthRange Value, string Display);

    /// <summary>
    /// 配方编辑视图模型（仅数据库新增/编辑，不涉及 PLC）。
    /// 业务字段通过 RequestClose 事件把结果回传给宿主窗口。
    /// </summary>
    public partial class RecipeEditViewModel : ObservableObject
    {
        private readonly IRecipeService _service;
        private readonly IDialogService _dialogService;

        // ============ 表单字段（16 个） ============

        [ObservableProperty] private string _recipeName = string.Empty;

        [ObservableProperty] private int? _upperLamiTmpStage1;

        [ObservableProperty] private int? _lowerLamiTmpStage1;

        [ObservableProperty] private int? _upperLamiTmpStage2;

        [ObservableProperty] private int? _lowerLamiTmpStage2;

        [ObservableProperty] private MembraneWidthRange _membraneWidth;

        [ObservableProperty] private int? _panelOutTmp;

        [ObservableProperty] private int? _upperTackingTmp;

        [ObservableProperty] private int? _lowerTackingTmp;

        [ObservableProperty] private double? _drvSpeed;

        [ObservableProperty] private double? _upperTackingPressure;

        [ObservableProperty] private double? _frontCuPos;

        [ObservableProperty] private double? _rearCuPos;

        [ObservableProperty] private double? _rollerPressureStage1;

        [ObservableProperty] private double? _rollerPressureStage2;

        [ObservableProperty] private double? _durationFilm;

        [ObservableProperty] private double? _plateThickness;

        [ObservableProperty] private string _errorMessage = string.Empty;

        public bool IsEditMode { get; }

        public RecipeEntity? Editing { get; }

        public event Action<bool>? RequestClose;

        /// <summary>膜宽枚举选项</summary>
        public IReadOnlyList<MembraneWidthOption> MembraneWidthOptions { get; } = new[]
        {
            new MembraneWidthOption(MembraneWidthRange.Below06mm, "< 0.6mm"),
            new MembraneWidthOption(MembraneWidthRange.Below24mil, "< 24mil"),
            new MembraneWidthOption(MembraneWidthRange.AtLeast06mm, ">= 0.6mm"),
            new MembraneWidthOption(MembraneWidthRange.AtLeast24mil, ">= 24mil"),
        };

        public RecipeEditViewModel(IRecipeService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            IsEditMode = false;
        }

        public RecipeEditViewModel(RecipeEntity editing, IRecipeService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            IsEditMode = true;
            Editing = editing;

            RecipeName = editing.RecipeName;
            UpperLamiTmpStage1 = editing.UpperLamiTmp_Stage1;
            LowerLamiTmpStage1 = editing.LowerLamiTmp_Stage1;
            UpperLamiTmpStage2 = editing.UpperLamiTmp_Stage2;
            LowerLamiTmpStage2 = editing.LowerLamiTmp_Stage2;
            MembraneWidth = editing.MembraneWidth;
            PanelOutTmp = editing.PanelOutTmp;
            UpperTackingTmp = editing.UpperTackingTmp;
            LowerTackingTmp = editing.LowerTackingTmp;
            DrvSpeed = editing.DrvSpeed;
            UpperTackingPressure = editing.UpperTackingPressure;
            FrontCuPos = editing.FrontCuPos;
            RearCuPos = editing.RearCuPos;
            RollerPressureStage1 = editing.RollerPressure_Stage1;
            RollerPressureStage2 = editing.RollerPressure_Stage2;
            DurationFilm = editing.DurationFilm;
            PlateThickness = editing.PlateThickness;
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(RecipeName))
            {
                ErrorMessage = "配方名称不能为空";
                return;
            }

            if (!Validate(out var message))
            {
                ErrorMessage = message;
                return;
            }

            RequestClose?.Invoke(true);
        }

        /// <summary>校验所有数值字段是否落在合法范围内（与数据库 CHECK 约束保持一致）。</summary>
        private bool Validate(out string message)
        {
            if (!InRange(UpperLamiTmpStage1, 15, 130, "一段上热压轮温度", out message)) return false;
            if (!InRange(LowerLamiTmpStage1, 15, 130, "一段下热压轮温度", out message)) return false;
            if (!InRange(UpperLamiTmpStage2, 15, 130, "二段上热压轮温度", out message)) return false;
            if (!InRange(LowerLamiTmpStage2, 15, 130, "二段下热压轮温度", out message)) return false;
            if (!InRange(PanelOutTmp, 15, 100, "出板温度", out message)) return false;
            if (!InRange(UpperTackingTmp, 15, 100, "上贴膜温度", out message)) return false;
            if (!InRange(LowerTackingTmp, 15, 100, "下贴膜温度", out message)) return false;
            if (!InRange(DrvSpeed, 1, 5, "输送速度", out message)) return false;
            if (!InRange(UpperTackingPressure, 0, 5, "上贴膜压力", out message)) return false;
            if (!InRange(FrontCuPos, 0, 49.9, "前留铜", out message)) return false;
            if (!InRange(RearCuPos, 0, 12.9, "后留铜", out message)) return false;
            if (!InRange(RollerPressureStage1, 0.3, 5.0, "一段压轮压力", out message)) return false;
            if (!InRange(RollerPressureStage2, 0.3, 5.0, "二段压轮压力", out message)) return false;
            if (!InRange(DurationFilm, 0.3, 7, "贴膜时间", out message)) return false;
            if (!InRange(PlateThickness, 0.5, 8, "板厚", out message)) return false;

            message = string.Empty;
            return true;
        }

        private static bool InRange(int? value, int min, int max, string field, out string message)
        {
            if (value is null)
            {
                message = $"{field}不能为空";
                return false;
            }
            if (value >= min && value <= max)
            {
                message = string.Empty;
                return true;
            }
            message = $"{field}需在 {min}~{max} 之间";
            return false;
        }

        private static bool InRange(double? value, double min, double max, string field, out string message)
        {
            if (value is null)
            {
                message = $"{field}不能为空";
                return false;
            }
            if (value >= min && value <= max)
            {
                message = string.Empty;
                return true;
            }
            message = $"{field}需在 {min}~{max} 之间";
            return false;
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);

        public RecipeEntity ToEntity()
        {
            return new RecipeEntity
            {
                Id = IsEditMode && Editing != null ? Editing.Id : 0,
                RecipeName = RecipeName?.Trim() ?? string.Empty,
                UpperLamiTmp_Stage1 = UpperLamiTmpStage1 ?? 0,
                LowerLamiTmp_Stage1 = LowerLamiTmpStage1 ?? 0,
                UpperLamiTmp_Stage2 = UpperLamiTmpStage2 ?? 0,
                LowerLamiTmp_Stage2 = LowerLamiTmpStage2 ?? 0,
                MembraneWidth = MembraneWidth,
                PanelOutTmp = PanelOutTmp ?? 0,
                UpperTackingTmp = UpperTackingTmp ?? 0,
                LowerTackingTmp = LowerTackingTmp ?? 0,
                DrvSpeed = DrvSpeed ?? 0,
                UpperTackingPressure = UpperTackingPressure ?? 0,
                FrontCuPos = FrontCuPos ?? 0,
                RearCuPos = RearCuPos ?? 0,
                RollerPressure_Stage1 = RollerPressureStage1 ?? 0,
                RollerPressure_Stage2 = RollerPressureStage2 ?? 0,
                DurationFilm = DurationFilm ?? 0,
                PlateThickness = PlateThickness ?? 0
            };
        }

        public async Task<RecipeEntity?> SaveAsync()
        {
            try
            {
                var entity = ToEntity();

                var saved = IsEditMode
                    ? await _service.UpdateAsync(entity)
                    : await _service.InsertAsync(entity);

                _dialogService.ShowSuccess(IsEditMode ? "修改配方成功" : "新增配方成功");
                return saved;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError((IsEditMode ? "修改" : "新增") + "配方失败：" + ex.Message);
                return null;
            }
        }
    }
}
