# mcpx_template_project 项目介绍

> 版本：v0.9.0 ｜ 更新：2026-11-01
> 项目类型：WPF 桌面工业上位机（Windows）
> 适用场景：半导体 / PCB 等自动化设备的 PLC**（三菱 Q系列）** 通讯、SECS/GEM 通讯与配方管理

---

## 1. 项目概述

`mcpx_template_project` 是一套基于 **C# / WPF / .NET 8** 的工业设备上位机（EQP，Equipment）软件，主要解决三类问题：

| 能力              | 说明                                                         |
| ----------------- | ------------------------------------------------------------ |
| **PLC 通讯**      | 通过三菱 MC 协议（McpX）读写设备点位（D/W/M/X/Y… 软元件），支持批量读、单点写、连接管理 |
| **SECS/GEM 通讯** | 以 Equipment/Passive 端角色，通过 HSMS 与 MES/Host 上位机通讯，覆盖 S1/S2/S5/S6/S7/S10 核心消息 |
| **业务管理**      | 配方（Recipe）管理、警报/事件统一触发器、用户权限、日志、看板监控等 |

软件内置「注解驱动」开发模型：业务开发者只需在实体属性上打 `[PlcRead]` / `[PlcWrite]` / `[SecsStream]` / `[SecsFunction]` 等特性，底层框架自动完成点位扫描、报文编解码、消息路由与握手，无需编写通讯样板代码。

---

## 2. 技术选型

### 2.1 运行时与框架

| 类别     | 选型                                              | 版本                         | 说明                          |
| -------- | ------------------------------------------------- | ---------------------------- | ----------------------------- |
| 语言     | C#                                                | 12                           | —                             |
| 目标框架 | .NET                                              | `net8.0-windows10.0.19041.0` | Win10 2004 及以上             |
| UI       | WPF                                               | —                            | `WinExe` 桌面应用             |
| UI 主题  | iNKORE.UI.WPF.Modern                              | 0.10.2.1                     | Fluent 现代化界面             |
| MVVM     | CommunityToolkit.Mvvm                             | 8.4.2                        | 命令、消息、可观察对象        |
| 依赖注入 | Microsoft.Extensions.DependencyInjection          | 8.0                          | 全局 IOC 容器                 |
| 配置     | Microsoft.Extensions.Configuration(.Json/.Binder) | 8.0                          | `appsettings.json` 强类型绑定 |

### 2.2 数据与持久层

| 类别   | 选型                           | 版本  | 说明                                       |
| ------ | ------------------------------ | ----- | ------------------------------------------ |
| ORM    | Entity Framework Core          | 8.0.8 | Code First + `IDbContextFactory`           |
| 数据库 | SQLite                         | 8.0.8 | 单文件本地库，`Microsoft.Data.Sqlite` 驱动 |
| 建表   | DatabaseInitializer + SQL 脚本 | —     | 启动自动建表 + 种子数据                    |

### 2.3 通讯协议

| 类别     | 选型                    | 版本  | 说明                                       |
| -------- | ----------------------- | ----- | ------------------------------------------ |
| PLC 通讯 | McpX                    | 0.9.1 | 三菱 Q 系列 MC 协议（3E/4E、二进制/ASCII） |
| SECS/GEM | Secs4Net + Secs4Net.Sml | 3.1.0 | HSMS-SS Passive 端，SML 报文解析           |

### 2.4 横切与辅助

| 类别     | 选型                                         | 版本                  | 说明                      |
| -------- | -------------------------------------------- | --------------------- | ------------------------- |
| 日志     | Serilog (+Sinks.File/Settings.Configuration) | 4.4.0                 | 控制台 + 按日滚动文件     |
| AOP      | Fody + MethodBoundaryAspect.Fody             | 6.9.3 / 2.0.150       | 方法级日志切面            |
| 密码     | BCrypt.Net-Next                              | 4.2.0                 | 密码哈希                  |
| 图表     | LiveChartsCore.SkiaSharpView.WPF             | 2.0.5                 | 看板图表                  |
| Office   | NPOI / EPPlus**（商用需付费）** / DocX       | 2.8.0 / 8.6.3 / 5.2.0 | Excel 导入导出、Word 导出 |
| 3D       | OpenTK.GLWpfControl                          | 4.3.6                 | OpenGL 控件               |
| 系统信息 | System.Management                            | 8.0.0                 | 硬件信息采集              |
| GIF      | XamlAnimatedGif                              | —                     | 动画资源（本地 dll 引用） |

