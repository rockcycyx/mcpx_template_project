using System.Globalization;
using Secs4Net;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Service.Business.Recipe;
using RecipeEntity = mcpx_template_project.Domain.Entities.Business.Recipe.Recipe;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Stream 7（工艺程序管理）业务服务实现。
    /// PPID ↔ 配方名（recipe.recipe_name）一一对应。
    /// S7 工艺程序的 16 个参数直接映射 recipe 的配方参数字段，不额外存储。
    /// 参数顺序固定（见 <see cref="ParamOrder"/>），与 recipe 表列顺序一致，客户文档约定。
    /// </summary>
    public sealed class S7ProcessProgramService : IS7ProcessProgramService
    {
        private readonly IRecipeService _recipeService;

        public S7ProcessProgramService(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        /// <summary>
        /// 16 个配方参数的固定顺序（与 recipe 表列声明顺序一致）：
        /// 一段上/下热压轮温度、二段上/下热压轮温度、膜宽、出板温度、上/下贴膜温度、
        /// 输送速度、上贴膜压力、前/后留铜、一/二段压轮压力、贴膜时间、板厚。
        /// 前 8 个为整数（int），后 8 个为浮点（double）。
        /// </summary>
        private static readonly string[] ParamOrder =
        {
            nameof(RecipeEntity.UpperLamiTmp_Stage1),
            nameof(RecipeEntity.LowerLamiTmp_Stage1),
            nameof(RecipeEntity.UpperLamiTmp_Stage2),
            nameof(RecipeEntity.LowerLamiTmp_Stage2),
            nameof(RecipeEntity.MembraneWidth),
            nameof(RecipeEntity.PanelOutTmp),
            nameof(RecipeEntity.UpperTackingTmp),
            nameof(RecipeEntity.LowerTackingTmp),
            nameof(RecipeEntity.DrvSpeed),
            nameof(RecipeEntity.UpperTackingPressure),
            nameof(RecipeEntity.FrontCuPos),
            nameof(RecipeEntity.RearCuPos),
            nameof(RecipeEntity.RollerPressure_Stage1),
            nameof(RecipeEntity.RollerPressure_Stage2),
            nameof(RecipeEntity.DurationFilm),
            nameof(RecipeEntity.PlateThickness),
        };

        public async Task<List<string>> GetPpidListAsync()
        {
            var recipes = await _recipeService.QueryAllAsync();
            return recipes.Select(r => r.RecipeName).ToList();
        }

        public async Task<byte> DeletePpidAsync(IReadOnlyList<string> ppids)
        {
            var recipes = await _recipeService.QueryAllAsync();

            if (ppids.Count == 0)
            {
                foreach (var r in recipes)
                    await _recipeService.DeleteAsync(r.Id);
                return SecsAckCodes.Ackc7Accepted;
            }

            foreach (var ppid in ppids)
            {
                var recipe = recipes.FirstOrDefault(r => r.RecipeName == ppid);
                if (recipe == null)
                    return SecsAckCodes.Ackc7PpidNotFound;
            }

            foreach (var ppid in ppids)
            {
                var recipe = recipes.First(r => r.RecipeName == ppid);
                await _recipeService.DeleteAsync(recipe.Id);
            }
            return SecsAckCodes.Ackc7Accepted;
        }

        public async Task<byte> InquireLoadAsync(string ppid)
        {
            var recipes = await _recipeService.QueryAllAsync();
            bool exists = recipes.Any(r => r.RecipeName == ppid);
            return exists ? SecsAckCodes.PpgntOk : SecsAckCodes.PpgntInvalidPpid;
        }

        public async Task<byte> StoreProcessProgramAsync(string ppid, string ccode, IReadOnlyList<Item> pparms)
        {
            if (pparms.Count != ParamOrder.Length)
                return SecsAckCodes.Ackc7LengthError;

            // 反解参数到 recipe 字段（按固定顺序，前 8 int / 后 8 double）
            var recipe = new RecipeEntity { RecipeName = ppid, Ccode = ccode };
            if (!TryFillParams(recipe, pparms))
                return SecsAckCodes.Ackc7LengthError;

            // 已存在则更新，否则新增
            var existing = (await _recipeService.QueryAllAsync()).FirstOrDefault(r => r.RecipeName == ppid);
            if (existing != null)
            {
                recipe.Id = existing.Id;
                await _recipeService.UpdateAsync(recipe);
            }
            else
            {
                await _recipeService.InsertAsync(recipe);
            }
            return SecsAckCodes.Ackc7Accepted;
        }

        public async Task<ProcessProgramData?> GetProcessProgramAsync(string ppid)
        {
            var recipe = (await _recipeService.QueryAllAsync()).FirstOrDefault(r => r.RecipeName == ppid);
            if (recipe == null)
                return null;

            var items = BuildParamItems(recipe);
            return new ProcessProgramData(recipe.Ccode ?? string.Empty, items);
        }

        /// <summary>把 16 个配方字段按固定顺序转成 SECS Item（int→I4，double→F8）。</summary>
        private static IReadOnlyList<Item> BuildParamItems(RecipeEntity r)
        {
            return new Item[]
            {
                Item.I4(r.UpperLamiTmp_Stage1),
                Item.I4(r.LowerLamiTmp_Stage1),
                Item.I4(r.UpperLamiTmp_Stage2),
                Item.I4(r.LowerLamiTmp_Stage2),
                Item.I4((int)r.MembraneWidth),
                Item.I4(r.PanelOutTmp),
                Item.I4(r.UpperTackingTmp),
                Item.I4(r.LowerTackingTmp),
                Item.F8(r.DrvSpeed),
                Item.F8(r.UpperTackingPressure),
                Item.F8(r.FrontCuPos),
                Item.F8(r.RearCuPos),
                Item.F8(r.RollerPressure_Stage1),
                Item.F8(r.RollerPressure_Stage2),
                Item.F8(r.DurationFilm),
                Item.F8(r.PlateThickness),
            };
        }

        /// <summary>把 S7F23 下发的参数 Item 列表反解回 recipe 字段。任一解析失败返回 false。</summary>
        private static bool TryFillParams(RecipeEntity r, IReadOnlyList<Item> pparms)
        {
            // 前 8 个 int 字段
            r.UpperLamiTmp_Stage1 = ParseInt(pparms[0]);
            r.LowerLamiTmp_Stage1 = ParseInt(pparms[1]);
            r.UpperLamiTmp_Stage2 = ParseInt(pparms[2]);
            r.LowerLamiTmp_Stage2 = ParseInt(pparms[3]);
            int mw = ParseInt(pparms[4]);
            if (mw < 0 || mw > 3) return false;
            r.MembraneWidth = (MembraneWidthRange)mw;
            r.PanelOutTmp = ParseInt(pparms[5]);
            r.UpperTackingTmp = ParseInt(pparms[6]);
            r.LowerTackingTmp = ParseInt(pparms[7]);

            // 后 8 个 double 字段
            r.DrvSpeed = ParseDouble(pparms[8]);
            r.UpperTackingPressure = ParseDouble(pparms[9]);
            r.FrontCuPos = ParseDouble(pparms[10]);
            r.RearCuPos = ParseDouble(pparms[11]);
            r.RollerPressure_Stage1 = ParseDouble(pparms[12]);
            r.RollerPressure_Stage2 = ParseDouble(pparms[13]);
            r.DurationFilm = ParseDouble(pparms[14]);
            r.PlateThickness = ParseDouble(pparms[15]);
            return true;
        }

        /// <summary>从 Item 取整数值（兼容 ASCII 文本与数值类型）。</summary>
        private static int ParseInt(Item item)
        {
            if (item.Format == SecsFormat.ASCII)
                return int.Parse(item.GetString()!.Trim(), CultureInfo.InvariantCulture);

            return item.Format switch
            {
                SecsFormat.I1 => item.FirstValue<sbyte>(),
                SecsFormat.I2 => item.FirstValue<short>(),
                SecsFormat.I4 => item.FirstValue<int>(),
                SecsFormat.I8 => item.FirstValue<long>() > int.MaxValue ? int.MaxValue : (int)item.FirstValue<long>(),
                SecsFormat.U1 => item.FirstValue<byte>(),
                SecsFormat.U2 => item.FirstValue<ushort>(),
                SecsFormat.U4 => item.FirstValue<uint>() > int.MaxValue ? int.MaxValue : (int)item.FirstValue<uint>(),
                SecsFormat.F4 => (int)item.FirstValue<float>(),
                SecsFormat.F8 => (int)item.FirstValue<double>(),
                _ => 0,
            };
        }

        /// <summary>从 Item 取浮点值（兼容 ASCII 文本与数值类型）。</summary>
        private static double ParseDouble(Item item)
        {
            if (item.Format == SecsFormat.ASCII)
                return double.Parse(item.GetString()!.Trim(), CultureInfo.InvariantCulture);

            return item.Format switch
            {
                SecsFormat.I1 => item.FirstValue<sbyte>(),
                SecsFormat.I2 => item.FirstValue<short>(),
                SecsFormat.I4 => item.FirstValue<int>(),
                SecsFormat.I8 => item.FirstValue<long>(),
                SecsFormat.U1 => item.FirstValue<byte>(),
                SecsFormat.U2 => item.FirstValue<ushort>(),
                SecsFormat.U4 => item.FirstValue<uint>(),
                SecsFormat.F4 => item.FirstValue<float>(),
                SecsFormat.F8 => item.FirstValue<double>(),
                _ => 0d,
            };
        }
    }
}
