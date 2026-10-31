namespace mcpx_template_project.Domain.Model
{
    public class SysOperLogQuery
    {
        public string? Title { get; set; }

        public int? BusinessType { get; set; }

        public string? OperName { get; set; }

        public string? OperIp { get; set; }

        public int? Status { get; set; }

        public DateTime? BeginTime { get; set; }

        public DateTime? EndTime { get; set; }
    }
}
