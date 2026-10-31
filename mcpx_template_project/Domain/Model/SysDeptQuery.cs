namespace mcpx_template_project.Domain.Model
{
    public class SysDeptQuery
    {
        public long? DeptId { get; set; }

        public long? ParentId { get; set; }

        public string? DeptName { get; set; }

        public string? Status { get; set; }
    }
}
