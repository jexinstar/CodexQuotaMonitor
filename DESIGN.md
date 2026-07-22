# Codex 额度监控器开发设计流程

请使用以下技术栈开发一个 Windows 11 桌面额度监控组件：

* C#
* WPF
* .NET Framework 4.8.1
* MVVM 架构
* 原生 DWM Acrylic
* Windows 11 圆角与窗口阴影
* 不使用 WinUI 3
* 不使用 `AllowsTransparency="True"`
* 尽量减少第三方 UI 框架依赖

项目名称暂定为：

```text
CodexQuotaMonitor
```

## 一、项目目标

开发一个运行于 Windows 11 的 Codex 额度监控桌面组件，用于展示 Codex 的使用额度、剩余额度、刷新周期和近期使用趋势。

界面整体参考提供的设计图，采用 Windows 11 Acrylic 半透明磨砂玻璃风格，具有以下视觉特征：

* 半透明磨砂玻璃背景
* Windows 11 圆角
* 柔和的窗口阴影
* 细微白色玻璃描边
* 蓝色、青色和紫色作为主要强调色
* 深色背景下保持清晰的文字层级
* 卡片式信息布局
* 环形进度条
* 横向进度条
* 最近使用趋势折线图
* 窗口可拖动
* 支持刷新和设置入口
* 支持浅色、深色主题适配

第一阶段优先完成 UI、窗口效果和模拟数据，不要立即接入真实 Codex 接口。

---

## 二、开发原则

请严格按照以下原则开发：

1. 每完成一个阶段后，先确保项目可以正常编译运行。
2. 不要一次性生成所有功能。
3. 每个阶段完成后说明：

   * 新增了哪些文件
   * 修改了哪些文件
   * 当前可以看到什么效果
   * 下一阶段准备做什么
4. 所有原生 Windows API 调用集中放在独立的辅助类中。
5. UI、业务逻辑和数据模型必须分离。
6. 不要在代码后台直接堆积大量 UI 逻辑。
7. 优先使用数据绑定、命令和资源字典。
8. 所有数值先使用模拟数据。
9. 所有异常必须被捕获并记录。
10. 不要使用已废弃或来源不明的 Acrylic 实现方式。

---

## 三、项目目录结构

请先创建以下目录结构：

```text
CodexQuotaMonitor
│
├─ Models
│  ├─ QuotaSummary.cs
│  ├─ QuotaPeriod.cs
│  ├─ UsageTrendPoint.cs
│  └─ ApplicationSettings.cs
│
├─ ViewModels
│  ├─ ViewModelBase.cs
│  ├─ MainViewModel.cs
│  └─ SettingsViewModel.cs
│
├─ Views
│  ├─ MainWindow.xaml
│  ├─ MainWindow.xaml.cs
│  ├─ SettingsWindow.xaml
│  └─ SettingsWindow.xaml.cs
│
├─ Controls
│  ├─ CircularProgress.xaml
│  ├─ CircularProgress.xaml.cs
│  ├─ QuotaCard.xaml
│  ├─ QuotaCard.xaml.cs
│  ├─ UsageProgressItem.xaml
│  └─ UsageProgressItem.xaml.cs
│
├─ Services
│  ├─ IQuotaService.cs
│  ├─ MockQuotaService.cs
│  ├─ QuotaService.cs
│  ├─ SettingsService.cs
│  └─ LoggingService.cs
│
├─ Native
│  ├─ DwmApi.cs
│  ├─ WindowBackdropService.cs
│  └─ WindowMessageHelper.cs
│
├─ Commands
│  ├─ RelayCommand.cs
│  └─ AsyncRelayCommand.cs
│
├─ Converters
│  ├─ PercentageToAngleConverter.cs
│  ├─ PercentageToWidthConverter.cs
│  ├─ BooleanToVisibilityConverter.cs
│  └─ StatusToBrushConverter.cs
│
├─ Themes
│  ├─ Colors.xaml
│  ├─ Typography.xaml
│  ├─ Buttons.xaml
│  ├─ Cards.xaml
│  └─ DarkTheme.xaml
│
├─ Assets
│  └─ Icons
│
├─ App.xaml
└─ App.xaml.cs
```

如果某些目录在当前阶段暂时没有内容，可以先保留规划，不需要创建无意义的空文件。

---

## 四、第一阶段：创建基础项目

创建一个 WPF `.NET Framework 4.8.1` 项目。

基础要求：

* 启用 nullable 不作强制要求，因为 .NET Framework 项目可能不方便统一使用。
* 窗口默认尺寸建议为：

```text
宽度：1100
高度：720
最小宽度：900
最小高度：600
```

* 主窗口使用无边框设计：

```xml
WindowStyle="None"
ResizeMode="CanResize"
AllowsTransparency="False"
Background="Transparent"
```

注意：

