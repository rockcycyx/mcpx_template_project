using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.Service.Business.CommSetting;
using mcpx_template_project.Service.Business.CommSetting.impl;
using mcpx_template_project.Service.Business.Plc;
using mcpx_template_project.Service.Business.Recipe;
using mcpx_template_project.Service.Business.Recipe.impl;
using mcpx_template_project.Service.Business.Plc.Impl;
using mcpx_template_project.Service.Business.Trigger;
using mcpx_template_project.Service.Business.Trigger.impl;
using mcpx_template_project.Service.Business.Secs;
using mcpx_template_project.Service.Business.Secs.impl;
using mcpx_template_project.Service.Business.Machine;
using mcpx_template_project.Service.Business.Machine.impl;
using mcpx_template_project.Controller.SecsGem;
using mcpx_template_project.Core.Plc;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Core.SecsRoute;

namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 业务模块服务注册器
    /// </summary>
    /// <author>Rudeus</author>
    public static class BusinessServiceRegister
    {
        public static void AddBusinessModuleServices(this IServiceCollection services)
        {
            services.AddSingleton<IFileService, FileServiceImpl>();

            services.AddSingleton<ICommSettingService, CommSettingServiceImpl>();

            #if DEBUG
            // Debug 构建使用 Mock 实现，便于在没有真实PLC时预览实时监控页面
            services.AddSingleton<IPlcCommunicationService, MockPlcCommunicationService>();
            #else
            // Release 构建使用真实 PLC 通讯引擎（三菱Q系列MC协议）
            services.AddSingleton<IPlcCommunicationService, PlcCommunicationService>();
            #endif

            // 通用 PLC 对象读写门面（面向对象读写 PLC 点位，含注解扫描与握手协议，屏蔽底层地址细节）
            services.AddSingleton<IPlcObjectService, PlcObjectService>();

            // PLC 自动连线服务（登录后自动连接 ConnectMode=auto 的通讯设定）
            services.AddSingleton<IPlcAutoConnectService, PlcAutoConnectService>();

            // SECS/GEM 消息路由（类 SpringMVC）：注册各 Stream Controller + 路由器
            services.AddSingleton<ISecsController, S1Controller>();
            services.AddSingleton<ISecsController, S2Controller>();
            services.AddSingleton<ISecsController, S5Controller>();
            services.AddSingleton<ISecsController, S6Controller>();
            services.AddSingleton<ISecsController, S7Controller>();
            services.AddSingleton<ISecsController, S10Controller>();

            // SECS/GEM 消息执行权限校验（SxFy 表 + [SecsPermission] 注解前置增强）
            services.AddSingleton<ISecsPermissionService, SecsPermissionService>();

            services.AddSingleton<SecsRouter>();

            // Host 指令（HCMD）注册表：扫描 IHostCommandHandler 中带 [Hcmd] 注解的方法
            services.AddSingleton<HcmdRegistry>();

            // Host 指令处理器（示例：PP_SELECT 工艺程序选择等）
            services.AddSingleton<IHostCommandHandler, HostCommandHandler>();

            // SECS/GEM 通讯引擎（Equipment/Passive 端，单设备实例，由工厂按需创建）
            services.AddTransient<SecsGemCommunicationService>();
            services.AddSingleton<Func<ISecsGemCommunicationService>>(
                sp => () => sp.GetRequiredService<SecsGemCommunicationService>());

            // SECS/GEM 多设备管理器（每个设备一个引擎实例）
            services.AddSingleton<ISecsGemManager, SecsGemManager>();
            services.AddSingleton<ISecsGemAutoStartService, SecsGemAutoStartService>();

            // SECS 实例上下文（AsyncLocal，消息分发时注入当前实例 ID）
            services.AddSingleton<ISecsContext, SecsContext>();

            // 配方（Recipe）业务：CRUD
            services.AddSingleton<IRecipeService, RecipeServiceImpl>();

            // PLC 触发器（警报 + 事件）业务：CRUD + Excel 导入导出
            services.AddSingleton<IPlcTriggerService, PlcTriggerServiceImpl>();

            // 事件处理器注册表：扫描所有 IPlcEventHandler 实现，供配点界面下拉框与运行时反射调用
            services.AddSingleton<PlcEventHandlerRegistry>();

            // 注册内置事件处理器（可扩展更多业务处理器）
            services.AddSingleton<LogPlcEventHandler>();

            // PLC 触发器后台监控服务：单线程轮询缓存，统一处理警报弹窗与事件反射调用
            services.AddSingleton<IPlcTriggerMonitorService, PlcTriggerMonitorService>();

            // SECS/GEM 实例业务服务（多实例数据表驱动）
            services.AddSingleton<ISecsInstanceService, SecsInstanceServiceImpl>();

            // SECS/GEM VID 业务服务（数据表驱动）
            services.AddSingleton<ISecsVidService, SecsVidServiceImpl>();

            // SECS/GEM Report 业务服务（含成员 VID 多选）
            services.AddSingleton<ISecsReportService, SecsReportServiceImpl>();

            // SECS/GEM 收集事件（CEID/Event）业务服务（含成员 Report 多选）
            services.AddSingleton<ISecsCeidService, SecsCeidServiceImpl>();

            // SECS/GEM SxFy 权限表业务服务（数据表驱动，配合 [SecsPermission] 前置增强）
            services.AddSingleton<ISecsSxFyService, SecsSxFyServiceImpl>();

            // SECS/GEM 设备状态机（Communication/Control State Model，按实例隔离，内存态）
            services.AddSingleton<ISecsStateService, SecsStateService>();

            // SECS/GEM VID 值读取门面（db 固定值 / PLC 点位缓存 → SecsItem）
            services.AddSingleton<ISecsValueReader, SecsValueReader>();

            // SECS/GEM Stream 业务服务（Controller 仅校验，业务实现全部下沉到这里）
            services.AddSingleton<IS1StatusService, S1StatusService>();
            services.AddSingleton<IS2EquipmentService, S2EquipmentService>();
            services.AddSingleton<IS5AlarmService, S5AlarmService>();
            services.AddSingleton<IS6DataCollectionService, S6DataCollectionService>();
            services.AddSingleton<IS7ProcessProgramService, S7ProcessProgramService>();
            services.AddSingleton<IS10TerminalService, S10TerminalService>();

            // SECS/GEM Trace 采样上报服务（S2F23 定义 Trace，后台按 DSPER 采样上报 S6F1）
            services.AddSingleton<IS2TraceService, SecsTraceService>();

            // PLC 字段目录（[PlcRead] 注解字段缓存，供 SECS VID 界面选择，不落库）
            services.AddSingleton<PlcFieldCatalog>();

            // 机台业务：machine 表 CRUD + 后台 PLC 轮询（首页机台监控数据源）
            services.AddSingleton<IMachineService, MachineServiceImpl>();
            services.AddSingleton<MachinePlcPollService>();

        }
    }
}
