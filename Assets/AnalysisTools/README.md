# AnalysisTools

AnalysisTools是一个Unity项目性能分析工具，通过IL注入技术在方法的开始和结束处插入性能统计代码，帮助开发者分析和优化游戏性能。

## 功能特点

- 支持两种分析模式：
    - ProfilerSample：使用Unity的Profiler进行采样分析
    - AnalysisAll：自定义分析，记录方法调用次数、内存使用和执行时间
- 通过IL注入技术，无需修改源代码即可实现性能分析
- 支持导出分析结果到CSV文件，方便数据分析和比较
- 支持在编辑器模式和构建过程中进行代码注入

## 使用方式

### 1. 配置注入路径

在Resources目录下创建名为`inject_paths.json`的配置文件，指定需要注入的程序集路径：

```json
{
  "paths": [
    "/../Library/ScriptAssemblies/Assembly-CSharp.dll"
  ],
  "build_paths": [
    "/../Temp/StagingArea/Data/Managed/Assembly-CSharp.dll"
  ]
}
```

- `paths`：编辑器模式下注入的路径（相对于Application.dataPath）
- `build_paths`：构建时注入的路径（相对于Application.dataPath）

### 2. 标记需要分析的方法

使用特性标记需要进行性能分析的方法：

```csharp
// 使用ProfilerSample特性，将使用Unity的Profiler进行采样分析
[ProfilerSample]
public void YourMethod()
{
    // 方法实现
}

// 使用AnalysisAll特性，将进行全面的性能分析
[AnalysisAll]
public void AnotherMethod()
{
    // 方法实现
}
```

### 3. 注入分析代码

有两种方式可以注入分析代码：

#### 方式一：通过菜单项手动注入

在Unity编辑器中，点击菜单栏的`AnalysisTools`，选择以下选项之一：

- `注入代码【ProfilerSample】`：注入ProfilerSample特性标记的方法
- `注入代码【All】`：注入所有方法（包括未标记特性的方法）

#### 方式二：在构建过程中自动注入

在构建项目时，可以通过添加编译符号来自动注入代码：

- `INJECT_SAMPLE`：注入ProfilerSample特性标记的方法
- `INJECT_FUNC`：注入AnalysisAll特性标记的方法

### 4. 导出分析结果

在游戏运行过程中，可以通过以下方式导出分析结果：

#### 方式一：通过代码导出

```csharp
// 在需要导出分析结果的地方调用
HookUtils.ExportMethodAnalysisCSV();
```

#### 方式二：通过菜单项导出

在Unity编辑器中，点击菜单栏的`AnalysisTools`，选择`导出CSV`选项。

导出的CSV文件将保存在`Application.persistentDataPath`目录下，文件名格式为`[yyyy-MM-dd]-[HH-mm-ss].csv`。

CSV文件包含以下信息：
- 函数名
- 平均内存使用（KB）
- 平均执行时间（毫秒）
- 调用次数

## 注意事项

1. **不要在运行时注入代码**：确保在游戏未运行时进行代码注入，否则可能导致错误。

2. **避免注入Unity的轮询方法**：工具默认会跳过Unity的常见轮询方法（如Update、LateUpdate等），以避免性能影响。

3. **注入的函数不能在修改的程序集中**：确保HookUtils等工具类不在被注入的程序集中，否则会导致无法修改该DLL。

4. **多线程限制**：性能分析仅在主线程中有效，非主线程的方法调用不会被记录。

5. **构建设置**：如果需要在构建过程中自动注入代码，请确保添加了相应的编译符号（INJECT_SAMPLE或INJECT_FUNC）。

6. **性能影响**：注入代码会对游戏性能产生一定影响，建议仅在需要分析性能时使用，并在发布版本中移除。

## 示例

```csharp
using System;
using System.Collections.Generic;
using AnalysisTools;
using UnityEngine;
using UnityEngine.UI;

public class MainTest : MonoBehaviour
{
    public Button btnReport;
    
    private void Awake()
    {
        btnReport.onClick.AddListener(HookUtils.ExportMethodAnalysisCSV);
    }

    [ProfilerSample]
    public void TestMethod()
    {
        // 此方法将使用Unity的Profiler进行采样分析
        byte[] arr = new Byte[1024];
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            TestMethod();
        }
    }
}
```

## 工作原理

AnalysisTools通过Mono.Cecil库进行IL注入，在目标方法的开始和结束处插入性能统计代码。具体步骤如下：

1. 加载配置文件中指定的程序集
2. 遍历程序集中的所有类型和方法
3. 根据过滤条件和特性标记，确定需要注入的方法
4. 在方法的开始处插入Begin代码，在所有返回点插入End代码
5. 保存修改后的程序集

在游戏运行过程中，注入的代码会记录方法的调用次数、内存使用和执行时间，并可以通过ExportMethodAnalysisCSV方法导出分析结果。