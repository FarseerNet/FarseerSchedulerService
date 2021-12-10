using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FS.Core.LinkTrack;
using FSS.Abstract.Server.MetaInfo;
using FSS.Application.Tasks.TaskGroup.Interface;
using FSS.Infrastructure.Repository.Tasks.Enum;

namespace FSS.Service.Background
{
    /// <summary>
    /// 检测完成状态的任务
    /// </summary>
    public class CheckFinishStatusService : BackgroundServiceTrace
    {
        public ITaskGroupApp TaskGroupApp { get; set; }
        public ITaskInfo     TaskInfo     { get; set; }
        public ITaskAdd      TaskAdd      { get; set; }

        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 取出任务组
                var dicTaskGroup = await TaskGroupApp.ToListAsync();

                // 只检测Enable状态的任务组
                foreach (var taskGroupDO in dicTaskGroup.Where(o => o.IsEnable))
                {
                    // 状态必须是 完成的
                    if (taskGroupDO.Task.Status != EumTaskType.Fail && taskGroupDO.Task.Status != EumTaskType.Success) continue;
                    // 加个时间，来限制并发
                    if ((DateTime.Now - taskGroupDO.Task.RunAt).TotalSeconds < 3) continue;
                    
                    await taskGroupDO.CreateTask();
                    await Task.Delay(200, stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}