不要使用 `AllowsTransparency="True"`，避免破坏 DWM 合成、阴影和硬件加速。

主窗口需要支持：

* 鼠标拖动
* 双击标题栏最大化或还原
* 最小化
* 关闭
* 调整窗口尺寸
* Windows 11 系统圆角
* 系统阴影

第一阶段只需要建立可运行的空窗口和基本 MVVM 框架。

---

## 五、第二阶段：实现原生 DWM Acrylic

使用 `DwmSetWindowAttribute` 实现 Windows 11 原生背景材质。

优先使用：

```text
DWMWA_SYSTEMBACKDROP_TYPE
```

建议设置为：

```text
DWMSBT_TRANSIENTWINDOW
```

用于获得 Desktop Acrylic 效果。

同时设置：

```text
DWMWA_WINDOW_CORNER_PREFERENCE
DWMWA_USE_IMMERSIVE_DARK_MODE
```

需要实现以下类：

```text
DwmApi
WindowBackdropService
```

`DwmApi` 只负责声明：

* 枚举
* 常量
* P/Invoke 方法

`WindowBackdropService` 负责：

* 检测 Windows 版本
* 启用 Acrylic
* 设置圆角
* 设置深色模式
* Acrylic 不可用时提供降级背景

在窗口的 `SourceInitialized` 事件中获取 HWND，并应用 DWM 属性。

需要考虑以下情况：

* Windows 11：使用原生 Acrylic
* Windows 10：使用纯半透明深色背景降级
* DWM API 调用失败：不能导致应用崩溃
* 节能模式下 Acrylic 失效：界面仍然可读

窗口内部建议叠加一层半透明背景：

```text
深色玻璃背景：#B0182440
浅色高光层：#18FFFFFF
玻璃边框：#55FFFFFF
```

不要将窗口内部背景设置为完全透明，否则可能出现黑色区域或材质不生效。

---

## 六、第三阶段：建立主题资源

将界面样式拆分至多个资源字典。

### 主色建议

```text
背景深色：#10172A
卡片背景：#2AFFFFFF
卡片悬停：#3AFFFFFF
玻璃描边：#55FFFFFF
主要文字：#F5F7FF
次要文字：#B7C2DC
弱化文字：#8190AE
蓝色强调：#54B9FF
青色强调：#55E3CB
紫色强调：#A277FF
绿色状态：#49E59B
警告颜色：#FFC85A
危险颜色：#FF6B7D
```

### 圆角规范

```text
主窗口：28
一级卡片：20
二级卡片：16
按钮：12
状态标签：10
```

### 间距规范

```text
页面边距：32
卡片间距：16
卡片内部边距：20
标题与内容间距：8
大模块间距：20
```

### 字体层级

```text
应用标题：30px，SemiBold
卡片标题：17px，SemiBold
核心数字：36px，Bold
正文：14px
辅助文字：12px
```

默认优先使用：

```text
Segoe UI
Microsoft YaHei UI
```

---

## 七、第四阶段：实现主窗口布局

主窗口整体分为四个区域。

### 1. 顶部标题栏

左侧包含：

* 应用图标
* 标题：Codex 额度监控器
* 副标题：实时监控使用情况，智能管理额度资源

右侧包含：

* 实时状态
* 绿色状态点
* 同步正常
* 刷新按钮
* 更多菜单按钮
* 最小化按钮
* 关闭按钮

标题栏高度建议：

```text
90px
```

标题栏空白区域支持拖动窗口。

### 2. 顶部额度卡片

顶部展示四张卡片：

```text
今日额度
本周额度
本月额度
剩余额度
```

前三张额度卡片包含：

* 图标
* 标题
* 环形进度条
* 使用百分比
* 已使用文字
* 已使用数量
* 总额度
* 单位

第四张剩余额度卡片包含：

* 剩余额度
* 单位
* 可用百分比
* 半圆形或分段式仪表效果

