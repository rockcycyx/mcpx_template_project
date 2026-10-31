using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.DependencyInjection;
using mcpx_template_project.Domain.Entities.Business.CommSetting;
using mcpx_template_project.Domain.Entities.Business.Recipe;
using mcpx_template_project.Domain.Entities.Business.Secs;
using mcpx_template_project.Domain.Entities.Business.Machine;
using PlcTriggerEntity = mcpx_template_project.Domain.Entities.Business.Trigger.PlcTrigger;

namespace mcpx_template_project.Configation
{
    /// <summary>
    /// EF‑Core 数据库上下文
    /// </summary>
    /// <author>Rudeus</author>
    public class EFDbContext : DbContext, IDesignTimeDbContextFactory<EFDbContext>
    {
        public DbSet<SysUser> SysUsers { get; set; }

        public DbSet<SysOperLog> SysOperLogs { get; set; }

        public DbSet<SysDept> SysDepts { get; set; }

        public DbSet<SysMenu> SysMenus { get; set; }

        public DbSet<SysRole> SysRoles { get; set; }

        public DbSet<SysUserRole> SysUserRoles { get; set; }

        public DbSet<SysUserPost> SysUserPosts { get; set; }

        public DbSet<SysRoleMenu> SysRoleMenus { get; set; }

        public DbSet<SysPost> SysPosts { get; set; }

        public DbSet<SysNotice> SysNotices { get; set; }

        public DbSet<SysNoticeRead> SysNoticeReads { get; set; }

        public DbSet<SysDictData> SysDictDatas { get; set; }

        public DbSet<CommSetting> CommSettings { get; set; }

        public DbSet<CommSettingArea> CommSettingAreas { get; set; }

        public DbSet<Recipe> Recipes { get; set; }

        public DbSet<PlcTriggerEntity> PlcTriggers { get; set; }

        public DbSet<SecsVid> SecsVids { get; set; }

        public DbSet<SecsReport> SecsReports { get; set; }

        public DbSet<SecsReportVid> SecsReportVids { get; set; }

        public DbSet<SecsCeid> SecsCeids { get; set; }

        public DbSet<SecsCeidReport> SecsCeidReports { get; set; }

        public DbSet<SecsInstance> SecsInstances { get; set; }

        public DbSet<SecsSxFy> SecsSxFys { get; set; }

        public DbSet<Machine> Machines { get; set; }


        public EFDbContext() { }

        public EFDbContext(DbContextOptions<EFDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SysUserRole>()
                .HasKey(e => new { e.UserId, e.RoleId });

            modelBuilder.Entity<SysUserPost>()
                .HasKey(e => new { e.UserId, e.PostId });

            modelBuilder.Entity<SysRoleMenu>()
                .HasKey(e => new { e.RoleId, e.MenuId });

            modelBuilder.Entity<SysNoticeRead>()
                .HasKey(e => new { e.NoticeId, e.UserId });

            modelBuilder.Entity<CommSettingArea>()
                .HasOne(a => a.Setting)
                .WithMany(s => s.Areas)
                .HasForeignKey(a => a.SettingId)
                .OnDelete(DeleteBehavior.Cascade);

            // SECS/GEM 数据表：Report ↔ VID 多对多、CEID ↔ Report 关联
            modelBuilder.Entity<SecsReportVid>()
                .HasOne(rv => rv.Report)
                .WithMany(r => r.Vids)
                .HasForeignKey(rv => rv.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SecsReportVid>()
                .HasOne(rv => rv.Vid)
                .WithMany()
                .HasForeignKey(rv => rv.VidId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SecsCeidReport>()
                .HasOne(cr => cr.Ceid)
                .WithMany(c => c.Reports)
                .HasForeignKey(cr => cr.CeidId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SecsCeidReport>()
                .HasOne(cr => cr.Report)
                .WithMany()
                .HasForeignKey(cr => cr.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // VID 类别枚举 → 存字符串（"SVID"/"DVID"/"ECID"），兼容旧库 TEXT 列
            modelBuilder.Entity<SecsVid>()
                .Property(v => v.VidType)
                .HasConversion<string>();

            // VID 数据类型枚举 → 存字符串（"U1"/"A" 等），兼容旧库 TEXT 列
            modelBuilder.Entity<SecsVid>()
                .Property(v => v.DataType)
                .HasConversion<string>();

            // SECS 实例：可空关联 PLC 通讯设定（comm_setting），删除实例时置空而非级联
            modelBuilder.Entity<SecsInstance>()
                .HasOne<CommSetting>()
                .WithMany()
                .HasForeignKey(i => i.CommSettingId)
                .OnDelete(DeleteBehavior.SetNull);

        }

        public EFDbContext CreateDbContext(string[] args)
        {
            var connStr = DbRegister.ResolveConnectionString(
                AppConfiguration.BuildConfiguration()
                    .GetConnectionString("SqliteDb"));

            var optionsBuilder = new DbContextOptionsBuilder<EFDbContext>();
            optionsBuilder.UseSqlite(connStr);
            return new EFDbContext(optionsBuilder.Options);
        }
    }
}