using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Helper;
using Bcrypt = BCrypt.Net.BCrypt;

namespace mcpx_template_project.Service.Admin.impl
{
    /// <summary>
    /// 授权校验服务实现。
    /// 共享文件：\\sc-filesrv\視動軟體共用資料區\VmIOTemp.ini
    /// INI 格式（section 名为 user_name 的 BCrypt 哈希）：
    ///   [&lt;bcrypt(user_name)&gt;]
    ///   dt=YYYYMMDD    （到期时间）
    ///   fp=xxx         （机器指纹）
    /// </summary>
    public class LicenseCheckServiceImpl : ILicenseCheckService
    {
        private const string IniPath = @"\\192.168.88.101\myshare\VmIOTemp.ini";

        //private const string IniPath = @"\\sc-filesrv\視動軟體共用資料區\VmIOTemp.ini";

        /// <summary>保留账号：除当前登录用户外，此账号不被删除</summary>
        private const string ReservedUserName = "04412";

        /// <summary>
        /// 固定盐（用于 user_name 哈希为 INI section 名，保证同用户每次登录 section 名稳定可复现）。
        /// BCrypt 盐格式：$2a$11$ + 22 位 base64 字符。
        /// </summary>
        private const string FixedSalt = "$2a$11$g9TDdmX67P5jorQgsgj8ueTX";

        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public LicenseCheckServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<LicenseCheckResult> CheckAsync(SysUser user)
        {
            // 1. 本月已检查过则跳过（last_check_time 与当前同月）
            if (IsCheckedThisMonth(user.LastCheckTime))
                return LicenseCheckResult.Ok;

            using var db = await _contextFactory.CreateDbContextAsync();

            // 重新加载用户,确保拿到最新字段
            var dbUser = await db.SysUsers
                .FirstOrDefaultAsync(u => u.UserId == user.UserId && u.DelFlag != "2");
            if (dbUser == null)
                return LicenseCheckResult.FingerprintMismatch;

            var section = Bcrypt.HashPassword(dbUser.UserName, FixedSalt); // 固定盐
            var localFp = HardwareIdHelper.GetFingerprint(); // 获取当前机器指纹HASH

            // 2. 先探测共享目录是否可达。离线（不在局域网）时跳过一切 INI 读写，
            //    避免 GetPrivateProfileString / WritePrivateProfileString 对不可达 UNC 路径
            //    阻塞等待网络超时（SMB 超时可达数十秒），拖慢启动。
            var online = IsShareReachable();

            // 3. 机器指纹 + 到期时间校验（仅在线时走 INI）
            var iniDt = string.Empty; // 到期时间明文（YYYYMMDD）
            if (online)
            {
                var storedFp = ReadIni(section, "fp");           // 获取 section 对应机器指纹
                var storedDtCipher = ReadIni(section, "dt");     // 获取 section 日期 AES 密文
                iniDt = AesHelper.Decrypt(storedDtCipher);       // 解密为明文 YYYYMMDD

                // section 不存在：创建 section，写入指纹码 + 到期时间
                if (string.IsNullOrWhiteSpace(storedFp) && string.IsNullOrWhiteSpace(storedDtCipher))
                {
                    var initialDt = NormalizeExpireDate(dbUser.ExpireDate ?? "20270430");
                    WriteIni(section, "fp", localFp);
                    WriteIni(section, "dt", AesHelper.Encrypt(initialDt)); // AES 加密后存储
                    iniDt = initialDt;
                }
                else if (string.IsNullOrWhiteSpace(storedFp))
                {
                    // 仅缺指纹（如曾只写过 dt）：补写本机指纹
                    WriteIni(section, "fp", localFp);
                }
                else if (!string.Equals(storedFp.Trim(), localFp, StringComparison.OrdinalIgnoreCase))
                {
                    // 指纹不匹配：静默退出
                    return LicenseCheckResult.FingerprintMismatch;
                }
            }

            //// 4. 删除除当前用户与保留账号外的所有用户（物理删除）
            //try
            //{
            //    await DeleteOtherUsersAsync(db, dbUser.UserId);
            //}
            //catch
            //{
            //    // 删除其他用户失败不阻断授权校验（主校验为指纹 + 到期）
            //}

            // 5. 获取到期信息（在线以 INI 为准，离线回退表内 expire_date）
            if (!online || string.IsNullOrWhiteSpace(iniDt))
            {
                // 离线（或 INI 无 dt）：用表内到期时间兜底
                if (IsExpired(dbUser.ExpireDate))
                    return LicenseCheckResult.Expired;
            }
            else
            {
                // 在线：以 INI 的 dt 为准
                if (IsExpired(iniDt.Trim()))
                    return LicenseCheckResult.Expired;

                // 更新表内到期时间为 INI 值
                dbUser.ExpireDate = NormalizeExpireDate(iniDt);
            }

            // 更新最后检查时间
            dbUser.LastCheckTime = DateTime.Now;
            await db.SaveChangesAsync();

            return LicenseCheckResult.Ok;
        }

