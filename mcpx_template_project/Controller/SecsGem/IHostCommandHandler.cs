namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// Host 指令处理器标记接口。
    /// 实现此接口的类中带 [Hcmd] 注解的方法，将被 <see cref="Core.SecsRoute.HcmdRegistry"/>
    /// 扫描注册为可被 Host 通过 S2F41 调用的命令。实现类需注册为单例（供运行时解析实例）。
    /// </summary>
    public interface IHostCommandHandler
    {
    }
}
