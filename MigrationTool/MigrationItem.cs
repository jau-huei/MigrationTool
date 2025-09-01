namespace MigrationTool
{
    /// <summary>
    /// 用于界面显示的迁移项。
    /// </summary>
    public class MigrationItem
    {
        /// <summary>
        /// 时间戳。
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;
        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 显示文本（时间戳 - 名称）。
        /// </summary>
        public string Display { get; set; } = string.Empty;
        /// <summary>
        /// 文件完整路径。
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
    }
}