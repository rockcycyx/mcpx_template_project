using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using mcpx_template_project.Domain.Model.Secs;
using SecsInstanceEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsInstance;

namespace mcpx_template_project.Configation
{
    /// <summary>
    /// SECS/GEM 通讯配置持久化：负责从 appsettings.json 的 "SecsGem" 段加载初始配置，
    /// 并在用户保存时将配置写回 JSON（保持其它配置段不变）。
    /// 多实例改造后，实例配置以 secs_instance 表为准（见 <see cref="ISecsInstanceService"/>），
    /// 本类提供实例实体 ↔ <see cref="SecsGemConfiguration"/> 的转换，供通讯层启动引擎时使用。
    /// </summary>
    /// <author>Rudeus</author>
    public sealed class SecsGemConfigStore
    {
        private readonly string _filePath;

        public SecsGemConfigStore()
        {
            // appsettings.json 运行时位于工作目录下的 resouces 子目录
            _filePath = Path.Combine(Directory.GetCurrentDirectory(), "resouces", "appsettings.json");
        }

        /// <summary>
        /// 从 <see cref="SecsInstanceEntity"/> 构造通讯配置（多实例场景下启动引擎用）。
        /// </summary>
        public static SecsGemConfiguration ToConfiguration(SecsInstanceEntity instance)
        {
            return new SecsGemConfiguration
            {
                IpAddress = instance.IpAddress,
                Port = instance.Port,
                DeviceId = instance.DeviceId,
                ConnectMode = instance.ConnectMode,
                T3 = instance.T3,
                T5 = instance.T5,
                T6 = instance.T6,
                T7 = instance.T7,
                T8 = instance.T8,
            };
        }

        /// <summary>
        /// 从 appsettings.json 的 "SecsGem" 段加载初始配置（文件缺失或段缺失时用默认值）。
        /// </summary>
        public void Load(SecsGemConfiguration config)
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                var doc = JsonNode.Parse(File.ReadAllText(_filePath));
                if (doc?["SecsGem"] is not JsonObject sec) return;

                if (sec["IpAddress"]?.GetValue<string>() is string ip) config.IpAddress = ip;
                if (sec["Port"]?.GetValue<int>() is int port) config.Port = port;
                if (sec["DeviceId"]?.GetValue<ushort>() is ushort deviceId) config.DeviceId = deviceId;
                if (sec["ConnectMode"]?.GetValue<string>() is string mode) config.ConnectMode = mode;
                if (sec["T3"]?.GetValue<int>() is int t3) config.T3 = t3;
                if (sec["T5"]?.GetValue<int>() is int t5) config.T5 = t5;
                if (sec["T6"]?.GetValue<int>() is int t6) config.T6 = t6;
                if (sec["T7"]?.GetValue<int>() is int t7) config.T7 = t7;
                if (sec["T8"]?.GetValue<int>() is int t8) config.T8 = t8;
            }
            catch
            {
                // 读取失败不影响启动，使用默认配置
            }
        }

        /// <summary>
        /// 将配置写回 appsettings.json 的 "SecsGem" 段，保持其它段不变。
        /// </summary>
        public void Save(SecsGemConfiguration config)
        {
            JsonObject root;
            try
            {
                root = File.Exists(_filePath)
                    ? JsonNode.Parse(File.ReadAllText(_filePath)) as JsonObject ?? new JsonObject()
                    : new JsonObject();
            }
            catch
            {
                root = new JsonObject();
            }

            root["SecsGem"] = new JsonObject
            {
                ["IpAddress"] = config.IpAddress,
                ["Port"] = config.Port,
                ["DeviceId"] = config.DeviceId,
                ["ConnectMode"] = config.ConnectMode,
                ["T3"] = config.T3,
                ["T5"] = config.T5,
                ["T6"] = config.T6,
                ["T7"] = config.T7,
                ["T8"] = config.T8,
            };

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, json);
        }
    }
}
