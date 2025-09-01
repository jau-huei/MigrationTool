namespace MigrationTool
{
    /// <summary>
    /// 列信息（表/列/类型/是否可空）。
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// 表名。
        /// </summary>
        public string Table { get; set; } = string.Empty;
        /// <summary>
        /// 列名。
        /// </summary>
        public string Column { get; set; } = string.Empty;
        /// <summary>
        /// C# 类型名。
        /// </summary>
        public string ClrType { get; set; } = string.Empty;
        /// <summary>
        /// 是否可空。
        /// </summary>
        public bool IsNullable { get; set; }
    }
}