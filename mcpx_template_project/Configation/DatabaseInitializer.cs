using System.IO;
using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Domain.Entities.System;

namespace mcpx_template_project.Configation
{
    /// <summary>
    /// 数据库初始化器：使用 EF Core Code First 自动建表 + 种子数据
    /// 不再依赖手动 DDL 脚本执行
    /// </summary>
    /// <author>Rudeus</author>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// 种子数据SQL文件按依赖顺序执行（仅系统管理模板种子 sys_xxx）
        /// </summary>
        private static readonly string[] SeedFileOrder =
        {
            "sys_dept.sql",
            "sys_dict_data.sql",
            "sys_menu.sql",
            "sys_post.sql",
            "sys_role.sql",
            "sys_role_menu.sql",
            "sys_user.sql",
            "sys_user_post.sql",
            "sys_user_role.sql",
            "machine.sql"
        };

        /// <summary>
        /// 执行数据库初始化：
        /// 1. EnsureCreated（Code First 按 DbSet/模型 创建表，若表已存在则跳过）
        /// 2. 若无种子数据，则执行初始化 SQL
        /// </summary>
        /// <param name="contextFactory">EF DbContext 工厂</param>
        /// <param name="sqlDirectory">种子数据SQL文件所在目录（相对当前工作目录）</param>
        public static void Initialize(
            IDbContextFactory<EFDbContext> contextFactory,
            string sqlDirectory)
        {
            using var db = contextFactory.CreateDbContext();

            // 1. 基于 EF 模型自动建表
            db.Database.EnsureCreated();

            // 1.1 确保机台表存在（旧库升级：EnsureCreated 不重建已有库，需显式补建 machine 表）
            EnsureTable(db,
                "CREATE TABLE IF NOT EXISTS machine (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "comm_setting_id INTEGER NOT NULL, " +
                "name TEXT NOT NULL DEFAULT '', " +
                "create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT);");

            // 1.2 旧库升级：plc_trigger 表补充 alarm_category 列（警报类别，ALCD bit7-1）
            EnsureColumn(db, "plc_trigger", "alarm_category", "INTEGER NOT NULL DEFAULT 0");

            // 1.3 旧库升级：recipe 表补充 ccode 列（S7 工艺程序命令码）
            EnsureColumn(db, "recipe", "ccode", "TEXT");

            // 1.4 旧库升级：通知公告表（EnsureCreated 不重建已有库，需显式补建）
            EnsureTable(db,
                "CREATE TABLE IF NOT EXISTS sys_notice (" +
                "notice_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "notice_title TEXT NOT NULL DEFAULT '', " +
                "notice_type TEXT NOT NULL DEFAULT '0', " +
                "notice_content TEXT, " +
                "source TEXT, " +
                "status TEXT NOT NULL DEFAULT '0', " +
                "create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT);");

            EnsureTable(db,
                "CREATE TABLE IF NOT EXISTS sys_notice_read (" +
                "notice_id INTEGER NOT NULL, " +
                "user_id INTEGER NOT NULL, " +
                "read_time TEXT, " +
                "PRIMARY KEY (notice_id, user_id));");

            // 1.5 旧库升级：secs_sxfy 表（SxFy 消息权限，配合 [SecsPermission] 前置增强）
            EnsureTable(db,
                "CREATE TABLE IF NOT EXISTS secs_sxfy (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "instance_id INTEGER NOT NULL, " +
                "stream INTEGER NOT NULL, " +
                "function INTEGER NOT NULL, " +
                "allow_offline INTEGER NOT NULL DEFAULT 0, " +
                "allow_online_local INTEGER NOT NULL DEFAULT 0, " +
                "allow_online_remote INTEGER NOT NULL DEFAULT 0, " +
                "status TEXT NOT NULL DEFAULT '0', " +
                "name TEXT NOT NULL DEFAULT '', " +
                "create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT);");

            // 2. 检查是否已有种子数据（以 sys_user 为标志）
            if (db.SysUsers.Any())
                return;

            // 3. 执行SQL种子数据文件（事务包裹，确保原子性）
            string fullSqlDir = System.IO.Path.IsPathRooted(sqlDirectory)
                ? sqlDirectory
                : System.IO.Path.Combine(Directory.GetCurrentDirectory(), sqlDirectory);

            if (!Directory.Exists(fullSqlDir))
                return;

            using var transaction = db.Database.BeginTransaction();
            try
            {
                foreach (var fileName in SeedFileOrder)
                {
                    string filePath = System.IO.Path.Combine(fullSqlDir, fileName);
                    ExecuteSqlFile(db, filePath);
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 确保表中存在指定列（SQLite），不存在则 ALTER TABLE 补充。
        /// 用于旧数据库升级：EnsureCreated 只建新表，不会修改已有表结构。
        /// </summary>
        private static void EnsureColumn(EFDbContext db, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                var conn = db.Database.GetDbConnection();
                var wasClosed = conn.State != System.Data.ConnectionState.Open;
                if (wasClosed) conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = $"PRAGMA table_info({tableName});";
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                                return; // 列已存在
                        }
                    }
                }
                finally
                {
                    if (wasClosed) conn.Close();
                }

                db.Database.ExecuteSqlRaw($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
            }
            catch
            {
                // 结构同步失败不阻断启动（缺失列会在相关查询时暴露具体错误）
            }
        }

        /// <summary>
        /// 执行建表语句（IF NOT EXISTS），失败不阻断启动。
        /// </summary>
        private static void EnsureTable(EFDbContext db, string createTableSql)
        {
            try
            {
                db.Database.ExecuteSqlRaw(createTableSql);
            }
            catch
            {
                // 建表失败不阻断启动（缺失表会在相关查询时暴露具体错误）
            }
        }

        private static void ExecuteSqlFile(EFDbContext db, string filePath)
        {
            if (!File.Exists(filePath))
                return;

            string sql = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(sql))
                return;

            // SQLite 原生支持多语句执行，直接传入完整 SQL 文本
            // 避免按 ; 拆分时误拆分字符串值内部的引号
            db.Database.ExecuteSqlRaw(sql);
        }
    }
}
