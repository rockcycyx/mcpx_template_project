using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using Bcrypt = BCrypt.Net.BCrypt;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Admin.impl
{
    public class UserServiceImpl : IUserService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public UserServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<ApiResult<SysUser>> LoginAsync(string userName, string password)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var user = await db.SysUsers
                .FirstOrDefaultAsync(u => u.UserName == userName && u.DelFlag != "2");

            if (user == null)
            {
                return new ApiResult<SysUser> { Code = 401, Msg = "账号不存在" };
            }

            if (user.Status == "1")
            {
                return new ApiResult<SysUser> { Code = 401, Msg = "账号已停用" };
            }

            if (!VerifyPassword(password, user.Password))
            {
                return new ApiResult<SysUser> { Code = 401, Msg = "密码错误" };
            }

            user.LoginDate = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return new ApiResult<SysUser> { Code = 200, Msg = "登录成功", Data = user };
        }

        public async Task<ApiResult<SysUser>> InsertUserAsync(SysUser user, List<long> roleIds, List<long> postIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var exists = await db.SysUsers
                .AnyAsync(u => u.UserName == user.UserName && u.DelFlag != "2");

            if (exists)
            {
                return new ApiResult<SysUser> { Code = 400, Msg = $"用户名称 '{user.UserName}' 已存在" };
            }

            user.Password = Bcrypt.HashPassword(user.Password);
            user.CreateTime = DateTime.Now;
            user.CreateBy = AppHelper.GetCurrentUserName();

            db.SysUsers.Add(user);
            await db.SaveChangesAsync();

            // 保存用户角色关联
            if (roleIds != null && roleIds.Count > 0)
            {
                foreach (var roleId in roleIds)
                {
                    db.SysUserRoles.Add(new SysUserRole
                    {
                        UserId = user.UserId,
                        RoleId = roleId
                    });
                }
            }

            // 保存用户岗位关联
            if (postIds != null && postIds.Count > 0)
            {
                foreach (var postId in postIds)
                {
                    db.SysUserPosts.Add(new SysUserPost
                    {
                        UserId = user.UserId,
                        PostId = postId
                    });
                }
            }

            await db.SaveChangesAsync();

            return new ApiResult<SysUser> { Code = 200, Msg = "新增成功", Data = user };
        }

        public async Task<ApiResult<SysUser>> UpdateUserAsync(SysUser user, List<long> roleIds, List<long> postIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var exists = await db.SysUsers
                .FirstOrDefaultAsync(u => u.UserId == user.UserId && u.DelFlag != "2");

            if (exists == null)
            {
                return new ApiResult<SysUser> { Code = 400, Msg = "用户不存在" };
            }

            var duplicateName = await db.SysUsers
                .AnyAsync(u => u.UserName == user.UserName && u.UserId != user.UserId && u.DelFlag != "2");

            if (duplicateName)
            {
                return new ApiResult<SysUser> { Code = 400, Msg = $"用户名称 '{user.UserName}' 已存在" };
            }

            exists.NickName = user.NickName;
            exists.UserName = user.UserName;
            exists.Email = user.Email;
            exists.PhoneNumber = user.PhoneNumber;
            exists.Sex = user.Sex;
            exists.Status = user.Status;
            exists.DeptId = user.DeptId;
            exists.Remark = user.Remark;
            exists.UpdateBy = AppHelper.GetCurrentUserName();
            exists.UpdateTime = DateTime.Now;

            db.SysUsers.Update(exists);

            // 更新用户角色关联：先删除旧关联，再插入新关联
            var oldUserRoles = db.SysUserRoles.Where(ur => ur.UserId == user.UserId);
            db.SysUserRoles.RemoveRange(oldUserRoles);

            if (roleIds != null && roleIds.Count > 0)
            {
                foreach (var roleId in roleIds)
                {
                    db.SysUserRoles.Add(new SysUserRole
                    {
                        UserId = user.UserId,
                        RoleId = roleId
                    });
                }
            }

            // 更新用户岗位关联：先删除旧关联，再插入新关联
            var oldUserPosts = db.SysUserPosts.Where(up => up.UserId == user.UserId);
            db.SysUserPosts.RemoveRange(oldUserPosts);

            if (postIds != null && postIds.Count > 0)
            {
                foreach (var postId in postIds)
                {
                    db.SysUserPosts.Add(new SysUserPost
                    {
                        UserId = user.UserId,
                        PostId = postId
                    });
                }
            }

            await db.SaveChangesAsync();

            return new ApiResult<SysUser> { Code = 200, Msg = "修改成功", Data = exists };
        }

        public async Task<PagedResult<SysUserVO>> QueryUsersPagedAsync(SysUserQuery query, PagingParameters paging)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = db.SysUsers.AsNoTracking()
                .Where(u => u.DelFlag == "0");

            if (query.UserId.HasValue && query.UserId.Value != 0)
            {
                queryable = queryable.Where(u => u.UserId == query.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.UserName))
            {
                queryable = queryable.Where(u =>
                    EF.Functions.Like(u.UserName, $"%{query.UserName}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                queryable = queryable.Where(u => u.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
            {
                queryable = queryable.Where(u =>
                    EF.Functions.Like(u.PhoneNumber!, $"%{query.PhoneNumber}%"));
            }

            if (query.BeginTime.HasValue)
            {
                var beginDate = query.BeginTime.Value.Date;
                queryable = queryable.Where(u =>
                    u.CreateTime.HasValue && u.CreateTime.Value >= beginDate);
            }

            if (query.EndTime.HasValue)
            {
                var endDate = query.EndTime.Value.Date.AddDays(1);
                queryable = queryable.Where(u =>
                    u.CreateTime.HasValue && u.CreateTime.Value < endDate);
            }

            List<long> deptIds = new();
            if (query.DeptId.HasValue && query.DeptId.Value != 0)
            {
                var deptId = query.DeptId.Value;
                deptIds = db.SysDepts.AsNoTracking()
                    .Where(d => d.DeptId == deptId ||
                                (d.Ancestors != null &&
                                 (EF.Functions.Like(d.Ancestors, $"{deptId},%") ||
                                  EF.Functions.Like(d.Ancestors, $"%{deptId},%") ||
                                  EF.Functions.Like(d.Ancestors, $",{deptId}"))))
                    .Select(d => d.DeptId)
                    .ToList();

                queryable = queryable.Where(u =>
                    u.DeptId.HasValue && deptIds.Contains(u.DeptId.Value));
            }

            var pagedEntities = await queryable
                .OrderBy(u => u.UserId)
                .ToPagedResultAsync(paging);

            var users = pagedEntities.Items.ToList();

            var userDeptIds = users
                .Where(u => u.DeptId.HasValue)
                .Select(u => u.DeptId!.Value)
                .Distinct()
                .ToList();

            var deptMap = userDeptIds.Count > 0
                ? db.SysDepts.AsNoTracking()
                    .Where(d => userDeptIds.Contains(d.DeptId))
                    .ToDictionary(d => d.DeptId, d => d.DeptName)
                : new Dictionary<long, string>();

            var result = users.Select(u => new SysUserVO
            {
                UserId = u.UserId,
                DeptId = u.DeptId,
                UserName = u.UserName,
                NickName = u.NickName,
                Email = u.Email,
                Avatar = u.Avatar,
                PhoneNumber = u.PhoneNumber,
                Sex = u.Sex,
                Status = u.Status,
                LoginIp = u.LoginIp,
                LoginDate = u.LoginDate,
                CreateBy = u.CreateBy,
                CreateTime = u.CreateTime,
                Remark = u.Remark,
                DeptName = u.DeptId.HasValue && deptMap.ContainsKey(u.DeptId.Value)
                    ? deptMap[u.DeptId.Value]
                    : null
            }).ToList();

            return new PagedResult<SysUserVO>
            {
                Items = result,
                TotalCount = pagedEntities.TotalCount,
                PageIndex = pagedEntities.PageIndex,
                PageSize = pagedEntities.PageSize
            };
        }

        public async Task<ApiResult<int>> DeleteUsersAsync(List<long> userIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            if (userIds == null || userIds.Count == 0)
            {
                return new ApiResult<int> { Code = 400, Msg = "请选择要删除的用户", Data = 0 };
            }

            var users = await db.SysUsers
                .Where(u => userIds.Contains(u.UserId) && u.DelFlag != "2")
                .ToListAsync();

            if (users.Count == 0)
            {
                return new ApiResult<int> { Code = 400, Msg = "未找到要删除的用户", Data = 0 };
            }

            // 删除用户与角色的关联，避免残留导致角色无法删除
            var userRoles = await db.SysUserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();
            if (userRoles.Count > 0)
            {
                db.SysUserRoles.RemoveRange(userRoles);
            }

            // 删除用户与岗位的关联，避免残留
            var userPosts = await db.SysUserPosts
                .Where(up => userIds.Contains(up.UserId))
                .ToListAsync();
            if (userPosts.Count > 0)
            {
                db.SysUserPosts.RemoveRange(userPosts);
            }

            foreach (var user in users)
            {
                user.DelFlag = "2";
            }

            db.SysUsers.UpdateRange(users);
            await db.SaveChangesAsync();

            return new ApiResult<int> { Code = 200, Msg = "删除成功", Data = users.Count };
        }

        public async Task SwitchStatusAsync(long userId, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var entity = await db.SysUsers.FindAsync(userId);
            if (entity != null)
            {
                entity.Status = status;
                entity.UpdateBy = AppHelper.GetCurrentUserName();
                entity.UpdateTime = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }

        public async Task<ApiResult<int>> InsertUsersAsync(List<SysUser> users)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var existingNames = await db.SysUsers
                .Where(u => u.DelFlag != "2")
                .Select(u => u.UserName)
                .ToListAsync();

            var validUsers = new List<SysUser>();
            var skipped = new List<string>();

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    skipped.Add($"{user.NickName ?? ""}({user.UserName}) 账号为空");
                    continue;
                }

                if (existingNames.Contains(user.UserName))
                {
                    skipped.Add($"{user.UserName} 账号已存在");
                    continue;
                }

                user.Password = Bcrypt.HashPassword(string.IsNullOrWhiteSpace(user.Password) ? "123456" : user.Password);
                user.CreateTime = DateTime.Now;
                user.CreateBy = AppHelper.GetCurrentUserName();
                user.UserType ??= "00";
                validUsers.Add(user);
            }

            if (validUsers.Count == 0)
            {
                return new ApiResult<int>
                {
                    Code = 400,
                    Msg = skipped.Count > 0 ? string.Join(";", skipped) : "没有可导入的用户",
                    Data = 0
                };
            }

            db.SysUsers.AddRange(validUsers);
            await db.SaveChangesAsync();

            string msg = $"成功导入 {validUsers.Count} 条";
            if (skipped.Count > 0)
                msg += $"，跳过 {skipped.Count} 条（{string.Join(";", skipped)}）";

            return new ApiResult<int> { Code = 200, Msg = msg, Data = validUsers.Count };
        }

        private static bool VerifyPassword(string inputPassword, string storedPassword)
        {
            return Bcrypt.Verify(inputPassword, storedPassword);
        }

        public async Task<ApiResult<SysUser>> UpdateUserProfileAsync(long userId, string nickName, string email, string phoneNumber, string sex)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var user = await db.SysUsers
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DelFlag != "2");

            if (user == null)
            {
                return new ApiResult<SysUser> { Code = 400, Msg = "用户不存在" };
            }

            user.NickName = nickName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;
            user.Sex = sex;
            user.UpdateBy = AppHelper.GetCurrentUserName();
            user.UpdateTime = DateTime.Now;

            db.SysUsers.Update(user);
            await db.SaveChangesAsync();

            return new ApiResult<SysUser> { Code = 200, Msg = "修改成功", Data = user };
        }

        public async Task<ApiResult<int>> ChangePasswordAsync(long userId, string oldPassword, string newPassword)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var user = await db.SysUsers
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DelFlag != "2");

            if (user == null)
            {
                return new ApiResult<int> { Code = 400, Msg = "用户不存在", Data = 0 };
            }

            if (!VerifyPassword(oldPassword, user.Password))
            {
                return new ApiResult<int> { Code = 400, Msg = "原密码错误", Data = 0 };
            }

            user.Password = Bcrypt.HashPassword(newPassword);
            user.PwdUpdateDate = DateTime.Now;
            user.UpdateBy = AppHelper.GetCurrentUserName();
            user.UpdateTime = DateTime.Now;

            db.SysUsers.Update(user);
            await db.SaveChangesAsync();

            return new ApiResult<int> { Code = 200, Msg = "密码修改成功", Data = 1 };
        }
    }
}