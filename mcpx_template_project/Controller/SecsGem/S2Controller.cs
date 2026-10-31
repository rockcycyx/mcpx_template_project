using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;

namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// Stream 2（设备控制与诊断）消息处理器。
    /// 职责：解析并校验入参结构，合法则转发 <see cref="IS2EquipmentService"/>，再组装 SecsMessage 回复。
    /// 业务动作（常量读写、时间、Trace、报表、远程命令、Spooling、Limit）全部下沉到 Service。
    /// </summary>
    [SecsStream(2)]
    public class S2Controller : ISecsController
    {
        private readonly IS2EquipmentService _equipmentService;
        private readonly ISecsContext _secsContext;

        public S2Controller(IS2EquipmentService equipmentService, ISecsContext secsContext)
        {
            _equipmentService = equipmentService;
            _secsContext = secsContext;
        }

        private long InstanceId => _secsContext.CurrentInstanceId ?? 0;

        /// <summary>S2F13 Equipment Constant Request → S2F14 Equipment Constant Data</summary>
        [SecsFunction(13)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F13(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List) return null;

            var ecids = ParseIdList(data);
            var values = await _equipmentService.GetConstantValuesAsync(InstanceId, ecids);

            // S2F14 = L,n [ECV...]；不存在用空列表项占位
            var items = values.Select(v => v.Exists && v.Value != null ? v.Value! : Item.L()).ToArray();
            return new SecsMessage(2, 14, replyExpected: false)
            {
                Name = "EquipmentConstantData",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>S2F15 New Equipment Constant Send → S2F16 New Equipment Constant Acknowledge</summary>
        [SecsFunction(15)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F15(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S2F15 = L,n [L,2 [ECID, ECV]...]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List) return null;

            var dict = new Dictionary<int, string>();
            foreach (var item in data.Items)
            {
                if (item.Format != SecsFormat.List || item.Items.Length != 2) return null;
                var ecidItem = item.Items[0];
                var ecvItem = item.Items[1];
                if (ecidItem.Format is not (SecsFormat.U4 or SecsFormat.U2 or SecsFormat.U1)) return null;
                int ecid = (int)ReadId(ecidItem);
                dict[ecid] = ReadAsText(ecvItem);
            }

            byte eac = await _equipmentService.UpdateConstantsAsync(InstanceId, dict);
            return new SecsMessage(2, 16, replyExpected: false)
            {
                Name = "NewEquipmentConstantAcknowledge",
                SecsItem = Item.B(eac)
            };
        }

        /// <summary>S2F17 Date and Time Request → S2F18 Date and Time Data</summary>
        [SecsFunction(17)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F17(SecsMessage primaryMsg, CancellationToken ct)
        {
            return await Task.FromResult(new SecsMessage(2, 18, replyExpected: false)
            {
                Name = "DateAndTimeData",
                SecsItem = Item.A(_equipmentService.CurrentTimeText)
            });
        }

        /// <summary>S2F23 Trace Initialize Send → S2F24 Trace Initialize Acknowledge</summary>
        [SecsFunction(23)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F23(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S2F23 = L,5 [TRID, DSPER, TOTSMP, REPGSZ, L,n [SVID...]]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 5) return null;

            var tridItem = data.Items[0];
            var dsperItem = data.Items[1];
            var totSmpItem = data.Items[2];
            var repgszItem = data.Items[3];
            var svidList = data.Items[4];

            if (tridItem.Format != SecsFormat.U4) return null;
            if (dsperItem.Format != SecsFormat.ASCII) return null;
            if (totSmpItem.Format != SecsFormat.U4) return null;
            if (repgszItem.Format != SecsFormat.U4) return null;
            if (svidList.Format != SecsFormat.List) return null;

            uint trid = tridItem.FirstValue<uint>();
            string dsper = dsperItem.GetString() ?? string.Empty;
            uint totSmp = totSmpItem.FirstValue<uint>();
            uint repgsz = repgszItem.FirstValue<uint>();
            var svids = ParseIdList(svidList);

            byte ack = await _equipmentService.InitializeTraceAsync(InstanceId, trid, dsper, totSmp, repgsz, svids);
            return new SecsMessage(2, 24, replyExpected: false)
            {
                Name = "TraceInitializeAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S2F25 Loop Back Diagnosis Request → S2F26 Loop Back Diagnosis Data</summary>
        [SecsFunction(25)]
        public async Task<SecsMessage?> S2F25(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.Binary) return null;

            var echoed = _equipmentService.Echo(data.GetMemory<byte>().ToArray());
            return await Task.FromResult(new SecsMessage(2, 26, replyExpected: false)
            {
                Name = "LoopBackDiagnosisData",
                SecsItem = Item.B(echoed)
            });
        }

        /// <summary>S2F29 Equipment Constant Name List Request → S2F30 Equipment Constant Name List</summary>
        [SecsFunction(29)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F29(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List) return null;

            var ecids = ParseIdList(data);
            var names = await _equipmentService.GetConstantNamesAsync(InstanceId, ecids);

            // S2F30 = L,n [L,6 [ECID, ECNAME, ECMIN, ECMAX, ECDEF, UNITS]]
            var items = names.Select(n => Item.L(
                Item.U4((uint)n.Ecid),
                Item.A(n.Name ?? string.Empty),
                Item.A(n.Min ?? string.Empty),
                Item.A(n.Max ?? string.Empty),
                Item.A(n.Default ?? string.Empty),
                Item.A(n.Units ?? string.Empty))).ToArray();

            return new SecsMessage(2, 30, replyExpected: false)
            {
                Name = "EquipmentConstantNameList",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>S2F31 Date and Time Send → S2F32 Date and Time Acknowledge</summary>
        [SecsFunction(31)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F31(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.ASCII) return null;

            byte ack = await _equipmentService.SetTimeAsync(InstanceId, data.GetString() ?? string.Empty);
            return new SecsMessage(2, 32, replyExpected: false)
            {
                Name = "DateAndTimeAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S2F33 Define Report → S2F34 Define-Report Acknowledge</summary>
        [SecsFunction(33)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F33(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S2F33 = L,2 [DATAID, L,a [L,2 [RPTID, L,b [VID...]]...]]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;
            var reportList = data.Items[1];
            if (reportList.Format != SecsFormat.List) return null;

            var reports = new List<(int Rptid, IReadOnlyList<int> Vids)>();
            foreach (var report in reportList.Items)
            {
                if (report.Format != SecsFormat.List || report.Items.Length != 2) return null;
                int rptid = (int)ReadId(report.Items[0]);
                var vidList = report.Items[1];
                if (vidList.Format != SecsFormat.List) return null;
                reports.Add((rptid, ParseIdList(vidList)));
            }

            byte ack = await _equipmentService.DefineReportAsync(InstanceId, reports);
            return new SecsMessage(2, 34, replyExpected: false)
            {
                Name = "DefineReportAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S2F35 Link Event Report → S2F36 Link Event Report Acknowledge</summary>
        [SecsFunction(35)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F35(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;
            var eventList = data.Items[1];
            if (eventList.Format != SecsFormat.List) return null;

            var links = new List<(int Ceid, IReadOnlyList<int> Rptids)>();
            foreach (var ev in eventList.Items)
            {
                if (ev.Format != SecsFormat.List || ev.Items.Length != 2) return null;
                int ceid = (int)ReadId(ev.Items[0]);
                var rptidList = ev.Items[1];
                if (rptidList.Format != SecsFormat.List) return null;
                links.Add((ceid, ParseIdList(rptidList)));
            }

            byte ack = await _equipmentService.LinkEventReportAsync(InstanceId, links);
            return new SecsMessage(2, 36, replyExpected: false)
            {
                Name = "LinkEventReportAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S2F37 Enable/Disable Event Report → S2F38 Enable/Disable Event Report Acknowledge</summary>
        [SecsFunction(37)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F37(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S2F37 = L,2 [CEED B, L,n [CEID...]]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;
            var ceedItem = data.Items[0];
            var ceidList = data.Items[1];
            if (ceedItem.Format != SecsFormat.Boolean && ceedItem.Format != SecsFormat.Binary) return null;
            if (ceidList.Format != SecsFormat.List) return null;

            bool enable = ceedItem.Format == SecsFormat.Boolean
                ? ceedItem.FirstValue<bool>()
                : ceedItem.FirstValue<byte>() != 0;
            var ceids = ParseIdList(ceidList);

            byte ack = await _equipmentService.EnableDisableEventAsync(InstanceId, enable, ceids);
            return new SecsMessage(2, 38, replyExpected: false)
            {
                Name = "EnableDisableEventReportAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S2F39 Multi-block Inquire → S2F40 Multi-block Grant（本项目不分块，直接授权）</summary>
        [SecsFunction(39)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F39(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S2F39 = L,2 [DATAID, DATALENGTH]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;

            // 授权（GRANT B, 0=OK 授权）
            return await Task.FromResult(new SecsMessage(2, 40, replyExpected: false)
            {
                Name = "MultiBlockGrant",
                SecsItem = Item.B(0x00)
            });
        }

        /// <summary>S2F41 Remote Command Send → S2F42 Remote Command Acknowledge</summary>
        [SecsFunction(41)]
        [SecsPermission]
        public async Task<SecsMessage?> S2F41(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S2F41 = L,2 [RCMD A, L,n [L,2 [CPNAME, CPVAL]...]]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;
            var rcmdItem = data.Items[0];
            var paramList = data.Items[1];
            if (rcmdItem.Format != SecsFormat.ASCII) return null;
            if (paramList.Format != SecsFormat.List) return null;

            string rcmd = rcmdItem.GetString() ?? string.Empty;
            var parameters = ParseParamList(paramList);
            if (parameters == null) return null;

            var (hcack, errors) = await _equipmentService.ExecuteRemoteCommandAsync(InstanceId, rcmd, parameters);
            return BuildCommandAck(2, 42, "RemoteCommandAcknowledge", hcack, errors);
        }

        /// <summary>S2F43 Reset Spooling Streams and Functions → S2F44 Reset Spooling Acknowledge</summary>
        [SecsFunction(43)]
        public async Task<SecsMessage?> S2F43(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List) return null;

            var config = new List<(byte Stream, IReadOnlyList<byte> Functions)>();
            foreach (var streamItem in data.Items)
            {
                if (streamItem.Format != SecsFormat.List || streamItem.Items.Length != 2) return null;
                byte stream = (byte)ReadId(streamItem.Items[0]);
                var fnList = streamItem.Items[1];
                if (fnList.Format != SecsFormat.List) return null;
                config.Add((stream, fnList.Items.Select(i => (byte)ReadId(i)).ToArray()));
            }

            var (rspack, errors) = await _equipmentService.ResetSpoolingAsync(InstanceId, config);

            // S2F44 = L,2 [RSPACK B, L,m [L,3 [STRID, STRACK, L,n [FCNID...]]...]]
            var errItems = (errors ?? Array.Empty<(byte, IReadOnlyList<byte>)>())
                .Select(e => Item.L(
                    Item.U1(e.Item1),
                    Item.B(0x01),
                    Item.L(e.Item2.Select(f => Item.U1(f)).ToArray())))
                .ToArray();
            return new SecsMessage(2, 44, replyExpected: false)
            {
                Name = "ResetSpoolingAcknowledge",
                SecsItem = Item.L(Item.B(rspack), Item.L(errItems))
            };
        }

        /// <summary>S2F45 Define Variable Limit Attributes → S2F46 Variable Limit Attributes Acknowledge</summary>
        [SecsFunction(45)]
        public async Task<SecsMessage?> S2F45(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;

            byte ack = await _equipmentService.DefineLimitAttributesAsync(InstanceId);

            // S2F46 = L,2 [VLAACK B, L,0]（无错误时 m=0）
            return new SecsMessage(2, 46, replyExpected: false)
            {
                Name = "VariableLimitAttributesAcknowledge",
                SecsItem = Item.L(Item.B(ack), Item.L())
            };
        }

        /// <summary>S2F47 Variable Limit Attributes Request → S2F48 Variable Limit Attributes Send（本期回空）</summary>
        [SecsFunction(47)]
        public async Task<SecsMessage?> S2F47(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List) return null;

            // 限值属性查询未落地，返回空列表（L,0 = 无限值定义）
            return await Task.FromResult(new SecsMessage(2, 48, replyExpected: false)
            {
                Name = "VariableLimitAttributesSend",
                SecsItem = Item.L()
            });
        }

        /// <summary>S2F49 Enhanced Remote Command → S2F50 Enhanced Remote Command Acknowledge</summary>
        [SecsFunction(49)]
        public async Task<SecsMessage?> S2F49(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S2F49 = L,4 [DATAID, OBJSPEC, RCMD, L,m [L,2 [CPNAME, CEPVAL]...]]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 4) return null;
            var objSpecItem = data.Items[1];
            var rcmdItem = data.Items[2];
            var paramList = data.Items[3];
            if (rcmdItem.Format != SecsFormat.ASCII) return null;
            if (paramList.Format != SecsFormat.List) return null;

            string objSpec = objSpecItem.Format == SecsFormat.ASCII ? (objSpecItem.GetString() ?? string.Empty) : string.Empty;
            string rcmd = rcmdItem.GetString() ?? string.Empty;
            var parameters = ParseParamList(paramList);
            if (parameters == null) return null;

            var (hcack, errors) = await _equipmentService.ExecuteEnhancedCommandAsync(InstanceId, objSpec, rcmd, parameters);
            return BuildCommandAck(2, 50, "EnhancedRemoteCommandAcknowledge", hcack, errors);
        }

        /// <summary>组装 S2F42/S2F50 命令应答：L,2 [HCACK B, L,n [L,2 [CPNAME, CPACK]...]]</summary>
        private static SecsMessage BuildCommandAck(byte stream, byte function, string name, byte hcack, IReadOnlyList<(string Name, byte Ack)>? errors)
        {
            var errItems = (errors ?? Array.Empty<(string, byte)>())
                .Select(e => Item.L(Item.A(e.Item1), Item.B(e.Item2)))
                .ToArray();
            return new SecsMessage(stream, function, replyExpected: false)
            {
                Name = name,
                SecsItem = Item.L(Item.B(hcack), Item.L(errItems))
            };
        }

        /// <summary>解析 ID 列表（ECID/SVID/RPTID/CEID/VID），空列表 → 空数组（表示全部）。</summary>
        private static IReadOnlyList<int> ParseIdList(Item? data)
        {
            if (data == null || data.Format != SecsFormat.List) return Array.Empty<int>();
            return data.Items.Select(ReadId).Select(x => (int)x).ToArray();
        }

        /// <summary>读取单个标量 ID（U1/U2/U4/I1/I2/I4 均转 long）。</summary>
        private static long ReadId(Item item)
        {
            return item.Format switch
            {
                SecsFormat.U1 => item.FirstValue<byte>(),
                SecsFormat.U2 => item.FirstValue<ushort>(),
                SecsFormat.U4 => item.FirstValue<uint>(),
                SecsFormat.I1 => item.FirstValue<sbyte>(),
                SecsFormat.I2 => item.FirstValue<short>(),
                SecsFormat.I4 => item.FirstValue<int>(),
                SecsFormat.I8 => item.FirstValue<long>(),
                SecsFormat.U8 => (long)item.FirstValue<ulong>(),
                _ => 0,
            };
        }

        /// <summary>把标量值读成文本（用于 ECV/CPVAL）。</summary>
        private static string ReadAsText(Item item)
        {
            return item.Format switch
            {
                SecsFormat.ASCII => item.GetString() ?? string.Empty,
                SecsFormat.JIS8 => item.GetString() ?? string.Empty,
                SecsFormat.Binary => BitConverter.ToString(item.GetMemory<byte>().ToArray()),
                SecsFormat.Boolean => item.FirstValue<bool>() ? "1" : "0",
                SecsFormat.U1 => item.FirstValue<byte>().ToString(),
                SecsFormat.U2 => item.FirstValue<ushort>().ToString(),
                SecsFormat.U4 => item.FirstValue<uint>().ToString(),
                SecsFormat.U8 => item.FirstValue<ulong>().ToString(),
                SecsFormat.I1 => item.FirstValue<sbyte>().ToString(),
                SecsFormat.I2 => item.FirstValue<short>().ToString(),
                SecsFormat.I4 => item.FirstValue<int>().ToString(),
                SecsFormat.I8 => item.FirstValue<long>().ToString(),
                SecsFormat.F4 => item.FirstValue<float>().ToString(System.Globalization.CultureInfo.InvariantCulture),
                SecsFormat.F8 => item.FirstValue<double>().ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => string.Empty,
            };
        }

        /// <summary>解析参数列表 L,n [L,2 [CPNAME, CPVAL]...]；结构非法返回 null。</summary>
        private static List<(string Name, string Value)>? ParseParamList(Item paramList)
        {
            var result = new List<(string, string)>();
            foreach (var param in paramList.Items)
            {
                if (param.Format != SecsFormat.List || param.Items.Length != 2) return null;
                var nameItem = param.Items[0];
                if (nameItem.Format != SecsFormat.ASCII) return null;
                string name = nameItem.GetString() ?? string.Empty;
                string value = ReadAsText(param.Items[1]);
                result.Add((name, value));
            }
            return result;
        }
    }
}
