# PLC 通讯架构 - UML 图（v3.0 读写注解分离 + 通用门面）

> 与 `docs/plc-framework.md` 保持一致。以下为可编辑 Mermaid 源码，可粘贴到
> [Mermaid Live Editor](https://mermaid.live) 等站点二次编辑。

## 1. 核心类图

```mermaid
classDiagram
    direction TB

    class PlcDevice {
        <<enumeration>>
        D=0
        W=1
        M=2
        X=3
        Y=4
        L=5
        F=6
        V=7
        B=8
        S=9
        SM=10
        SD=11
    }

    class PlcWriteRole {
        <<enumeration>>
        Value=0
        Trigger=1
        Reply=2
    }

    class PlcReadAttribute {
        +PlcDevice DeviceCode
        +string Address
        +int Offset
        +string DisplayName
    }

    class PlcWriteAttribute {
        +PlcDevice DeviceCode
        +string Address
        +int Offset
        +string DisplayName
        +PlcWriteRole Role
    }

    class PlcReadDefinition {
        +string PropertyName
        +PlcDevice DeviceCode
        +string Address
        +int Offset
        +Type DataType
        +string DisplayName
    }

    class PlcWriteDefinition {
        +string PropertyName
        +PlcDevice DeviceCode
        +string Address
        +int Offset
        +Type DataType
        +string DisplayName
        +PlcWriteRole Role
    }

    class PlcObjectPlan {
        +IReadOnlyList~PlcReadDefinition~ Reads
        +IReadOnlyList~PlcWriteDefinition~ WriteValues
        +PlcWriteDefinition Trigger
        +PlcWriteDefinition Reply
    }

    class PlcPointAttributeScanner {
        -_cache ConcurrentDictionary~Type, PlcObjectPlan~
        +Scan~T~() PlcObjectPlan
        +Scan(Type) PlcObjectPlan
    }

    class PlcPointAccessor~T~ {
        -_getters IReadOnlyDictionary~string, Func~
        +PlcObjectPlan Plan
        +GetValue(obj, propertyName) object
    }

    class PlcHelper {
        +ReplyPending=0
        +ReplyAck=1
        +ReplyNak=2
        +IsOn(text) bool
        +ParseReply(text) int
        +ReadCachedValue(cached, device, address) string
    }

    class IPlcObjectService {
        <<interface>>
        +WriteAsync~T~(settingId, obj) Task~PlcWriteResult~
        +Read~T~(settingId) IReadOnlyDictionary~string,string~
        +IsConnected(settingId) bool
        +IsWriteSuccess~T~(settingId) bool
    }

    class PlcObjectService {
        -_plcService IPlcCommunicationService
        -_accessors ConcurrentDictionary~Type, object~
        +WriteAsync~T~()
        +Read~T~()
        +IsConnected()
        +IsWriteSuccess~T~()
        -WritePointAsync(settingId, def, value)
        -WaitForReplyAsync(settingId, reply)
        -GetAccessor~T~() PlcPointAccessor~T~
    }

    class PlcWriteResult {
        +bool Success
        +string Message
    }

    class IPlcCommunicationService {
        <<interface>>
        +ConnectAsync(setting) Task~bool~
        +ReadAllPointsAsync(settingId) Task~List~PointValueVO~~
        +WriteSinglePointAsync(settingId, deviceCode, plcAddress, value) Task~bool~
        +GetCachedPoints() List~PointValueVO~
        +GetConnectedSettingIds() List~long~
        +Disconnect(settingId)
        +IsConnected(settingId) bool
        +LastError string
    }

    class PlcCommunicationService {
        -_connections ConcurrentDictionary~long, McpX~
        -_cache ConcurrentDictionary~long, List~PointValueVO~~
        -WriteWordAsync(plc, prefix, address, value)
        -ReadSinglePointAsync(plc, point)
    }

    class MockPlcCommunicationService {
        -_recipeWriteValues ConcurrentDictionary
        -SimulateRecipeWriteAsync(settingId)
    }

    class PointValueVO {
        +long SettingId
        +string DeviceCode
        +string PlcAddress
        +int Quantity
        +Values[]
    }

    class PointSubValueVO {
        +string DeviceCode
        +string PlcAddress
        +string Value
    }

    class Recipe {
        +int UpperLamiTmp_Stage1
        +double DrvSpeed
        +bool WriteTrigger
        +int WriteReply
    }

    PlcReadAttribute --> PlcDevice
    PlcWriteAttribute --> PlcDevice
    PlcWriteAttribute --> PlcWriteRole
    PlcReadDefinition --> PlcDevice
    PlcWriteDefinition --> PlcDevice
    PlcWriteDefinition --> PlcWriteRole
    PlcObjectPlan *-- PlcReadDefinition
    PlcObjectPlan *-- PlcWriteDefinition
    PlcPointAttributeScanner --> PlcObjectPlan
    PlcPointAccessor --> PlcObjectPlan
    PlcObjectService ..|> IPlcObjectService
    PlcObjectService --> PlcPointAccessor
    PlcObjectService --> PlcHelper
    PlcObjectService --> PlcWriteResult
    PlcObjectService --> IPlcCommunicationService
    PlcCommunicationService ..|> IPlcCommunicationService
    MockPlcCommunicationService ..|> IPlcCommunicationService
    PlcCommunicationService --> PointValueVO
    PointValueVO *-- PointSubValueVO
    Recipe ..> PlcReadAttribute
    Recipe ..> PlcWriteAttribute
```

---

## 2. 配方下发 · 三态握手时序图

```mermaid
sequenceDiagram
    autonumber
    participant VM as PagePNSelectViewModel
    participant SVC as PlcObjectService
    participant PLC as IPlcCommunicationService
    participant Dev as PLC(Device)

    VM->>SVC: WriteAsync<Recipe>(settingId, recipe)
    SVC->>PLC: IsConnected(settingId)

    alt 未连接
        SVC-->>VM: PlcWriteResult{Success=false}
    else 已连接
        loop 每个 WriteValues 字段
            SVC->>SVC: accessor.GetValue(obj, prop)
            SVC->>PLC: WriteSinglePointAsync(值点位)
            PLC->>Dev: 写入寄存器
        end
        SVC->>PLC: WriteSinglePointAsync(Trigger, true)
        PLC->>Dev: 触发位置 1

        loop 轮询回复位(100ms, 超时5s)
            SVC->>PLC: GetCachedPoints()
            PLC-->>SVC: 回复位状态(0/1/2)
        end

        alt 回复 = ACK(1)
            SVC->>PLC: 触发位复归 0
            PLC->>Dev: 触发位复归 0
            Note over Dev: PLC 检测触发位=0，回复位复归 0
            SVC-->>VM: PlcWriteResult{Success=true}
        else 回复 = NAK(2)
            SVC->>PLC: 触发位复归 0
            SVC-->>VM: PlcWriteResult{Success=false, NAK}
        else 超时无回应
            SVC->>PLC: 触发位复归 0
            SVC-->>VM: PlcWriteResult{Success=false, 超时}
        end
    end
```

---

## 3. 注解 → 读写链路 · 分层数据流

```mermaid
flowchart TB
    subgraph L1["1 · 业务实体层"]
        A["Recipe 等实体<br/>[PlcRead] + [PlcWrite] 注解"]
        B["[PlcRead(PlcDevice.D, &quot;2034&quot;)]<br/>[PlcWrite(PlcDevice.D, &quot;3700&quot;)]<br/>public int UpperLamiTmp_Stage1"]
    end

    subgraph L2["2 · 注解扫描层（反射 + 缓存）"]
        C["PlcPointAttributeScanner.Scan&lt;T&gt;()<br/>→ PlcObjectPlan"]
        D["自动分类：Reads / WriteValues<br/>Trigger(Role=Trigger) / Reply(Role=Reply)<br/>ConcurrentDictionary 按 Type 缓存"]
    end

    subgraph L3["3 · 通用门面层（零样板）"]
        E["PlcObjectService<br/>WriteAsync（三态握手）/ Read（返回字典）"]
        F["PlcPointAccessor&lt;T&gt;<br/>表达式树编译 getter，零反射取值"]
        G["PlcHelper<br/>IsOn / ParseReply / ReadCachedValue"]
    end

    subgraph L4["4 · 通讯引擎层（按段批量）"]
        H["IPlcCommunicationService<br/>WriteSinglePointAsync / GetCachedPoints"]
        I["PointValueVO（段）+ Values[]（子值，地址十六进制递增）"]
    end

    L1 -->|扫描注解| L2
    L2 -->|生成计划| L3
    L3 -->|读写点位| L4
    L4 -->|返回缓存值| L3
```
