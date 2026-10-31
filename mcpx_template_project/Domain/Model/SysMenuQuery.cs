namespace mcpx_template_project.Domain.Model
{
    public class SysMenuQuery
    {
        public long? MenuId { get; set; }

        public long? ParentId { get; set; }

        public string? MenuName { get; set; }

        public string? Status { get; set; }

        public string? MenuType { get; set; }
    }
}