---

## 3. 系统功能

### 3.1 系统管理（Admin）

| 模块     | 页面             | 说明                                                         |
| -------- | ---------------- | ------------------------------------------------------------ |
| 登录     | `Login`          | 账号密码登录，BCrypt 校验，含授权校验（LicenseCheck，提供了基本的工具接口不可直接用于生产） |
| 用户管理 | `PageUser`       | 用户 CRUD、分配角色/岗位                                     |
| 角色管理 | `PageRole`       | 角色 CRUD、菜单权限分配                                      |
| 菜单管理 | `PageMenu`       | 菜单/路由/权限点维护，树形结构                               |
| 部门管理 | `PageDept`       | 部门树 CRUD                                                  |
| 岗位管理 | `PagePost`       | 岗位 CRUD                                                    |
| 字典管理 | 字典数据         | 数据字典维护                                                 |
| 通知公告 | `PageNotice`     | 公告发布、已读状态                                           |
| 操作日志 | `PageOperLog`    | 操作日志查询、详情                                           |
| 个人中心 | `PageUserCenter` | 个人资料、密码修改                                           |

> 权限模型：`用户 - 角色 - 菜单` 三级 RBAC，实现按钮/菜单级权限控制。

#### 系统基本功能介绍

##### 3.1.1 登录页（账号：admin 密码:1）

![Login](docs/imgs/Login.png)

##### 3.1.2 系统管理

