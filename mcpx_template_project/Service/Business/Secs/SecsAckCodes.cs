namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM 标准应答码常量（对齐《視動標準範例 SECS MANUAL》第 10.0 节 Data Item Dictionary）。
    /// Controller 与 Service 共享，避免魔法数字散落。
    /// </summary>
    public static class SecsAckCodes
    {
        /// <summary>COMMACK：建立通信应答码</summary>
        public const byte CommackAccept = 0x00;

        /// <summary>OFLACK：OFF-LINE 应答码</summary>
        public const byte OflackAccept = 0x00;

        /// <summary>ONLACK：ON-LINE 应答码</summary>
        public const byte OnlackAccepted = 0x00;
        public const byte OnlackNotAllowed = 0x01;
        public const byte OnlackAlreadyOnline = 0x02;

        /// <summary>EAC：设备常量应答码（0=OK，非 0=NG）</summary>
        public const byte EacOk = 0x00;
        public const byte EacError = 0x01;

        /// <summary>TIACK：时间应答码</summary>
        public const byte TiackOk = 0x00;
        public const byte TiackError = 0x01;

        /// <summary>DRACK：定义报表应答码</summary>
        public const byte DrackAccept = 0x00;
        public const byte DrackInsufficientSpace = 0x01;
        public const byte DrackInvalidFormat = 0x02;
        public const byte DrackRptidDefined = 0x03;
        public const byte DrackVidNotExist = 0x04;

        /// <summary>LRACK：关联报表应答码</summary>
        public const byte LrackAccept = 0x00;
        public const byte LrackInsufficientSpace = 0x01;
        public const byte LrackInvalidFormat = 0x02;
        public const byte LrackCeidLinkDefined = 0x03;
        public const byte LrackCeidNotExist = 0x04;
        public const byte LrackRptidNotExist = 0x05;

        /// <summary>ERACK：使能/禁用事件报表应答码</summary>
        public const byte ErackAccept = 0x00;
        public const byte ErackCeidNotExist = 0x01;

        /// <summary>TIAACK：Trace 初始化应答码</summary>
        public const byte TiaackOk = 0x00;
        public const byte TiaackTooManySvids = 0x01;
        public const byte TiaackNoMoreTraces = 0x02;
        public const byte TiaackInvalidPeriod = 0x03;

        /// <summary>ACKC5：报警应答码（0=OK，非 0=NG）</summary>
        public const byte Ackc5Ok = 0x00;
        public const byte Ackc5Error = 0x01;

        /// <summary>ACKC6：数据采集应答码（0=OK，非 0=NG）</summary>
        public const byte Ackc6Ok = 0x00;
        public const byte Ackc6Error = 0x01;

        /// <summary>ACKC7：工艺程序应答码</summary>
        public const byte Ackc7Accepted = 0x00;
        public const byte Ackc7PermissionNotGranted = 0x01;
        public const byte Ackc7LengthError = 0x02;
        public const byte Ackc7MatrixOverflow = 0x03;
        public const byte Ackc7PpidNotFound = 0x04;
        public const byte Ackc7ModeUnsupported = 0x05;

        /// <summary>ACKC10：终端应答码</summary>
        public const byte Ackc10Accepted = 0x00;
        public const byte Ackc10WillNotDisplay = 0x01;
        public const byte Ackc10TerminalNotAvailable = 0x02;

        /// <summary>PPGNT：工艺程序装载授权</summary>
        public const byte PpgntOk = 0x00;
        public const byte PpgntAlreadyHave = 0x01;
        public const byte PpgntNoSpace = 0x02;
        public const byte PpgntInvalidPpid = 0x03;
        public const byte PpgntBusy = 0x04;
        public const byte PpgntWillNotAccept = 0x05;

        /// <summary>HCACK：远程命令应答码</summary>
        public const byte HcackPerformed = 0x00;
        public const byte HcackCommandNotExist = 0x01;
        public const byte HcackCannotPerformNow = 0x02;
        public const byte HcackInvalidParameter = 0x03;
        public const byte HcackPerformedWithEvent = 0x04;
        public const byte HcackAlreadyInCondition = 0x05;
        public const byte HcackNoSuchObject = 0x06;

        /// <summary>RSPACK：重置 Spooling 应答码</summary>
        public const byte RspackAccept = 0x00;
        public const byte RspackReject = 0x01;

        /// <summary>VLAACK：变量限值属性应答码</summary>
        public const byte VlaackAccept = 0x00;
        public const byte VlaackDefinitionError = 0x01;
        public const byte VlaackCannotPerformNow = 0x02;

        /// <summary>RSDA：请求 Spooled 数据应答码</summary>
        public const byte RsdaOk = 0x00;
        public const byte RsdaBusy = 0x01;
        public const byte RsdaNoSpooledData = 0x02;
    }
}