        /// <summary>
        /// 探测共享目录根路径是否可达（在线/离线快速判断）。
        /// 使用 GetFileAttributes 直接探测 UNC 根目录，失败时快速返回（不依赖 .NET 异常，
        /// 且比 GetPrivateProfileString 失败得更快）。若共享不可达（离线），后续跳过所有 INI 读写，
        /// 避免 SMB 网络超时阻塞启动。
        /// </summary>
        private static bool IsShareReachable()
        {
            try
            {
                // 取 INI 所在目录的父目录作为共享根（如 \\server\share）
                var shareRoot = GetShareRoot(IniPath);
                if (string.IsNullOrEmpty(shareRoot))
                    return false;

                var attrs = GetFileAttributes(shareRoot);
                // INVALID_FILE_ATTRIBUTES == -1，表示路径不可达或不存在
                return attrs != -1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>从完整 UNC 文件路径提取共享根目录（\\server\share）</summary>
        private static string GetShareRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            // 去掉可能的文件名部分，定位到第一个 "\share" 之后
            var s = path.Replace('/', '\\').Trim();
            if (!s.StartsWith(@"\\"))
                return "";

            var noPrefix = s.Substring(2); // 去掉开头的 "\\"
            var idx = noPrefix.IndexOf('\\');
            if (idx <= 0 || idx == noPrefix.Length - 1)
                return "";

            var server = noPrefix.Substring(0, idx);
            var rest = noPrefix.Substring(idx + 1);
            var shareIdx = rest.IndexOf('\\');
            var share = shareIdx < 0 ? rest : rest.Substring(0, shareIdx);

            return $@"\\{server}\{share}";
        }

        /// <summary>判断 last_check_time 是否与当前同月（跨月才重新校验）</summary>
        private static bool IsCheckedThisMonth(DateTime? lastCheckTime)
        {
            if (!lastCheckTime.HasValue)
                return false;
            var now = DateTime.Now;
            return lastCheckTime.Value.Year == now.Year && lastCheckTime.Value.Month == now.Month;
        }

        /// <summary>判断到期时间是否已过期。支持 YYYYMMDD、YYYYMM、YYMMDD 等格式。</summary>
        private static bool IsExpired(string? expireDate)
        {
            if (string.IsNullOrWhiteSpace(expireDate))
                return true; // 无到期信息视为已过期（保守）

            if (!TryParseExpireDate(expireDate, out var expire))
                return true; // 格式无法识别视为已过期（保守）

            // 到期日当天仍可用，次日零点失效
            return DateTime.Now.Date > expire.Date;
        }

        /// <summary>
        /// 解析到期时间，返回该授权最后一天（到期日）。
        /// 支持格式：
        ///   - YYYYMMDD（8位，如 20270430）
        ///   - YYYYMM（6位，如 202610）→ 该月最后一天
        ///   - YYMMDD（6位，如 270430）→ 补 20 前缀
        /// </summary>
        private static bool TryParseExpireDate(string raw, out DateTime expire)
        {
            expire = default;
            var s = raw.Trim().Replace("-", "").Replace("/", "").Replace(".", "");

            if (s.Length == 8)
            {
                if (DateTime.TryParseExact(s, "yyyyMMdd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out expire))
                    return true;
                return false;
            }

            if (s.Length == 6)
            {
                // 尝试作为 YYYYMM（如 202610）—— 月份 1~12 且前4位为合法年份
                if (int.TryParse(s.Substring(0, 4), out var year)
                    && int.TryParse(s.Substring(4, 2), out var month)
                    && month >= 1 && month <= 12)
                {
                    var daysInMonth = DateTime.DaysInMonth(year, month);
                    expire = new DateTime(year, month, daysInMonth);
                    return true;
                }

                // 否则作为 YYMMDD（如 270430）
                var full = "20" + s;
                if (DateTime.TryParseExact(full, "yyyyMMdd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out expire))
                    return true;
            }

            return false;
        }

        /// <summary>规范化到期时间为统一 8 位 YYYYMMDD（用于写回表内 expire_date 字段）</summary>
        private static string NormalizeExpireDate(string raw)
        {
            if (TryParseExpireDate(raw, out var expire))
                return expire.ToString("yyyyMMdd");
            return raw.Trim();
        }

        /// <summary>删除除当前用户与保留账号外的所有用户（物理删除）</summary>
        private static async Task DeleteOtherUsersAsync(EFDbContext db, long currentUserId)
        {
            var others = await db.SysUsers
                .Where(u => u.UserId != currentUserId
                            && u.UserName != ReservedUserName)
                .ToListAsync();

            if (others.Count == 0)
                return;

            // 物理删除：先清理关联表，再删除用户
            var otherIds = others.Select(u => u.UserId).ToList();

            var userRoles = await db.SysUserRoles
                .Where(ur => otherIds.Contains(ur.UserId))
                .ToListAsync();
            if (userRoles.Count > 0)
                db.SysUserRoles.RemoveRange(userRoles);

            var userPosts = await db.SysUserPosts
                .Where(up => otherIds.Contains(up.UserId))
                .ToListAsync();
            if (userPosts.Count > 0)
                db.SysUserPosts.RemoveRange(userPosts);

            db.SysUsers.RemoveRange(others);
            await db.SaveChangesAsync();
        }

        #region INI 读写（P/Invoke 系统 API，支持 UNC 路径）

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            int nSize,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetFileAttributes(string lpFileName);

        private static string ReadIni(string section, string key)
        {
            var sb = new StringBuilder(1024);
            var len = GetPrivateProfileString(section, key, "", sb, sb.Capacity, IniPath);
            return len > 0 ? sb.ToString() : "";
        }

        private static void WriteIni(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, IniPath);
        }

        #endregion
    }
}
