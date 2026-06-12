# WordWhiz for Windows

一款 Windows 原生轻量级文案优化工具，常驻系统托盘，通过全局快捷键一键调用 LLM 大语言模型优化剪贴板文本。

基于 [WordWhiz macOS 版](https://github.com/dongritengfei/WordWhiz) 完整移植。

![Platform](https://img.shields.io/badge/platform-Windows%2010%2021H1+-blue)
![Runtime](https://img.shields.io/badge/runtime-.NET%208-purple)
![Framework](https://img.shields.io/badge/framework-WinUI%203-green)
![License](https://img.shields.io/badge/license-MIT-green)

> **声明**：本项目 100% 的代码均由 AI 生成，未包含任何人工编写的代码。

## 功能特性

- **全局快捷键触发** - 默认 `Ctrl+Shift+Z` 一键唤出优化面板，无需切换应用
- **实时流式输出** - LLM 响应逐字呈现，无需等待完整响应
- **多 LLM 服务商支持** - 支持 OpenAI、Anthropic Claude、DeepSeek、通义千问、Google Gemini、Kimi、智谱 GLM、MiniMax 及自定义 OpenAI 兼容接口（推荐通过阿里云百炼平台使用`kimi-k2.6`，速度最快）
- **自定义指令** - 内置 6 种默认指令（润色、翻译、摘要、扩写、正式化、口语化），支持自定义 Prompt
- **历史记录** - 自动保存优化记录，支持搜索和复用
- **浮动面板** - 660×660 置顶无边框面板，支持多种锚定位置（右侧/左侧/居中）
- **安全存储** - API Key 使用 Windows DPAPI 加密存储
- **开机自启动** - 支持随 Windows 启动自动运行
- **MSIX 打包** - 现代化应用分发格式，干净安装与卸载

## 系统要求

- Windows 10 版本 2004 (19041) 或更高版本
- Windows 11 推荐
- .NET 8 运行时（MSIX 包内自包含）

## 安装

### 从源码构建

```powershell
git clone https://github.com/dongritengfei/WordWhiz-windows.git
cd WordWhiz-windows
dotnet restore
dotnet build --configuration Release
```

构建完成后，可通过以下方式运行：

```powershell
# 直接运行（调试）
dotnet run --project src/WordWhiz

# 发布 MSIX 包
dotnet publish src/WordWhiz --configuration Release
```

发布后的 MSIX 包位于 `src/WordWhiz/AppPackages/` 目录，双击安装即可。

## 使用说明

### 首次配置

1. 启动应用后，首次运行将进入引导向导
2. 在引导向导中选择 LLM 服务商并填写 API Key
3. 点击"测试连接"验证配置
4. 设置常用快捷键后完成引导

也可通过系统托盘图标右键 → "设置" 进入偏好设置界面。

### 日常使用

1. **复制文本** - 在任意应用中选中需要优化的文本，按 `Ctrl+C` 复制
2. **触发优化** - 按 `Ctrl+Shift+Z`（默认快捷键）唤出优化面板
3. **查看结果** - 面板实时显示优化后的文本，支持直接编辑
4. **复制使用** - 点击"复制结果"或按 `Ctrl+Shift+C`，然后 `Ctrl+V` 粘贴到目标位置

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+Shift+Z` | 触发优化/显示面板 |
| `Ctrl+Shift+C` | 复制结果 |
| `Ctrl+R` | 重新生成 |
| `Ctrl+1-9` | 快速切换指令 |
| `Esc` | 关闭面板 |
| `Ctrl+Shift+,` | 打开设置 |

## 技术栈

- **UI 框架**: WinUI 3 (Windows App SDK 1.6+)
- **运行时**: .NET 8
- **架构模式**: MVVM (CommunityToolkit.Mvvm)
- **数据持久化**: SQLite + Dapper
- **全局快捷键**: RegisterHotKey Win32 API (CsWin32 源生成器)
- **网络请求**: HttpClient + SSE 流式响应
- **安全存储**: DPAPI (ProtectedData)
- **系统托盘**: H.NotifyIcon.WinUI
- **打包格式**: MSIX

## 项目结构

```
WordWhiz-windows/
├── WordWhiz.sln                      # 解决方案文件
└── src/WordWhiz/
    ├── WordWhiz.csproj               # 项目配置
    ├── Package.appxmanifest          # MSIX 打包清单
    ├── App.xaml / App.xaml.cs        # 应用入口和 DI 容器
    ├── Assets/                       # 图标资源
    ├── Models/                       # 数据模型
    │   ├── CustomPrompt.cs
    │   ├── OptimizationRecord.cs
    │   ├── LLMProviderConfig.cs
    │   └── HotkeyConfig.cs
    ├── ViewModels/                   # MVVM 视图模型
    │   ├── PanelViewModel.cs
    │   ├── SettingsViewModel.cs
    │   └── OnboardingViewModel.cs
    ├── Views/                        # XAML 视图
    │   ├── MainWindow.xaml           # 宿主窗口（隐藏）
    │   ├── OptimizationPanel.xaml    # 浮动优化面板
    │   ├── SettingsWindow.xaml       # 设置窗口
    │   ├── OnboardingWindow.xaml     # 首次引导向导
    │   └── Pages/                    # 设置子页面
    ├── Services/                     # 业务服务
    │   ├── LLM/                      # LLM API 集成
    │   │   ├── ILLMProvider.cs
    │   │   ├── OpenAIProvider.cs
    │   │   ├── AnthropicProvider.cs
    │   │   ├── SSEParser.cs
    │   │   └── LLMProviderFactory.cs
    │   ├── ClipboardService.cs
    │   ├── DataService.cs
    │   ├── HotkeyService.cs
    │   ├── PanelWindowService.cs
    │   ├── SecureStorageService.cs
    │   └── TrayIconService.cs
    ├── Helpers/                      # 工具类
    │   ├── Constants.cs
    │   └── DefaultPrompts.cs
    ├── Converters/                   # XAML 值转换器
    └── Styles/                       # 品牌色和样式
```

## 支持的 LLM 服务商

- [OpenAI](https://openai.com/)
- [Anthropic Claude](https://www.anthropic.com/)
- [DeepSeek](https://www.deepseek.com/)
- [通义千问 (Alibaba)](https://tongyi.aliyun.com/)
- [Google Gemini](https://ai.google.dev/)
- [Kimi / Moonshot AI](https://moonshot.cn/)
- [智谱 GLM](https://open.bigmodel.cn/)
- [MiniMax](https://www.minimaxi.com/)
- 自定义 OpenAI 兼容接口

## 与 macOS 版的差异

| 特性 | macOS 版 | Windows 版 |
|------|---------|-----------|
| UI 框架 | SwiftUI | WinUI 3 XAML |
| 数据存储 | SwiftData | SQLite + Dapper |
| 安全存储 | Keychain Services | DPAPI (ProtectedData) |
| 系统托盘 | MenuBarExtra | H.NotifyIcon.WinUI |
| 全局快捷键 | HotKey (Carbon API) | RegisterHotKey (Win32) |
| 默认触发快捷键 | `⌃Z` | `Ctrl+Shift+Z`* |
| 浮动面板 | NSPanel (floating) | Topmost ToolWindow |
| 打包格式 | .app | MSIX |

> *注：Windows 版将默认触发快捷键改为 `Ctrl+Shift+Z`，以避免与系统 Undo (`Ctrl+Z`) 冲突。

## 隐私说明

- API Key 仅存储在本地 Windows DPAPI 加密存储中，不会上传到任何服务器
- 优化历史记录仅保存在本地 SQLite 数据库（`%LOCALAPPDATA%\WordWhiz\data\`）
- 文本优化请求直接发送至用户配置的 LLM 服务商 API

## 开发计划

- [ ] 快捷键绑定到特定指令
- [ ] 批量优化功能
- [ ] 本地 LLM 支持 (Ollama)
- [ ] 浏览器扩展
- [ ] 原地替换文本 (Windows UI Automation)

## 贡献

本项目 100% 代码由 AI 生成。欢迎提交 Issue 反馈问题或建议。

## 许可证

MIT License
