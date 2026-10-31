using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Helper;
using RecipeEntity = mcpx_template_project.Domain.Entities.Business.Recipe.Recipe;

namespace mcpx_template_project.Service.Business.Recipe.impl
{
    /// <summary>
    /// 配方业务服务实现
    /// </summary>
    public class RecipeServiceImpl : IRecipeService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public RecipeServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<RecipeEntity>> QueryAllAsync(string? keyword = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            IQueryable<RecipeEntity> queryable = db.Recipes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                queryable = queryable.Where(r => EF.Functions.Like(r.RecipeName, $"%{keyword}%"));
            }

            return await queryable.OrderBy(r => r.Id).ToListAsync();
        }

        public async Task<RecipeEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<RecipeEntity> InsertAsync(RecipeEntity recipe)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            if (await db.Recipes.AnyAsync(r => r.RecipeName == recipe.RecipeName))
                throw new InvalidOperationException($"配方名称 '{recipe.RecipeName}' 已存在");

            recipe.Id = 0;
            recipe.CreateTime = DateTime.Now;
            recipe.CreateBy = AppHelper.GetCurrentUserName();

            db.Recipes.Add(recipe);
            await db.SaveChangesAsync();
            return recipe;
        }

        public async Task<RecipeEntity> UpdateAsync(RecipeEntity recipe)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.Recipes.FindAsync(recipe.Id);
            if (entity == null)
                throw new InvalidOperationException($"配方不存在（ID: {recipe.Id}）");

            if (await db.Recipes.AnyAsync(r => r.RecipeName == recipe.RecipeName && r.Id != recipe.Id))
                throw new InvalidOperationException($"配方名称 '{recipe.RecipeName}' 已存在");

            entity.RecipeName = recipe.RecipeName;
            entity.UpperLamiTmp_Stage1 = recipe.UpperLamiTmp_Stage1;
            entity.LowerLamiTmp_Stage1 = recipe.LowerLamiTmp_Stage1;
            entity.UpperLamiTmp_Stage2 = recipe.UpperLamiTmp_Stage2;
            entity.LowerLamiTmp_Stage2 = recipe.LowerLamiTmp_Stage2;
            entity.MembraneWidth = recipe.MembraneWidth;
            entity.PanelOutTmp = recipe.PanelOutTmp;
            entity.UpperTackingTmp = recipe.UpperTackingTmp;
            entity.LowerTackingTmp = recipe.LowerTackingTmp;
            entity.DrvSpeed = recipe.DrvSpeed;
            entity.UpperTackingPressure = recipe.UpperTackingPressure;
            entity.FrontCuPos = recipe.FrontCuPos;
            entity.RearCuPos = recipe.RearCuPos;
            entity.RollerPressure_Stage1 = recipe.RollerPressure_Stage1;
            entity.RollerPressure_Stage2 = recipe.RollerPressure_Stage2;
            entity.DurationFilm = recipe.DurationFilm;
            entity.PlateThickness = recipe.PlateThickness;
            entity.Remark = recipe.Remark;
            entity.Ccode = recipe.Ccode;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.Recipes.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"配方不存在（ID: {id}）");

            db.Recipes.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