建议使用四列布局：

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
</Grid>
```

窗口宽度不足时，可以改为两列两行布局。

### 3. 使用详情区域

左下区域展示三条使用进度：

```text
今日使用
本周使用
本月使用
```

每条数据包含：

* 图标
* 名称
* 已使用/总额度
* 使用百分比
* 横向进度条

进度条需要支持：

* 平滑动画
* 根据额度类型显示不同颜色
* 背景轨道半透明
* 百分比变化时自动更新

### 4. 最近七日趋势区域

右下区域显示：

```text
近7日使用趋势
```

内容包括：

* 七个日期
* 每日额度消耗
* 折线
* 数据点
* 半透明面积填充
* 最大值提示
* 单位标识

第一版可以使用 WPF 原生：

* `Canvas`
* `Polyline`
* `Path`
* `Ellipse`
* `TextBlock`

不要在第一版引入大型图表框架。

趋势图应根据控件实际宽度和高度动态计算坐标。

---

## 八、第五阶段：实现自定义环形进度条

创建：

```text
CircularProgress
```

需要支持以下依赖属性：

```text
Value
Maximum
StrokeThickness
ProgressBrush
TrackBrush
StartAngle
DisplayText
Subtitle
AnimationDuration
```

实现方式建议使用：

* `Path`
* `ArcSegment`
* `PathGeometry`

角度计算：

```text
进度角度 = Value / Maximum × 360
```

需要处理：

* Value 小于 0
* Value 大于 Maximum
* 0%
* 100%
* 控件尺寸变化
* Maximum 为 0

进度变化时使用动画平滑过渡。

不要直接在 XAML 中写死三个不同的圆环，应使用一个可复用控件。

---

## 九、第六阶段：建立数据模型

创建 `QuotaPeriod`：

```csharp
public class QuotaPeriod
{
    public string Name { get; set; }
    public double Used { get; set; }
    public double Limit { get; set; }
    public string Unit { get; set; }
    public string AccentColor { get; set; }

    public double UsagePercentage
    {
        get
        {
            if (Limit <= 0)
                return 0;

            return Math.Min(Used / Limit * 100.0, 100.0);
        }
    }

