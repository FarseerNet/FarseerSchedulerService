using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FS.Cache;
using FS.Extends;
using FSS.Domain.Tasks.TaskGroup.Entity;
using FSS.Domain.Tasks.TaskGroup.Interface;
using FSS.Infrastructure.Repository.TaskGroup.Interface;
using FSS.Infrastructure.Repository.TaskGroup.Model;
using FSS.Infrastructure.Repository.Tasks.Interface;

namespace FSS.Domain.Tasks.TaskGroup
{
    public class TaskGroupService : ITaskGroupService
    {
        public ITaskGroupAgent TaskGroupAgent { get; set; }
        public ITaskAgent      TaskAgent      { get; set; }

        /// <summary>
        /// 删除任务组
        /// </summary>
        public async Task DeleteAsync(int taskGroupId)
        {
            var taskGroup = await TaskGroupAgent.ToEntityAsync(EumCacheStoreType.Redis, taskGroupId).MapAsync<TaskGroupDO, TaskGroupPO>();
            await taskGroup.DeleteAsync();
        }

        /// <summary>
        /// 创建新的Task缓存
        /// </summary>
        public async Task<TaskDO> CreateTaskAsync(TaskGroupDO taskGroup)
        {
            taskGroup.CreateTask();
            await TaskGroupAgent.SaveAsync(taskGroup.Map<TaskGroupPO>());
            return taskGroup.Task;
        }

        /// <summary>
        /// 获取所有任务组中的任务
        /// </summary>
        public async Task<List<TaskDO>> ToGroupListAsync()
        {
            var lstTaskGroup = await TaskGroupAgent.ToListAsync(EumCacheStoreType.Redis).MapAsync<TaskGroupDO, TaskGroupPO>();

            foreach (var taskGroupPO in lstTaskGroup)
            {
                if (taskGroupPO.Task == null) await CreateTaskAsync(taskGroupPO);
            }
            return lstTaskGroup.Select(o => o.Task).Where(o => o != null).ToList();
        }
    }
}