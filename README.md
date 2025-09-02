# EF Core 迁移助手 (WPF)

一款面向多目标框架项目的 EF Core 迁移可视化小工具。支持快速列出 DbContext、查看迁移、推断表结构，并生成新的迁移，自动避免迁移命名重复。

<image src="..\MigrationTool\MigrationTool\img" alt="Screenshot" width="600"/>

## 功能特性
- 选择 .csproj 并自动解析 TargetFramework/TargetFrameworks
- 一键切换目标框架（适配多目标）
- 枚举 DbContext（调用 `dotnet ef dbcontext list`）
- 展示已有迁移列表（时间戳 - 名称），兼容以下目录：
  - `Migrations/`
  - `Migrations/{ContextShort}`（示例：`MyDbContext` → `My`）
- 推断选中迁移之前的表结构（Table/Column/Type/Nullable）
- 生成迁移（输出至 `Migrations/{ContextShort}`），避免与现有迁移后缀名重名
- 详细错误弹窗：包含标准输出/错误输出与常见问题提示

## 环境要求
- Windows
- .NET 8 SDK
- Visual Studio 2022 (或使用 `dotnet build`/`dotnet run`)
- 目标项目需引用 `Microsoft.EntityFrameworkCore.Design`
- 安装并确保 `dotnet-ef` 与项目 EF Core 版本兼容：
  ```bash
  dotnet tool install --global dotnet-ef
  dotnet tool update --global dotnet-ef
  ```

## 使用说明
1. 启动应用。
2. 点击“浏览...”，选择目标项目的 `.csproj` 文件。
3. 在右上角选择目标框架（多目标项目必须选择）。
4. 在左侧“数据库上下文 (DbContext)”中选择一个上下文。
5. 右侧可查看：
   - 左列：已有迁移（时间戳 - 名称）
   - 右列：推断的表结构（Table/Column/Type/Nullable）
6. 在左侧底部输入“数据库迁移版本”（作为迁移后缀名），点击“生成迁移”。
7. 新迁移会生成到 `Migrations/{ContextShort}` 目录下，并刷新列表。

## 重要说明
- 本工具通过正则解析迁移文件以推断结构，主要覆盖：`CreateTable`、`AddColumn`、`DropColumn`、`AlterColumn`，复杂场景（如重命名、复杂表达式、原生 SQL）可能无法准确识别，仅供参考。
- 生成迁移依赖目标项目可正常构建以及成功创建设计时 `DbContext`。若无法创建，请确保：
  - 存在无参构造函数，或
  - 实现 `IDesignTimeDbContextFactory<TContext>`。
- 多目标项目必须选择具体 TargetFramework，否则 `dotnet ef` 可能无法执行。

## 构建与运行
- 使用 Visual Studio 打开解决方案并直接运行；或在命令行于项目目录执行：
  ```bash
  dotnet build
  ```
- 运行应用后按“使用说明”操作。

## 目录结构（关键）
- `MigrationTool/` WPF 应用工程
  - `MainWindow.xaml` 与 `MainWindow.xaml.cs`：主界面与逻辑
  - `MigrationFileEntry.cs`、`MigrationItem.cs`：数据模型

## 故障排查
- 未找到 `dotnet-ef`：安装/更新全局工具并确认 PATH。
- 无法创建 `DbContext`：检查无参构造或 `IDesignTimeDbContextFactory<TContext>` 实现。
- 找不到项目或路径：确认工作目录与 `.csproj` 是否正确，确保项目能正常构建。
