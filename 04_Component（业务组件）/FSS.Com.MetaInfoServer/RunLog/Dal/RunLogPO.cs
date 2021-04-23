using System;
using FS.Core.Mapping.Attribute;
using Microsoft.Extensions.Logging;

namespace FSS.Com.MetaInfoServer.RunLog.Dal
{
    /// <summary>
    /// 运行日志
    /// </summary>
    public class RunLogPO
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Field(Name = "id",IsPrimaryKey = true)]
        public long Id { get; set; }
        
        /// <summary>
        /// 任务记录ID
        /// </summary>
        [Field(Name = "task_id")]
        public int TaskId { get; set; }
        
        /// <summary>
        /// 日志级别
        /// </summary>
        [Field(Name = "log_level")]
        public LogLevel LogLevel { get; set; }
        
        /// <summary>
        /// 任务的标题
        /// </summary>
        [Field(Name = "content")]
        public string Content { get; set; }
        
        /// <summary>
        /// 日志时间
        /// </summary>
        [Field(Name = "create_at")]
        public DateTime CreateAt { get; set; }
    }
}