此部分在应用层面大量参考[若依](https://doc.ruoyi.vip/ruoyi/)不过多赘述，因本项目为单机桌面应用未完全实现（如Security部分）。菜单管理输入完整限定名，直接加载组件，不再做拼接。icon请参考[iNKORE官网](https://www.inkore.net/)，或在微软商店下载iNKORE.UI.WPF.Modern Gallery即可，版本可使用的icon需要查看`mcpx_template_project.Tools.SegoeFluentIconsDump`

![](docs/imgs/menu.png)

##### 3.1.2 日志管理

基于Fody的AOP实现，使用范例如下：

```c#
[Log(Title = "用户登录", BusinessType = BusinessType.Grant)]
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
```

![LogAOP](docs/imgs/LogAOP.png)

### 3.2 业务功能（Business）

| 模块      | 页面/服务              | 说明                                           |
| --------- | ---------------------- | ---------------------------------------------- |
| 首页      | `PageHome`             | 系统首页导航                                   |
| 看板      | `PageDashboard`        | 数据看板（LiveCharts 图表）                    |
| 通讯设置  | `CommSettingWindow`    | PLC 通讯参数配置（IP/端口/帧类型/点位区域）    |
| PLC 监控  | `PlcMonitorWindow`     | 点位实时监控、读写调试                         |
| 配方管理  | `PagePNSelect`         | 配方 CRUD + 「写入配方」下发 PLC（含三态握手） |
| 警报      | `PageAlarm`            | 实时展示正在发生的警报，弹窗 + 声音 + 角标     |
| 触发器    | `PageTrigger`          | 警报/事件统一触发器配置，Excel 导入导出        |
| SECS 配置 | `PageSecsConfig`       | VID/Report/CEID/SxFy 权限/HCMD 配置            |
| SECS/GEM  | `SecsGemSettingWindow` | HSMS 监听参数、连接状态、多实例管理            |

### 3.3 核心通讯能力

**PLC（三菱 MC 协议）**

- 批量读取点位并缓存，周期刷新；单点写入。
- 帧类型/通信格式自动探测（3E 二进制 → 4E 二进制 → 3E ASCII → 4E ASCII）。
- 位/字软元件类型分派，内置错误码翻译（C200~C060）。
- DEBUG 下注入 `MockPlcCommunicationService`，无需真实 PLC 即可联调。

**SECS/GEM（HSMS Passive）**

- Equipment 端被动监听，Host 主动连接后双向建立通信（S1F13/S1F14）。
- 注解路由 `[SecsStream]`+`[SecsFunction]`，支持 S1/S2/S5/S6/S7/S10 与 S9 错误。
- 多设备管理器 `ISecsGemManager`，按 deviceKey 隔离引擎与状态（E30 Control State）。
- VID 双源取值：`db` 固定值 / `plc` 点位缓存，打通「PLC 采集 → SECS 上报」。

**警报与事件**

- 统一 `plc_trigger` 表，`trigger_type`（alarm/event）+ `trigger_mode`（on/change）驱动。
- 事件处理器 `IPlcEventHandler` + `[PlcEventHandler]` 注解，启动扫描注册，反射调用。

#### 3.3.1 PLC通讯模块功能

##### PLC通讯设定模块

项目支持读写多PLC设备实例：配置PLC通讯信息，并配置读写区域。架构设计请参考[plc-framework.md](./docs/plc-framework.md)

![](docs/imgs/comm_setting1.png)

> 启动连线模式：自动 - 在用户登录后自动建立连接。
>
> AI/AO/DI/DO：在本项目中未实际作用，设计之初考虑到不同品牌、不同型号PLC设备代号不同而引入作为中间层解耦，使开发者无需关注各PLC设备代号定义，目前仅支持三菱PLC Q系列，故仅作为概念存在。

![](docs/imgs/comm_setting2.png)

##### 实时监控模块：辅助开发者监控/写PLC调试

![](docs/imgs/monitor.png)

##### 触发器

该模块包含两个业务模块：事件和警报。于作者理解事件警报在交握层本质一致故整合一起。使用方式如下：

1. 实现IPlcEventHandler接口，并打PlcEventHandler注解。

   ```CC
   /// <summary>
   /// 示例事件处理器：触发时写一条操作日志（演示 IPlcEventHandler 的用法与注解维护）。
   /// 实际项目中可按业务需求新增更多处理器，每个处理器用 [PlcEventHandler] 特性声明 Key 与显示名，
   /// 配点界面的下拉框会自动读取这些处理器。
   /// </summary>
   /// <author>Rudeus</author>
   [PlcEventHandler("log_event", "写入操作日志", Description = "触发时记录一条日志")]
   public class LogPlcEventHandler : IPlcEventHandler
   {
       private readonly ILogService _logService;
   
       public LogPlcEventHandler(ILogService logService)
       {
           _logService = logService;
       }
   
       public Task HandleAsync(PlcEventContext context)
       {
           _logService.LogInformation(
               "[PLC事件] {Name} 触发：点位 {Address}，当前值 {Value}，时间 {Time}",
               context.Name, context.FullAddress, context.CurrentValue, context.TriggerTime);
   
           return Task.CompletedTask
       }
   }
   ```

2. 启动项目即可配置事件处理方法

![](docs/imgs/trigger.png)

> 该模块架构设计请移步[plc-trigger-alarm-event](./docs/plc-trigger-alarm-event.md)

##### 面向对象式读写PLC（项目核心设计）

详细请参阅[plc-framework](./docs/plc-framework.md)

#### 3.3.2 SECS/GEM通讯模块功能

本项目支持多SECS/GEM实例，一个SECS/GEM实例仅对应一台设备。

##### 通讯设定

![](docs/imgs/SECSconfig.png)

##### SxFy配置

控制层打SecsPermission即可做前置增强，在配置界面设定可执行的状态，即可在方法执行前获取当前连线状态并判断是否有权限执行。详见[secs-gem-framework](./secs-gem-framework.md)。使用范例如下：

```CC
/// <summary>S1F3</summary>
[SecsFunction(3)]
[SecsPermission]
public async Task<SecsMessage?> S1F3(SecsMessage primaryMsg, CancellationToken ct)
{
    // 零长度列表表示查询全部
    var data = primaryMsg.SecsItem;
    if (data != null && data.Format != SecsFormat.List)
        return null; // 结构非法 → 由调用方回 S9F7

    var svids = ParseU4List(data);

    var values = await _statusService.GetStatusValuesAsync(InstanceId, svids);

    // S1F4 结构：L,n [SV...]，不存在用空列表项占位
    var items = values.Select(v => v.Exists && v.Value != null ? v.Value! : Item.L()).ToArray();
    return new SecsMessage(1, 4, replyExpected: false)
    {
        Name = "SelectedEquipmentStatusData",
        SecsItem = Item.L(items)
    };
}
```

![](docs/imgs/SxFy.png)

------

## 4. 环境部署

### 4.1 前置要求

| 项       | 要求                                                         |
| -------- | ------------------------------------------------------------ |
| 操作系统 | Windows 10 19041（2004）及以上 / Windows 11                  |
| .NET SDK | .NET 8.0 SDK（含 `net8.0-windows` 工作负载）                 |
| IDE      | Visual Studio 2022（17.8+，含「.NET 桌面开发」工作负载）或 `dotnet` CLI |
| 其他     | 首次构建需联网还原 NuGet 包                                  |

### 4.2 获取与还原

```bash
# 1. 进入解决方案目录
cd D:\code\C#\mcpx_template_project

# 2. 还原 NuGet 依赖
dotnet restore mcpx_template_project.sln

# 3. 构建（Debug）
dotnet build mcpx_template_project.sln -c Debug
```

> `Libs\XamlAnimatedGif.dll` 为本地引用，需保留 `Libs` 目录。

### 4.3 运行

```bash
# 直接运行（Debug）
dotnet run --project mcpx_template_project\mcpx_template_project.csproj
```

或在 Visual Studio 中：打开 `mcpx_template_project.sln` → 设置 `mcpx_template_project` 为启动项目 → F5。

### 4.4 发布

发布后需保证以下文件随程序分发（`.csproj` 已配置 `CopyToOutputDirectory`）：

- `resouces\appsettings.json`（配置）
- `resouces\Sql\*.sql`（建表脚本）
- `resouces\csunresource\**`（FastCSUN所需的资源，因`PrintFiles.vbs`为对接打印机功能所保留）

### 4.5 配置说明（`resouces/appsettings.json`）

```jsonc
{
  "ConnectionStrings": {
    "SqliteDb": "Data Source=|DataDirectory|\\mcpx_template_project.db"   // SQLite 库文件
  },
  "SecsGem": {          // SECS/GEM 默认监听参数，以数据库配置优先
    "IpAddress": "0.0.0.0",
    "Port": 5000,
    "DeviceId": 0,
    "ConnectMode": "manual",
    "T3": 45000, "T5": 10000, "T6": 5000, "T7": 10000, "T8": 5000
  },
  "Serilog": { /* 日志配置：控制台 + logs/log-*.txt 按日滚动 */ }
}
```

### 4.6 首次启动

1. 启动后自动执行 `DatabaseInitializer.Initialize`：按 `resouces/Sql` 脚本建表并写入种子数据（含默认管理员账号（admin，1）、菜单）。
2. 弹出登录窗口，登录成功后进入主界面。
3. 在「通讯设置」配置 PLC 参数；在「SECS/GEM 设置」配置监听并启动。
4. DEBUG 构建下 PLC 通讯走 Mock 实现，可离线验证完整业务流程。

---

## 5. 文件结构

```
mcpx_template_project.sln
docs/                                  # 设计文档
  ├─ plc-framework.md                  # PLC 通讯框架设计
  ├─ secs-gem-framework.md             # SECS/GEM 通讯框架设计
  ├─ plc-trigger-alarm-event.md        # 警报/事件统一触发器设计
  └─ uml/                              # PlantUML / Mermaid 图
mcpx_template_project/               # 主项目
  ├─ App.xaml(.cs)                     # 入口：加载配置→注册DI→建表→登录
  ├─ MainWindow.xaml(.cs)              # 主窗口
  ├─ Assets/                           # 图片/图标资源
  ├─ Configation/                      # 配置与数据层初始化
  │   ├─ AppConfiguration.cs           # 配置加载
  │   ├─ EFDbContext.cs                # EF Core 上下文
  │   └─ DatabaseInitializer.cs        # 建表 + 种子数据
  ├─ Converters/                       # XAML 值转换器
  ├─ Core/                             # 核心抽象（无 UI 依赖）
  │   ├─ Logging/                      # 日志服务接口/实现
  │   ├─ Plc/                          # PlcFieldCatalog / PlcPointAccessor
  │   ├─ Scanner/                      # PlcPointAttributeScanner 注解扫描
  │   ├─ Context/                      # ISecsContext（AsyncLocal）
  │   ├─ Handler/                      # IPlcEventHandler
  │   └─ SecsRoute/                    # SecsRouter / SecsRoute / HcmdRegistry
  ├─ Controller/SecsGem/               # S1~S10 消息控制器 + S9 错误工厂
  ├─ DependencyInjection/              # DI 注册（Service/View/Db/Config/Admin/Business）
  ├─ Domain/                           # 领域层
  │   ├─ Enums/                        # 枚举（PlcDevice/PlcWriteRole/SecsControlState…）
  │   ├─ Entities/
  │   │   ├─ System/                   # SysUser/SysRole/SysMenu/SysDept/SysPost/SysDictData/SysNotice/SysOperLog
  │   │   └─ Business/                 # CommSetting/Secs*/Machine/Recipe/PlcTrigger
  │   ├─ Model/                        # Plc / Secs 领域模型
  │   └─ VO/                           # 视图对象
  ├─ Helper/                           # 工具类（Excel/AES/硬件ID/PLC地址…）
  ├─ Service/
  │   ├─ Admin/                        # 认证/用户/角色/菜单/部门/岗位/公告/日志/授权
  │   └─ Business/                     # CommSetting/Plc/Recipe/Secs/Machine/Trigger
  ├─ ViewModels/                       # MVVM 视图模型
  │   ├─ Admin/
  │   ├─ Business/
  │   └─ Common/                       # PagedViewModelBase / RelayCommand
  ├─ Views/Pages/
  │   ├─ Admin/                        # Login/User/Role/Menu/Dept/Post/Notice/OperLog/UserCenter
  │   └─ Business/                     # Home/Dashboard/CommSetting/PNSelect/Trigger/Secs/SecsGem/Monitor
  ├─ resouces/
  │   ├─ Sql/                          # 建表 + 种子 SQL（*.sql）
  │   ├─ csunresource/                 # 订单/估算模板资源（可直接删除）
  │   ├─ appsettings.json              # 全局配置
  │   └─ FodyWeavers.xml               # AOP 织入配置
  └─ Libs/                             # 本地第三方 dll（XamlAnimatedGif）
```

> 删除Business下文件，可作为桌面后台权限系统模板使用。

---

## 6. 分层架构

```
┌─────────────────────────────────────────────────────────┐
│ 表示层  ViewModels / Views（WPF + iNKORE 主题 + MVVM）    │
├─────────────────────────────────────────────────────────┤
│ 控制器层 Controller/SecsGem（[SecsStream]/[SecsFunction]）│
├─────────────────────────────────────────────────────────┤
│ 服务层   Service/Admin + Service/Business（业务逻辑）      │
├─────────────────────────────────────────────────────────┤
│ 核心层   Core（注解扫描/路由/访问器/上下文）               │
├─────────────────────────────────────────────────────────┤
│ 领域层   Domain（实体/枚举/模型/VO）                      │
├─────────────────────────────────────────────────────────┤
│ 通讯引擎 McpX（PLC） + Secs4Net（SECS/GEM）               │
├─────────────────────────────────────────────────────────┤
│ 持久层   EF Core + SQLite（本地单文件库）                  │
└─────────────────────────────────────────────────────────┘
```

---

## 7. 关键设计原则

1. **注解驱动，零样板**：PLC 读写走 `[PlcRead]/[PlcWrite]`，SECS 路由走 `[SecsStream]/[SecsFunction]`。
2. **读写分离**：PLC 读/写注解职责清晰，写点位用 `PlcWriteRole`（Value/Trigger/Reply）自动分类。
3. **类型安全**：设备代号用 `PlcDevice` 枚举，杜绝魔法字符串。
4. **可测试**：`IPlcCommunicationService` 抽象出真实/Mock 双实现，`#if DEBUG` 自动切换。
5. **数据表驱动**：SECS VID/Report/CEID/SxFy 全部落库，运行期可视化配置。
6. **多实例隔离**：`ISecsGemManager` + `Func` 工厂 + `ISecsContext`（AsyncLocal）支持多设备并发。
7. **性能**：反射扫描结果缓存 + 表达式树编译 getter，运行期零反射。

---

## 8. 相关文档

| 文档                  | 路径                              |
| --------------------- | --------------------------------- |
| PLC 通讯框架设计      | `docs/plc-framework.md`           |
| SECS/GEM 通讯框架设计 | `docs/secs-gem-framework.md`      |
| 警报与事件统一触发器  | `docs/plc-trigger-alarm-event.md` |
| UML 图集              | `docs/uml/`                       |
