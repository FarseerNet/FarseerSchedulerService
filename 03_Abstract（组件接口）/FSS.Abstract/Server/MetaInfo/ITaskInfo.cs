using System.Collections.Generic;
using System.Threading.Tasks;
using FS.DI;
using FSS.Abstract.Entity.MetaInfo;

namespace FSS.Abstract.Server.MetaInfo
{
    public interface ITaskInfo: ISingletonDependency
    {
        /// <summary>
        /// 获取当前任务组的任务
        /// </summary>
        Task<TaskVO> ToInfoByGroupIdAsync(int taskGroupId);

        /// <summary>
        /// 计算任务的平均运行速度
        /// </summary>
        Task<long> StatAvgSpeedAsync(int taskGroupId);

        /// <summary>
        /// 今日执行失败数量
        /// </summary>
        Task<int> TodayFailCountAsync();
    }
}