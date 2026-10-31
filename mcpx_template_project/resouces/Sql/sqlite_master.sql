INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_dept', 'sys_dept', 2, 'CREATE TABLE "sys_dept" (
    "dept_id" INTEGER NOT NULL CONSTRAINT "PK_sys_dept" PRIMARY KEY AUTOINCREMENT,
    "parent_id" INTEGER NULL,
    "ancestors" TEXT NULL,
    "dept_name" TEXT NOT NULL,
    "order_num" INTEGER NULL,
    "leader" TEXT NULL,
    "phone" TEXT NULL,
    "email" TEXT NULL,
    "status" TEXT NULL,
    "del_flag" TEXT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sqlite_sequence', 'sqlite_sequence', 3, 'CREATE TABLE sqlite_sequence(name,seq)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_dict_data', 'sys_dict_data', 4, 'CREATE TABLE "sys_dict_data" (
    "dict_code" INTEGER NOT NULL CONSTRAINT "PK_sys_dict_data" PRIMARY KEY AUTOINCREMENT,
    "dict_sort" INTEGER NULL,
    "dict_label" TEXT NULL,
    "dict_value" TEXT NULL,
    "dict_type" TEXT NULL,
    "css_class" TEXT NULL,
    "list_class" TEXT NULL,
    "is_default" TEXT NULL,
    "status" TEXT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_menu', 'sys_menu', 5, 'CREATE TABLE "sys_menu" (
    "menu_id" INTEGER NOT NULL CONSTRAINT "PK_sys_menu" PRIMARY KEY AUTOINCREMENT,
    "menu_name" TEXT NOT NULL,
    "parent_id" INTEGER NULL,
    "order_num" INTEGER NULL,
    "component" TEXT NULL,
    "menu_type" TEXT NULL,
    "visible" TEXT NULL,
    "status" TEXT NULL,
    "perms" TEXT NULL,
    "icon" TEXT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_oper_log', 'sys_oper_log', 6, 'CREATE TABLE "sys_oper_log" (
    -- 操作日志记录

    -- 日志主键
    "oper_id" INTEGER NOT NULL CONSTRAINT "PK_sys_oper_log" PRIMARY KEY AUTOINCREMENT,

    -- 模块标题
    "title" TEXT NOT NULL,

    -- 业务类型（0其它 1新增 2修改 3删除）
    "business_type" INTEGER NOT NULL,

    -- 方法名称
    "method" TEXT NOT NULL,

    -- 操作类别（0其它 1后台用户 2手机端用户）
    "operator_type" INTEGER NOT NULL,

    -- 操作人员
    "oper_name" TEXT NOT NULL,

    -- 部门名称
    "dept_name" TEXT NOT NULL,

    -- 主机地址
    "oper_ip" TEXT NOT NULL,

    -- 操作地点
    "oper_location" TEXT NOT NULL,

    -- 请求参数
    "oper_param" TEXT NOT NULL,

    -- 返回参数
    "json_result" TEXT NOT NULL,

    -- 操作状态（0正常 1异常）
    "status" INTEGER NOT NULL,

    -- 错误消息
    "error_msg" TEXT NOT NULL,

    -- 操作时间
    "oper_time" TEXT NULL,

    -- 消耗时间
    "cost_time" INTEGER NOT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_post', 'sys_post', 7, 'CREATE TABLE "sys_post" (
    "post_id" INTEGER NOT NULL CONSTRAINT "PK_sys_post" PRIMARY KEY AUTOINCREMENT,
    "post_code" TEXT NOT NULL,
    "post_name" TEXT NOT NULL,
    "post_sort" INTEGER NOT NULL,
    "status" TEXT NOT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_role', 'sys_role', 8, 'CREATE TABLE "sys_role" (
    "role_id" INTEGER NOT NULL CONSTRAINT "PK_sys_role" PRIMARY KEY AUTOINCREMENT,
    "role_name" TEXT NOT NULL,
    "role_key" TEXT NOT NULL,
    "role_sort" INTEGER NOT NULL,
    "data_scope" TEXT NULL,
    "menu_check_strictly" INTEGER NULL,
    "dept_check_strictly" INTEGER NULL,
    "status" TEXT NOT NULL,
    "del_flag" TEXT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_role_menu', 'sys_role_menu', 9, 'CREATE TABLE "sys_role_menu" (
    "role_id" INTEGER NOT NULL,
    "menu_id" INTEGER NOT NULL,
    CONSTRAINT "PK_sys_role_menu" PRIMARY KEY ("role_id", "menu_id")
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('index', 'sqlite_autoindex_sys_role_menu_1', 'sys_role_menu', 10, null);
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_user', 'sys_user', 12, 'CREATE TABLE "sys_user" (
    "user_id" INTEGER NOT NULL CONSTRAINT "PK_sys_user" PRIMARY KEY AUTOINCREMENT,
    "dept_id" INTEGER NULL,
    "user_name" TEXT NOT NULL,
    "nick_name" TEXT NOT NULL,
    "user_type" TEXT NULL,
    "email" TEXT NULL,
    "phonenumber" TEXT NULL,
    "sex" TEXT NULL,
    "avatar" TEXT NULL,
    "password" TEXT NOT NULL,
    "status" TEXT NULL,
    "del_flag" TEXT NULL,
    "login_ip" TEXT NULL,
    "login_date" TEXT NULL,
    "pwd_update_date" TEXT NULL,
    "expire_date" TEXT NULL,
    "last_check_time" TEXT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_user_post', 'sys_user_post', 14, 'CREATE TABLE "sys_user_post" (
    "user_id" INTEGER NOT NULL,
    "post_id" INTEGER NOT NULL,
    CONSTRAINT "PK_sys_user_post" PRIMARY KEY ("user_id", "post_id")
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('index', 'sqlite_autoindex_sys_user_post_1', 'sys_user_post', 15, null);
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_user_role', 'sys_user_role', 16, 'CREATE TABLE "sys_user_role" (
    "user_id" INTEGER NOT NULL,
    "role_id" INTEGER NOT NULL,
    CONSTRAINT "PK_sys_user_role" PRIMARY KEY ("user_id", "role_id")
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('index', 'sqlite_autoindex_sys_user_role_1', 'sys_user_role', 17, null);
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'comm_setting', 'comm_setting', 19, 'CREATE TABLE "comm_setting" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_comm_setting" PRIMARY KEY AUTOINCREMENT,
    "name" TEXT NOT NULL,
    "ip" TEXT NOT NULL,
    "port" INTEGER NOT NULL,
    "connect_mode" TEXT NOT NULL,
    "check_cycle_ms" INTEGER NOT NULL,
    "timeout_ms" INTEGER NOT NULL,
    "retry_cycle_s" INTEGER NOT NULL,
    "status" TEXT NOT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'comm_setting_area', 'comm_setting_area', 18, 'CREATE TABLE "comm_setting_area" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_comm_setting_area" PRIMARY KEY AUTOINCREMENT,
    "setting_id" INTEGER NOT NULL,
    "area_type" TEXT NOT NULL,
    "name" TEXT NULL,
    "position" TEXT NULL,
    "quantity" INTEGER NOT NULL,
    "sort" INTEGER NOT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'recipe', 'recipe', 20, 'CREATE TABLE "recipe" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_recipe" PRIMARY KEY AUTOINCREMENT,
    "recipe_name" TEXT NOT NULL UNIQUE,
    "upper_lami_tmp_stage1" INTEGER NOT NULL CHECK(upper_lami_tmp_stage1 BETWEEN 15 AND 130),
    "lower_lami_tmp_stage1" INTEGER NOT NULL CHECK(lower_lami_tmp_stage1 BETWEEN 15 AND 130),
    "upper_lami_tmp_stage2" INTEGER NOT NULL CHECK(upper_lami_tmp_stage2 BETWEEN 15 AND 130),
    "lower_lami_tmp_stage2" INTEGER NOT NULL CHECK(lower_lami_tmp_stage2 BETWEEN 15 AND 130),
    "membrane_width" INTEGER NOT NULL,
    "panel_out_tmp" INTEGER NOT NULL CHECK(panel_out_tmp BETWEEN 15 AND 100),
    "upper_tacking_tmp" INTEGER NOT NULL CHECK(upper_tacking_tmp BETWEEN 15 AND 100),
    "lower_tacking_tmp" INTEGER NOT NULL CHECK(lower_tacking_tmp BETWEEN 15 AND 100),
    "drv_speed" REAL NOT NULL CHECK(drv_speed BETWEEN 1 AND 5),
    "upper_tacking_pressure" REAL NOT NULL CHECK(upper_tacking_pressure BETWEEN 0 AND 5),
    "front_cu_pos" REAL NOT NULL CHECK(front_cu_pos BETWEEN 0 AND 49.9),
    "rear_cu_pos" REAL NOT NULL CHECK(rear_cu_pos BETWEEN 0 AND 12.9),
    "roller_pressure_stage1" REAL NOT NULL CHECK(roller_pressure_stage1 BETWEEN 0.3 AND 5.0),
    "roller_pressure_stage2" REAL NOT NULL CHECK(roller_pressure_stage2 BETWEEN 0.3 AND 5.0),
    "duration_film" REAL NOT NULL CHECK(duration_film BETWEEN 0.3 AND 7),
    "plate_thickness" REAL NOT NULL CHECK(plate_thickness BETWEEN 0.5 AND 8),
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
, program_content TEXT, ccode TEXT)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('index', 'sqlite_autoindex_recipe_1', 'recipe', 21, null);
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'plc_trigger', 'plc_trigger', 24, 'CREATE TABLE "plc_trigger" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_plc_trigger" PRIMARY KEY AUTOINCREMENT,
    "comm_setting_id" INTEGER NOT NULL,
    "device_code" TEXT NOT NULL,
    "address" TEXT NOT NULL,
    "trigger_type" TEXT NOT NULL,
    "trigger_mode" TEXT NOT NULL,
    "action_key" TEXT NULL,
    "name" TEXT NOT NULL,
    "status" TEXT NOT NULL,
    "create_by" TEXT NULL,
    "create_time" TEXT NULL,
    "update_by" TEXT NULL,
    "update_time" TEXT NULL,
    "remark" TEXT NULL
, alid INTEGER NULL, report_alarm TEXT NOT NULL DEFAULT ''0'', alarm_category INTEGER NOT NULL DEFAULT 0)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'secs_report_vid', 'secs_report_vid', 26, 'CREATE TABLE "secs_report_vid" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_secs_report_vid" PRIMARY KEY AUTOINCREMENT,
    "report_id" INTEGER NOT NULL,
    "vid_id" INTEGER NOT NULL,
    "sort" INTEGER NOT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'secs_ceid_report', 'secs_ceid_report', 28, 'CREATE TABLE "secs_ceid_report" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_secs_ceid_report" PRIMARY KEY AUTOINCREMENT,
    "ceid_id" INTEGER NOT NULL,
    "report_id" INTEGER NOT NULL,
    "sort" INTEGER NOT NULL
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'secs_instance', 'secs_instance', 29, 'CREATE TABLE secs_instance (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    name           TEXT    NOT NULL,
    ip_address     TEXT    NOT NULL DEFAULT ''0.0.0.0'',
    port           INTEGER NOT NULL DEFAULT 5000,
    device_id      INTEGER NOT NULL DEFAULT 0,
    connect_mode   TEXT    NOT NULL DEFAULT ''manual'',
    t3             INTEGER NOT NULL DEFAULT 45000,
    t5             INTEGER NOT NULL DEFAULT 10000,
    t6             INTEGER NOT NULL DEFAULT 5000,
    t7             INTEGER NOT NULL DEFAULT 10000,
    t8             INTEGER NOT NULL DEFAULT 5000,
    comm_setting_id INTEGER NULL,
    status         TEXT    NOT NULL DEFAULT ''0'',
    create_by      TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT
)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'secs_vid', 'secs_vid', 22, 'CREATE TABLE secs_vid (id INTEGER PRIMARY KEY AUTOINCREMENT, instance_id INTEGER NOT NULL, vid_type TEXT, vid_id INTEGER NOT NULL, name TEXT, data_type TEXT, source TEXT, plc_address TEXT, device_code TEXT, status TEXT, create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT, default_value TEXT)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'secs_report', 'secs_report', 27, 'CREATE TABLE secs_report (id INTEGER PRIMARY KEY AUTOINCREMENT, instance_id INTEGER NOT NULL, report_id INTEGER NOT NULL, name TEXT, status TEXT, create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'secs_ceid', 'secs_ceid', 30, 'CREATE TABLE secs_ceid (id INTEGER PRIMARY KEY AUTOINCREMENT, instance_id INTEGER NOT NULL, ceid INTEGER NOT NULL, name TEXT, status TEXT, create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'machine', 'machine', 25, 'CREATE TABLE machine (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    comm_setting_id INTEGER NOT NULL,
    create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT, name TEXT NOT NULL DEFAULT '''')');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_notice', 'sys_notice', 31, 'CREATE TABLE sys_notice (notice_id INTEGER PRIMARY KEY AUTOINCREMENT, notice_title TEXT NOT NULL DEFAULT '''', notice_type TEXT NOT NULL DEFAULT ''0'', notice_content TEXT, source TEXT, status TEXT NOT NULL DEFAULT ''0'', create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT)');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'sys_notice_read', 'sys_notice_read', 32, 'CREATE TABLE sys_notice_read (notice_id INTEGER NOT NULL, user_id INTEGER NOT NULL, read_time TEXT, PRIMARY KEY (notice_id, user_id))');
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('index', 'sqlite_autoindex_sys_notice_read_1', 'sys_notice_read', 33, null);
INSERT INTO sqlite_master (type, name, tbl_name, rootpage, sql) VALUES ('table', 'secs_sxfy', 'secs_sxfy', 34, 'CREATE TABLE secs_sxfy (id INTEGER PRIMARY KEY AUTOINCREMENT, instance_id INTEGER NOT NULL, stream INTEGER NOT NULL, function INTEGER NOT NULL, allow_offline INTEGER NOT NULL DEFAULT 0, allow_online_local INTEGER NOT NULL DEFAULT 0, allow_online_remote INTEGER NOT NULL DEFAULT 0, status TEXT NOT NULL DEFAULT ''0'', name TEXT NOT NULL DEFAULT '''', create_by TEXT, create_time TEXT, update_by TEXT, update_time TEXT, remark TEXT)');
