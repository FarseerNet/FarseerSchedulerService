using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FSS.Domain.Tasks.TaskGroup.Entity;
using FSS.Domain.Tasks.TaskGroup.Enum;

namespace FSS.Domain.Tasks.TaskGroup.Repository
{
    public interface ITaskGroupRepository
    {
        /// <summary>
        /// 获取任务组信息
        /// </summary>
        Task<TaskGroupDO> ToEntityAsync(int taskGroupId);

        /// <summary>
        /// 今日执行失败数量
        /// </summary>
        Task<int> TodayFailCountAsync();
        /// <summary>
        /// 当前任务组下所有任务的执行速度
        /// </summary>
        Task<List<long>> ToTaskSpeedListAsync(int taskGroupId);

        /// <summary>
        /// 获取所有任务组中的任务
        /// </summary>
        Task<List<TaskGroupDO>> ToListAsync();

        /// <summary>
        /// 获取任务组数量
        /// </summary>
        Task<long> GetTaskGroupCountAsync();

        /// <summary>
        /// 获取指定任务组执行成功的任务列表
        /// </summary>
        Task<List<TaskDO>> ToFinishListAsync(int taskGroupId, int top);
        /// <summary>
        /// 创建任务
        /// </summary>
        Task AddTaskAsync(TaskDO taskDO);

        /// <summary>
        /// 添加任务组
        /// </summary>
        Task<int> AddAsync(TaskGroupDO taskGroupDO);
        /// <summary>
        /// 保存任务组信息
        /// </summary>
        Task SaveAsync(TaskGroupDO taskGroupDO);
        /// <summary>
        /// 删除任务组
        /// </summary>
        Task DeleteAsync(int taskGroupId);
        /// <summary>
        /// 同步数据
        /// </summary>
        Task SyncToData();
        /// <summary>
        /// 获取所有任务组中的任务
        /// </summary>
        Task<List<TaskGroupDO>> GetMyCanSchedulerTaskGroup(string[] jobs, TimeSpan ts, int count);
        /// <summary>
        /// 获取未执行的任务数量
        /// </summary>
        Task<int> ToUnRunCountAsync();
        /// <summary>
        /// 获取执行中的任务
        /// </summary>
        Task<List<TaskGroupDO>> ToSchedulerWorkingListAsync();
        /// <summary>
        /// 获取指定任务组的任务列表（FOPS）
        /// </summary>
        Task<List<TaskDO>> ToListAsync(int groupId, int pageSize, int pageIndex, out int totalCount);
        /// <summary>
        /// 获取已完成的任务列表
        /// </summary>
        Task<List<TaskDO>> ToFinishListAsync(int pageSize, int pageIndex, out int totalCount);
        Task<List<TaskGroupDO>> ToListAsync(long clientId);
        /// <summary>
        /// 获取进行中的任务
        /// </summary>
        Task<List<TaskGroupDO>> GetTaskUnFinishList(int top);
        /// <summary>
        /// 获取在用的任务组
        /// </summary>
        List<TaskGroupDO> GetEnableTaskList(EumTaskType? status, int pageSize, int pageIndex, out int totalCount);
    }
}