    public double Remaining
    {
        get
        {
            return Math.Max(Limit - Used, 0);
        }
    }
}
```

创建 `UsageTrendPoint`：

```csharp
public class UsageTrendPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
}
```

创建 `QuotaSummary`，包含：

```text
今日额度
本周额度
本月额度
总剩余额度
同步状态
最后同步时间
最近七日趋势
```

所有模型都应避免直接依赖 WPF 控件。

---

## 十、第七阶段：建立主视图模型

`MainViewModel` 至少包含：

```text
TodayQuota
WeekQuota
MonthQuota
RemainingQuota
UsageTrend
SyncStatus
LastSyncTime
IsRefreshing
RefreshCommand
OpenSettingsCommand
```

刷新流程：

1. 用户点击刷新。
2. `IsRefreshing` 设置为 true。
3. 禁用重复点击。
4. 调用 `IQuotaService.GetQuotaSummaryAsync()`。
5. 更新额度数据。
6. 更新最后同步时间。
7. 更新状态。
8. 捕获异常。
9. 将 `IsRefreshing` 恢复为 false。

刷新期间，刷新图标可以旋转。

第一版通过 `MockQuotaService` 返回模拟数据：

```text
今日：6800 / 10000
本周：29400 / 70000
本月：162000 / 200000
剩余：38000
```

最近七日数据示例：

```text
2000
3800
4000
5200
5600
7200
3100
```

模拟刷新时可以随机调整少量数值，但必须保证：

```text
Used <= Limit
Remaining >= 0
Percentage <= 100
```

---

## 十一、第八阶段：实现状态与异常提示

状态区域至少支持：

```text
同步正常
正在刷新
同步失败
数据过期
未配置
```

颜色规则：

```text
同步正常：绿色
正在刷新：蓝色
同步失败：红色
数据过期：黄色
未配置：灰色
```

不要使用阻塞式 MessageBox 显示普通错误。

普通错误应使用窗口顶部或底部的非阻塞提示条。

严重错误才允许显示对话框。

日志至少记录：

* 应用启动
* Acrylic 初始化结果
* 刷新开始
* 刷新成功
* 刷新失败
* 配置读取失败
* 应用退出

日志保存在：

```text
%LocalAppData%\CodexQuotaMonitor\Logs
```

---

## 十二、第九阶段：桌面组件行为

实现以下桌面组件功能：

### 窗口置顶

设置项允许开启：

```text
始终置顶
```

绑定至：

```csharp
Topmost
```

### 记忆窗口位置

应用退出前保存：

```text
Left
Top
Width
Height
WindowState
```

重新打开时恢复。

恢复前必须检查坐标是否仍处于有效显示器范围内，避免窗口出现在屏幕外。

### 自动刷新

允许用户选择：

```text
关闭
每5分钟
每15分钟
每30分钟
每60分钟
```

使用 `DispatcherTimer` 或可取消的异步循环实现。

不要在 UI 线程执行阻塞操作。

### 最小化到托盘

可以作为后续功能，第一版先保留接口和设置项，不必立即实现。

---

## 十三、第十阶段：设置窗口

设置窗口包含：

```text
自动刷新间隔
始终置顶
开机启动
深色模式
显示趋势图
紧凑模式
数据来源
测试连接
```

设置保存位置：

```text
%LocalAppData%\CodexQuotaMonitor\settings.json
```

由于项目使用 .NET Framework 4.8.1，可以选择：

* Newtonsoft.Json
* DataContractJsonSerializer

如果引入 Newtonsoft.Json，只用于配置序列化，不要引入庞大的第三方依赖体系。

设置保存后应立即生效。

---

## 十四、第十一阶段：真实数据接口预留

定义统一服务接口：

```csharp
public interface IQuotaService
{
    Task<QuotaSummary> GetQuotaSummaryAsync(
        CancellationToken cancellationToken);
}
```

实现两个服务：

```text
MockQuotaService
CodexQuotaService
```

第一版默认使用 `MockQuotaService`。

在尚未确认官方稳定接口前，不要猜测接口地址，不要把账号密码、Cookie、Token 或 API Key 硬编码进源码。

真实数据接入必须满足：

* 请求可以取消
* 设置超时
* 捕获网络异常
* 数据校验
* 日志中隐藏敏感字段
* 密钥保存在 Windows Credential Manager 或其他安全存储中
* 不把密钥写入普通 JSON 文件

如果没有可靠的额度接口，先完成数据适配层，并允许从本地 JSON 文件读取模拟额度数据。

---

## 十五、第十二阶段：动画效果

动画应轻量，不影响 WPF 性能。

实现以下动画：

* 窗口打开时轻微淡入
* 卡片进入时轻微上移
* 环形进度平滑增长
* 横向进度条平滑增长
* 刷新图标旋转
* 鼠标悬停时卡片亮度轻微提升
* 按钮按下时缩小至 0.97 倍
* 状态变化时颜色平滑过渡

动画时长建议：

```text
按钮反馈：100–150ms
卡片悬停：150–200ms
进度变化：400–700ms
页面进入：250–400ms
```

避免：

* 大面积模糊动画
* 高频阴影动画
* 持续运行的装饰动画
* 占用大量 CPU 的 CompositionTarget.Rendering

---

## 十六、响应式布局要求

当窗口宽度大于 1000px：

```text
顶部四张卡片横向排列
底部左右两栏
```

当窗口宽度在 760px 至 1000px：

```text
顶部两列两行
底部上下排列
```

当窗口宽度小于 760px：

```text
切换紧凑模式
减少边距
隐藏部分辅助文字
趋势图可折叠
```

WPF 没有原生媒体查询，可以通过以下方式实现：

* 根据 `ActualWidth` 切换布局状态
* 使用 DataTrigger
* 使用 VisualStateManager
* 在 ViewModel 中暴露 `IsCompactMode`

避免在窗口尺寸变化时频繁创建新控件。

---

## 十七、无障碍与可读性

需要满足：

* 主要文字与背景具有足够对比度
* 不只依赖颜色表达状态
* 所有按钮提供 ToolTip
* 图标按钮具备可理解的 AutomationProperties.Name
* 键盘可以操作刷新、设置、关闭
* 环形进度同时显示数字
* Acrylic 失效后文字仍然清晰

---

## 十八、验收标准

第一版完成后应满足以下条件：

### 编译与运行

* 项目可以在 Visual Studio 中正常编译。
* 目标框架为 .NET Framework 4.8.1。
* 不存在阻止编译的警告或错误。
* 应用启动不崩溃。
* DWM 调用失败时应用仍然可运行。

### 视觉效果

* 主窗口具有明显的 Acrylic 磨砂玻璃效果。
* 窗口具有 Windows 11 圆角。
* 界面与参考图整体结构相似。
* 顶部包含四张额度卡片。
* 至少有三个环形进度条。
* 左下包含三条横向进度。
* 右下包含七日趋势图。
* 状态、刷新和最后同步时间清晰可见。

### 功能效果

* 点击刷新可以更新模拟数据。
* 刷新期间按钮不能重复触发。
* 额度百分比自动计算。
* 进度环和进度条随数据变化。
* 最近七日趋势图可以更新。
* 设置能够保存。
* 窗口位置能够恢复。
* 自动刷新可以开启和关闭。
* 始终置顶可以切换。

### 代码质量

* 使用 MVVM。
* 数据服务通过接口抽象。
* DWM API 与业务代码分离。
* 样式放在资源字典中。
* 自定义控件可复用。
* 没有大量重复 XAML。
* 没有硬编码敏感信息。
* 异步操作不阻塞 UI。

---

## 十九、开发顺序

请严格按照以下顺序执行：

```text
1. 创建项目和目录结构
2. 建立基础 MVVM
3. 创建无边框主窗口
4. 实现 DWM Acrylic
5. 创建主题资源
6. 搭建主界面静态布局
7. 实现环形进度控件
8. 实现横向进度组件
9. 实现七日趋势图
10. 创建数据模型
11. 创建 MockQuotaService
12. 绑定 MainViewModel
13. 实现刷新命令
14. 实现状态提示
15. 实现窗口位置记忆
16. 实现自动刷新
17. 创建设置窗口
18. 加入动画
19. 测试降级模式
20. 整理代码和 README
```

每次只执行一个阶段。

不要跳过编译检查。

---

