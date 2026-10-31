using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Stream 10（终端服务）业务服务实现。
    /// 无物理终端设备，Host 下发的显示文本记录日志，并作为「通知」写入通知公告表（sys_notice），
    /// 回「已接受显示」。写入的公告会在侧边栏通知公告菜单产生未读角标，供操作员确认阅读。
    /// </summary>
    public sealed class S10TerminalService : IS10TerminalService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public S10TerminalService(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<byte> DisplayAsync(long instanceId, byte tid, string text)
        {
            Log.Information("[SECS] 终端显示(实例 {InstanceId}, TID={Tid}): {Text}", instanceId, tid, text);

            try
            {
                await using var db = await _contextFactory.CreateDbContextAsync();

                var notice = new SysNotice
                {
                    NoticeTitle = tid == 1 ? "终端警告" : "终端消息",
                    NoticeType = "0", // 通知
                    NoticeContent = text,
                    Source = "S10F3",
                    Status = "0",
                    CreateTime = DateTime.Now,
                    CreateBy = "HOST"
                };

                db.SysNotices.Add(notice);
                await db.SaveChangesAsync();

                // 广播通知变更，驱动侧边栏未读角标刷新
                WeakReferenceMessenger.Default.Send(new NoticeChangedMessage());
            }
            catch (Exception ex)
            {
                // 落库失败不阻断 SECS 应答，仅记录日志
                Log.Error(ex, "[SECS] 终端显示消息写入通知公告表失败");
            }

            return SecsAckCodes.Ackc10Accepted;
        }
    }
}
