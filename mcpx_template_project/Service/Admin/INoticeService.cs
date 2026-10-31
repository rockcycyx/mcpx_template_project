using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    /// <summary>
    /// 通知公告服务。
    /// 支撑 PageNotice 的基本 CRUD，以及「未确认阅读」的未读角标统计；
    /// 同时承接 SECS S10F3 终端消息落库（Host 下发的内容作为通知写入）。
    /// </summary>
    public interface INoticeService
    {
        /// <summary>分页查询通知公告（含当前用户已读状态）</summary>
        Task<PagedResult<SysNoticeVO>> QueryNoticesPagedAsync(SysNoticeQuery query, PagingParameters paging);

        /// <summary>根据 ID 查询单条通知</summary>
        Task<SysNoticeVO?> GetNoticeByIdAsync(long noticeId);

        /// <summary>新增通知</summary>
        Task<SysNoticeVO> InsertNoticeAsync(SysNotice notice);

        /// <summary>修改通知</summary>
        Task<SysNoticeVO> UpdateNoticeAsync(SysNotice notice);

        /// <summary>删除通知（连带清理已读记录）</summary>
        Task DeleteNoticeAsync(long noticeId);

        /// <summary>切换通知状态</summary>
        Task SwitchStatusAsync(long noticeId, string status);

        /// <summary>查询当前用户的未读通知数量（驱动侧边栏角标）</summary>
        Task<int> QueryUnreadCountAsync();

        /// <summary>当前用户标记某条通知为已读</summary>
        Task MarkReadAsync(long noticeId);

        /// <summary>当前用户标记全部通知为已读</summary>
        Task MarkAllReadAsync();

        /// <summary>S10F3 终端消息落库：作为「通知」类型写入（来源标记为 S10F3）</summary>
        Task<long> InsertTerminalNoticeAsync(string title, string content);
    }
}
