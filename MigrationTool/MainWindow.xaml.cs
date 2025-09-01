using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace MigrationTool
{
    /// <summary>
    /// 主窗口的交互逻辑。
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 初始化 <see cref="MainWindow"/> 类的新实例。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取或设置上下文列表。
        /// </summary>
        public List<string> Contextes { get; set; } = new();

        /// <summary>
        /// 项目支持的目标框架列表（例如 net8.0、net9.0）。
        /// </summary>
        public List<string> Frameworks { get; set; } = new();

        /// <summary>
        /// 执行命令行工具并返回标准输出、错误输出和退出代码。
        /// </summary>
        /// <param name="command">要执行的命令行字符串。</param>
        /// <param name="workingDirectory">工作目录。</param>
        /// <returns>标准输出、错误输出及退出码。</returns>
        private static (string Output, string Error, int ExitCode) ExecuteCommand(string command, string workingDirectory)
        {
            var info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = info };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (output, error, process.ExitCode);
        }

        /// <summary>
        /// 解析项目文件，读取 TargetFramework 或 TargetFrameworks。
        /// </summary>
        /// <param name="csprojPath">项目文件完整路径。</param>
        /// <returns>目标框架列表。</returns>
        private static List<string> GetTargetFrameworks(string csprojPath)
        {
            var list = new List<string>();
            try
            {
                var doc = XDocument.Load(csprojPath);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
                var tf = doc.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value;
                var tfs = doc.Descendants(ns + "TargetFrameworks").FirstOrDefault()?.Value;

                if (!string.IsNullOrWhiteSpace(tfs))
                    list.AddRange(tfs.Split(';', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries));
                else if (!string.IsNullOrWhiteSpace(tf))
                    list.Add(tf.Trim());
            }
            catch { }
            return list;
        }

        /// <summary>
        /// 使用命令行工具生成上下文列表。
        /// </summary>
        /// <param name="projectDirectory">项目文件所在目录。</param>
        /// <param name="framework">可选：指定目标框架。</param>
        /// <returns>包含所有数据库上下文的列表。</returns>
        private static List<string> GenerateContextList(string projectDirectory, string? framework)
        {
            var contexts = new List<string>();
            try
            {
                var fw = string.IsNullOrWhiteSpace(framework) ? string.Empty : $" --framework {framework}";
                var result = ExecuteCommand($"dotnet ef dbcontext list{fw}", projectDirectory);

                if (!string.IsNullOrEmpty(result.Output))
                {
                    var outputLines = result.Output
                        .Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

                    bool looksError = result.ExitCode != 0 || outputLines.Any(line =>
                        line.Contains("error", System.StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("failed", System.StringComparison.OrdinalIgnoreCase) ||
                        line.Contains("doesn't reference", System.StringComparison.OrdinalIgnoreCase));

                    if (looksError)
                    {
                        ShowErrorDetailed("获取 DbContext 列表失败", result.Output, result.Error, projectDirectory);
                    }
                    else
                    {
                        contexts = outputLines.Where(line =>
                            !string.IsNullOrEmpty(line) &&
                            !line.Contains("...", System.StringComparison.Ordinal) &&
                            !Regex.IsMatch(line, @"\s") &&
                            !Regex.IsMatch(line, @"[<>:""/\\|?*]")
                        ).ToList();
                    }
                }
                else if (result.ExitCode != 0)
                {
                    ShowErrorDetailed("获取 DbContext 列表失败", result.Output, result.Error, projectDirectory);
                }
            }
            catch (System.Exception ex)
            {
                ShowError($"发生异常: {ex.Message}");
            }
            return contexts;
        }

        /// <summary>
        /// 处理打开 .csproj 文件所在目录按钮的点击事件。
        /// </summary>
        private void OpenCsprojDirectory_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "C# Project Files (*.csproj)|*.csproj",
                Title = "选择 .csproj 文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Contextes.Clear();
                var csprojFile = openFileDialog.FileName;
                var directoryPath = Path.GetDirectoryName(csprojFile) ?? string.Empty;

                CsprojPathTextBox.Text = directoryPath;
                Frameworks = GetTargetFrameworks(csprojFile);
                FrameworkComboBox.ItemsSource = Frameworks;
                if (Frameworks.Count > 0) FrameworkComboBox.SelectedIndex = 0;

                _ = LoadDataAsync(directoryPath, FrameworkComboBox.SelectedItem?.ToString());
            }
        }

        /// <summary>
        /// 异步加载 DbContext 列表并刷新界面项绑定。
        /// </summary>
        /// <param name="projectDirectory">项目目录。</param>
        /// <param name="framework">目标框架。</param>
        private async Task LoadDataAsync(string projectDirectory, string? framework)
        {
            try
            {
                SetLoading(true);
                var list = await Task.Run(() => GenerateContextList(projectDirectory, framework));
                Contextes.Clear();
                Contextes.AddRange(list);
                ContextListBox.ItemsSource = null;
                ContextListBox.ItemsSource = Contextes;
                MigrationsListBox.ItemsSource = null;
                SchemaGrid.ItemsSource = null;
            }
            finally
            {
                SetLoading(false);
            }
        }

        /// <summary>
        /// 切换加载遮罩并设置窗口可用状态。
        /// </summary>
        /// <param name="isLoading">是否显示加载中状态。</param>
        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            IsEnabled = !isLoading;
        }

        /// <summary>
        /// 当选择不同的 DbContext 时，刷新该上下文下的迁移列表。
        /// </summary>
        private void ContextListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadMigrationsForSelectedContext();
        }

        /// <summary>
        /// 当切换目标框架时，刷新 DbContext 列表和迁移列表。
        /// </summary>
        private async void FrameworkComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var projectDirectory = CsprojPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory)) return;

            await LoadDataAsync(projectDirectory, FrameworkComboBox.SelectedItem?.ToString());
            LoadMigrationsForSelectedContext();
        }

        /// <summary>
        /// 加载选中上下文的迁移列表。
        /// </summary>
        private void LoadMigrationsForSelectedContext()
        {
            MigrationsListBox.ItemsSource = null;
            SchemaGrid.ItemsSource = null;

            var selectedContext = ContextListBox.SelectedItem?.ToString();
            var projectDirectory = CsprojPathTextBox.Text;
            if (string.IsNullOrEmpty(selectedContext) || string.IsNullOrEmpty(projectDirectory)) return;

            var items = BuildMigrationItems(projectDirectory, selectedContext);
            MigrationsListBox.ItemsSource = items;
        }

        /// <summary>
        /// 迁移列表选择变化时，推断并展示该迁移前的表结构。
        /// </summary>
        private void MigrationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = MigrationsListBox.SelectedItem as MigrationItem;
            var selectedContext = ContextListBox.SelectedItem?.ToString();
            var projectDirectory = CsprojPathTextBox.Text;
            if (item == null || string.IsNullOrEmpty(selectedContext) || string.IsNullOrEmpty(projectDirectory)) return;

            var schema = InferSchemaUpTo(projectDirectory, selectedContext, item.Timestamp);
            SchemaGrid.ItemsSource = schema;
        }

        /// <summary>
        /// 构建迁移项集合，合并 .Designer 文件并按时间排序。
        /// </summary>
        /// <param name="projectDirectory">项目目录。</param>
        /// <param name="selectedContext">选中的上下文类型名称。</param>
        /// <returns>迁移项列表。</returns>
        private static List<MigrationItem> BuildMigrationItems(string projectDirectory, string selectedContext)
        {
            var entries = GetMigrationFileEntries(projectDirectory, selectedContext);

            // 合并 .Designer 与主文件，仅保留主文件；若只有 .Designer 也保留一份
            var grouped = entries
                .GroupBy(e => (e.Timestamp, e.Name), e => e)
                .Select(g => g.OrderBy(e => e.IsDesigner).First()) // 非 Designer 优先
                .ToList();

            var items = grouped
                .OrderBy(e => e.Timestamp) // 旧 -> 新
                .Select(e => new MigrationItem
                {
                    Timestamp = e.Timestamp,
                    Name = e.Name,
                    Display = string.IsNullOrEmpty(e.Timestamp) ? e.Name : ($"{e.Timestamp} - {e.Name}"),
                    FilePath = e.Path
                })
                .ToList();

            return items;
        }

        /// <summary>
        /// 获取指定上下文的迁移文件条目。
        /// </summary>
        /// <param name="projectDirectory">项目目录。</param>
        /// <param name="selectedContext">选中的上下文类型名称。</param>
        /// <returns>迁移文件条目列表。</returns>
        private static List<MigrationFileEntry> GetMigrationFileEntries(string projectDirectory, string selectedContext)
        {
            var list = new List<MigrationFileEntry>();
            var contextShort = selectedContext.Split('.').Last().Replace("Context", "");
            var baseMigrationsDir = Path.Combine(projectDirectory, "Migrations");
            var contextMigrationsDir = Path.Combine(baseMigrationsDir, contextShort);

            var dirs = new List<string>();
            if (Directory.Exists(contextMigrationsDir)) dirs.Add(contextMigrationsDir);
            if (Directory.Exists(baseMigrationsDir)) dirs.Add(baseMigrationsDir);

            var regex = new Regex(@"^(?<ts>\d{14})_(?<name>.+?)(?<designer>\.Designer)?$", RegexOptions.Compiled);

            foreach (var dir in dirs.Distinct())
            {
                foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.EndsWith("ModelSnapshot", System.StringComparison.OrdinalIgnoreCase)) continue;

                    var m = regex.Match(fileName);
                    string ts, name; bool isDesigner = false;
                    if (m.Success)
                    {
                        ts = m.Groups["ts"].Value;
                        name = m.Groups["name"].Value;
                        isDesigner = m.Groups["designer"].Success;
                    }
                    else
                    {
                        ts = string.Empty;
                        name = fileName;
                    }
                    list.Add(new MigrationFileEntry { Path = file, Timestamp = ts, Name = name, IsDesigner = isDesigner });
                }
            }

            return list;
        }

        /// <summary>
        /// 推断从第一条迁移至指定时间戳的表结构。
        /// </summary>
        /// <param name="projectDirectory">项目目录。</param>
        /// <param name="selectedContext">选中的上下文。</param>
        /// <param name="upToTimestamp">截止时间戳（不包含之后的迁移）。</param>
        /// <returns>列信息列表。</returns>
        private static List<ColumnInfo> InferSchemaUpTo(string projectDirectory, string selectedContext, string upToTimestamp)
        {
            var schema = new Dictionary<string, Dictionary<string, ColumnInfo>>(System.StringComparer.OrdinalIgnoreCase);
            var files = GetMigrationFileEntries(projectDirectory, selectedContext)
                .GroupBy(e => (e.Timestamp, e.Name))
                .Select(g => g.OrderBy(e => e.IsDesigner).First())
                .OrderBy(e => e.Timestamp)
                .ToList();

            foreach (var f in files)
            {
                // 如果没有时间戳（极少），视作最早
                if (!string.IsNullOrEmpty(upToTimestamp) && !string.IsNullOrEmpty(f.Timestamp) && string.Compare(f.Timestamp, upToTimestamp, System.StringComparison.Ordinal) > 0)
                {
                    break;
                }

                var content = File.ReadAllText(f.Path);
                ApplyCreateTable(content, schema);
                ApplyAddColumn(content, schema);
                ApplyDropColumn(content, schema);
                ApplyAlterColumn(content, schema);
            }

            // 展平
            return schema.Values.SelectMany(t => t.Values)
                .OrderBy(c => c.Table)
                .ThenBy(c => c.Column)
                .ToList();
        }

        /// <summary>
        /// 解析 CreateTable 片段并写入列信息。
        /// </summary>
        /// <param name="content">迁移文件内容。</param>
        /// <param name="schema">中间结构：表-列映射。</param>
        private static void ApplyCreateTable(string content, Dictionary<string, Dictionary<string, ColumnInfo>> schema)
        {
            // 匹配 CreateTable 的 name
            var tableNameRegex = new Regex(@"CreateTable\s*\(\s*name:\s*""(?<table>[^""]+)""", RegexOptions.Singleline);
            var columnLineRegex = new Regex(@"(?<col>\w+)\s*=\s*table\.Column<(?<type>[^>]+)>\([^)]*?nullable:\s*(?<nullable>true|false)", RegexOptions.Singleline);

            foreach (Match tMatch in tableNameRegex.Matches(content))
            {
                var table = tMatch.Groups["table"].Value;

                // 取该 CreateTable 后面的 columns 块（粗略抓取到下一个 Create/End 的前面）
                int start = tMatch.Index;
                int end = content.IndexOf("CreateTable", start + 1, System.StringComparison.Ordinal);
                if (end == -1) end = content.Length;
                var block = content.Substring(start, end - start);

                foreach (Match cMatch in columnLineRegex.Matches(block))
                {
                    var col = cMatch.Groups["col"].Value;
                    var clr = cMatch.Groups["type"].Value.Trim();
                    var nullable = cMatch.Groups["nullable"].Value.Equals("true", System.StringComparison.OrdinalIgnoreCase);

                    UpsertColumn(schema, table, col, clr, nullable);
                }
            }
        }

        /// <summary>
        /// 解析 AddColumn 片段并写入列信息。
        /// </summary>
        /// <param name="content">迁移文件内容。</param>
        /// <param name="schema">中间结构：表-列映射。</param>
        private static void ApplyAddColumn(string content, Dictionary<string, Dictionary<string, ColumnInfo>> schema)
        {
            // migrationBuilder.AddColumn<string>(name: "Error", table: "PallezingPositions", ..., nullable: false,
            var addRegex = new Regex(@"AddColumn<(?<type>[^>]+)>\([^)]*?name:\s*""(?<col>[^""]+)""[^)]*?table:\s*""(?<table>[^""]+)""[^)]*?nullable:\s*(?<nullable>true|false)", RegexOptions.Singleline);
            foreach (Match m in addRegex.Matches(content))
            {
                var table = m.Groups["table"].Value;
                var col = m.Groups["col"].Value;
                var clr = m.Groups["type"].Value.Trim();
                var nullable = m.Groups["nullable"].Value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
                UpsertColumn(schema, table, col, clr, nullable);
            }
        }

        /// <summary>
        /// 解析 DropColumn 片段并从结构中移除列。
        /// </summary>
        /// <param name="content">迁移文件内容。</param>
        /// <param name="schema">中间结构：表-列映射。</param>
        private static void ApplyDropColumn(string content, Dictionary<string, Dictionary<string, ColumnInfo>> schema)
        {
            var dropRegex = new Regex(@"DropColumn\([^)]*?name:\s*""(?<col>[^""]+)""[^)]*?table:\s*""(?<table>[^""]+)""", RegexOptions.Singleline);
            foreach (Match m in dropRegex.Matches(content))
            {
                var table = m.Groups["table"].Value;
                var col = m.Groups["col"].Value;
                if (schema.TryGetValue(table, out var cols))
                {
                    cols.Remove(col);
                }
            }
        }

        /// <summary>
        /// 解析 AlterColumn 片段并更新列信息。
        /// </summary>
        /// <param name="content">迁移文件内容。</param>
        /// <param name="schema">中间结构：表-列映射。</param>
        private static void ApplyAlterColumn(string content, Dictionary<string, Dictionary<string, ColumnInfo>> schema)
        {
            // migrationBuilder.AlterColumn<string>( name: "X", table: "T", nullable: true/false, ... )
            var alterRegex = new Regex(@"AlterColumn<(?<type>[^>]+)>\([^)]*?name:\s*""(?<col>[^""]+)""[^)]*?table:\s*""(?<table>[^""]+)""[^)]*?nullable:\s*(?<nullable>true|false)", RegexOptions.Singleline);
            foreach (Match m in alterRegex.Matches(content))
            {
                var table = m.Groups["table"].Value;
                var col = m.Groups["col"].Value;
                var clr = m.Groups["type"].Value.Trim();
                var nullable = m.Groups["nullable"].Value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
                UpsertColumn(schema, table, col, clr, nullable);
            }
        }

        /// <summary>
        /// 插入或更新列信息。
        /// </summary>
        /// <param name="schema">中间结构：表-列映射。</param>
        /// <param name="table">表名。</param>
        /// <param name="col">列名。</param>
        /// <param name="clr">CLR 类型名。</param>
        /// <param name="nullable">是否可空。</param>
        private static void UpsertColumn(Dictionary<string, Dictionary<string, ColumnInfo>> schema, string table, string col, string clr, bool nullable)
        {
            if (!schema.TryGetValue(table, out var cols))
            {
                cols = new Dictionary<string, ColumnInfo>(System.StringComparer.OrdinalIgnoreCase);
                schema[table] = cols;
            }
            var ci = new ColumnInfo
            {
                Table = table,
                Column = col,
                ClrType = ToCSharpTypeName(clr),
                IsNullable = nullable
            };
            cols[col] = ci;
        }

        /// <summary>
        /// 将常见 CLR 类型名转换为惯用 C# 类型名（如 System.Int32 -> int）。
        /// </summary>
        /// <param name="clr">CLR 类型名称。</param>
        /// <returns>C# 类型名称。</returns>
        private static string ToCSharpTypeName(string clr)
        {
            // clr 可能是 System.Nullable<int> 或者 int 等，这里统一输出常用 C# 名称
            clr = clr.Trim();
            // 处理可空类型泛型
            var nullableMatch = Regex.Match(clr, @"^System\.Nullable<(?<inner>.+)>$");
            if (nullableMatch.Success)
            {
                return ToCSharpTypeName(nullableMatch.Groups["inner"].Value) + "?";
            }

            return clr switch
            {
                "Int16" => "short",
                "System.Int16" => "short",
                "Int32" => "int",
                "System.Int32" => "int",
                "Int64" => "long",
                "System.Int64" => "long",
                "Boolean" => "bool",
                "System.Boolean" => "bool",
                "String" => "string",
                "System.String" => "string",
                "DateTime" => "DateTime",
                "System.DateTime" => "DateTime",
                "Decimal" => "decimal",
                "System.Decimal" => "decimal",
                "Double" => "double",
                "System.Double" => "double",
                "Single" => "float",
                "System.Single" => "float",
                "Byte[]" => "byte[]",
                "System.Byte[]" => "byte[]",
                "Guid" => "Guid",
                "System.Guid" => "Guid",
                _ => clr
            };
        }

        /// <summary>
        /// 处理“生成迁移”按钮的点击事件。
        /// </summary>
        private void GenerateMigration_Click(object sender, RoutedEventArgs e)
        {
            var selectedContext = ContextListBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedContext))
            {
                MessageBox.Show("请先选择一个数据库上下文。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var migrationVersion = MigrationNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(migrationVersion))
            {
                MessageBox.Show("请输入迁移版本标记。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var projectDirectory = CsprojPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(projectDirectory))
            {
                MessageBox.Show("请先选择 .csproj 文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var contextShort = selectedContext.Split('.').Last().Replace("Context", "");
            var outputDir = $"Migrations/{contextShort}";

            Directory.CreateDirectory(Path.Combine(projectDirectory, outputDir));

            // 防重复：按后缀名比较
            var existingSuffixNames = GetMigrationFileEntries(projectDirectory, selectedContext)
                .GroupBy(e => (e.Timestamp, e.Name))
                .Select(g => g.Key.Name)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

            if (existingSuffixNames.Contains(migrationVersion))
            {
                MessageBox.Show($"迁移文件 '{migrationVersion}' 已存在，请重新命名。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedFramework = FrameworkComboBox.SelectedItem?.ToString();
            var frameworkArg = string.IsNullOrWhiteSpace(selectedFramework) ? string.Empty : $" --framework {selectedFramework}";

            var result = ExecuteCommand($"dotnet ef migrations add {migrationVersion} --context {selectedContext} --output-dir {outputDir}{frameworkArg} --verbose", projectDirectory);

            if (result.ExitCode != 0)
            {
                ShowErrorDetailed("生成迁移失败", result.Output, result.Error, projectDirectory);
            }
            else
            {
                MessageBox.Show($"迁移 '{migrationVersion}' 已成功创建。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMigrationsForSelectedContext();
            }
        }

        /// <summary>
        /// 显示错误信息的通用方法（简单）。
        /// </summary>
        /// <param name="message">要显示的消息。</param>
        private static void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 显示详细错误信息，包含标准输出、错误输出、退出码和常见问题提示。
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="stdout">标准输出。</param>
        /// <param name="stderr">错误输出。</param>
        /// <param name="workingDir">工作目录。</param>
        private static void ShowErrorDetailed(string title, string stdout, string stderr, string workingDir)
        {
            var hints = new List<string>();
            var combined = (stdout + "\n" + stderr).ToLowerInvariant();

            if (combined.Contains("dotnet-ef") && combined.Contains("not") && combined.Contains("found"))
                hints.Add("未找到 dotnet-ef 工具，请安装：dotnet tool install --global dotnet-ef，并确保版本与项目 EF Core 兼容。");
            if (combined.Contains("unable to create an object of type") || combined.Contains("no design-time services"))
                hints.Add("无法创建 DbContext。请确保存在无参构造，或实现 IDesignTimeDbContextFactory<TContext>。");
            if (combined.Contains("the target framework") && combined.Contains("is not specified"))
                hints.Add("项目为多目标框架，请在界面中选择一个目标框架后重试。");
            if (combined.Contains("could not find a part of the path") || combined.Contains("no project was found"))
                hints.Add($"请确认工作目录正确：{workingDir}，且项目能正常构建。");

            var details = $"{title}\n\n工作目录: {workingDir}\n\n标准输出:\n{stdout}\n\n错误输出:\n{stderr}\n\n建议:\n- {string.Join("\n- ", hints)}";
            MessageBox.Show(details, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}