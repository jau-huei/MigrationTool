namespace MigrationTool
{
    /// <summary>
    /// 表示一个迁移文件条目。
    /// </summary>
    public class MigrationFileEntry
    {
        /// <summary>
        /// 文件完整路径。
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// 时间戳（yyyyMMddHHmmss）。
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;
        /// <summary>
        /// 迁移名称（不含时间戳）。
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 是否为 .Designer 文件。
        /// </summary>
        public bool IsDesigner { get; set; }
    }
}