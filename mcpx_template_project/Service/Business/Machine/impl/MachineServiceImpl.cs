using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Helper;
using MachineEntity = mcpx_template_project.Domain.Entities.Business.Machine.Machine;

namespace mcpx_template_project.Service.Business.Machine.impl
{
    /// <summary>
    /// 机台业务服务实现。
    /// 唯一性约束：同一 comm_setting_id 不重复（一台通讯设定只能绑定一台机台）。
    /// </summary>
    public class MachineServiceImpl : IMachineService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public MachineServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<MachineEntity>> QueryAllAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.Machines.AsNoTracking().OrderBy(m => m.Id).ToListAsync();
        }

        public async Task<MachineEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.Machines.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MachineEntity> InsertAsync(MachineEntity machine)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureUniqueAsync(db, machine, null);

            machine.Id = 0;
            machine.CreateTime = DateTime.Now;
            machine.CreateBy = AppHelper.GetCurrentUserName();

            db.Machines.Add(machine);
            await db.SaveChangesAsync();
            return machine;
        }

        public async Task<MachineEntity> UpdateAsync(MachineEntity machine)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.Machines.FindAsync(machine.Id);
            if (entity == null)
                throw new InvalidOperationException($"机台不存在（ID: {machine.Id}）");

            await EnsureUniqueAsync(db, machine, machine.Id);

            entity.CommSettingId = machine.CommSettingId;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.Machines.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"机台不存在（ID: {id}）");

            db.Machines.Remove(entity);
            await db.SaveChangesAsync();
        }

        /// <summary>校验同一 comm_setting_id 不重复。</summary>
        private static async Task EnsureUniqueAsync(EFDbContext db, MachineEntity machine, long? excludeId)
        {
            bool conflict = await db.Machines.AnyAsync(m =>
                m.CommSettingId == machine.CommSettingId &&
                (excludeId == null || m.Id != excludeId.Value));

            if (conflict)
                throw new InvalidOperationException(
                    $"通讯设定（ID: {machine.CommSettingId}）已被其它机台绑定");
        }
    }
}
