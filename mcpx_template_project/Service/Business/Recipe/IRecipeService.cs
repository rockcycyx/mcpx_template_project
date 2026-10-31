using RecipeEntity = mcpx_template_project.Domain.Entities.Business.Recipe.Recipe;

namespace mcpx_template_project.Service.Business.Recipe
{
    /// <summary>
    /// 配方业务服务：配方的增删改查。
    /// </summary>
    public interface IRecipeService
    {
        /// <summary>查询所有配方（可按名称模糊匹配）</summary>
        Task<List<RecipeEntity>> QueryAllAsync(string? keyword = null);

        /// <summary>按 ID 查询单个配方</summary>
        Task<RecipeEntity?> GetByIdAsync(long id);

        /// <summary>新增配方</summary>
        Task<RecipeEntity> InsertAsync(RecipeEntity recipe);

        /// <summary>修改配方</summary>
        Task<RecipeEntity> UpdateAsync(RecipeEntity recipe);

        /// <summary>删除配方</summary>
        Task DeleteAsync(long id);
    }
}
