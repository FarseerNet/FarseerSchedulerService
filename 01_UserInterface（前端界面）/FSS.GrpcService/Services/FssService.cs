using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FS.DI;
using FS.Extends;
using FSS.Abstract.Enum;
using FSS.Abstract.Server.MetaInfo;
using FSS.Abstract.Server.RegisterCenter;
using FSS.Abstract.Server.RemoteCall;
using FSS.Abstract.Server.Scheduler;
using FSS.Com.RemoteCallServer.ClientNotify;
using Grpc.Core;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace FSS.GrpcService.Services
{
    public class FssService : FssServer.FssServerBase
    {
        private readonly IIocManager     _ioc;
        private readonly IClientRegister _clientRegister;
        private readonly ITaskGroupList  _taskGroupList;

        public FssService(IIocManager ioc)
        {
            _ioc            = ioc;
            _clientRegister = _ioc.Resolve<IClientRegister>();
            _taskGroupList   = _ioc.Resolve<ITaskGroupList>();
        }

        /// <summary>
        /// 建立通道
        /// </summary>
        public override async Task Channel(IAsyncStreamReader<ChannelRequest> requestStream, IServerStreamWriter<CommandResponse> responseStream, ServerCallContext context)
        {
            var serverHost = $"{context.Host}_{context.Peer}";
            var clientIp   = context.RequestHeaders.GetValue("client_ip");
            try
            {
                
                await foreach (var registerRequest in requestStream.ReadAllAsync())
                {
                    // 判断客户端是否已经被踢了
                    if (registerRequest.Command !="Register" &&!_clientRegister.IsExists(serverHost)) return;
                    
                    var iocName = $"fss_server_{registerRequest.Command}";
                    if (!_ioc.IsRegistered(iocName))
                        await _ioc.Resolve<IClientResponse>().PrintAsync(responseStream, $"请求命令{registerRequest.Command}，不在服务端识别的范围中");
                    else
                        // 交由具体功能处理实现执行
                        await _ioc.Resolve<IRemoteCommand>(iocName).InvokeAsync(context, requestStream, responseStream);
                }
            }
            catch (Exception e)
            {
                if (e.InnerException is not ConnectionAbortedException) _ioc.Logger<FssService>().LogError(e.Message);
            }
            finally
            {
                await _ioc.Resolve<IClientRegister>().RemoveAsync(serverHost);
                _ioc.Logger<FssService>().LogWarning($"客户端{clientIp} 断开连接");
            }
        }

        /// <summary>
        /// 客户端执行JOB，并实时同步状态
        /// </summary>
        public override async Task<CommandResponse> JobInvoke(IAsyncStreamReader<JobInvokeRequest> requestStream, ServerCallContext context)
        {
            Stopwatch sw          = Stopwatch.StartNew();
            var       logger      = _ioc.Logger<ITaskScheduler>();
            var       taskGroupId = context.RequestHeaders.GetValue("task_group_id").ConvertType(0);
            var       taskId      = context.RequestHeaders.GetValue("task_id").ConvertType(0);
            var       serverHost  = $"{context.Host}_{context.Peer}";

            var runLogAdd = _ioc.Resolve<IRunLogAdd>();
            var task      = await _ioc.Resolve<ITaskInfo>().ToInfoByGroupIdAsync(taskGroupId);

            if (!_clientRegister.IsExists(serverHost)) return null;

            // 先读本地缓存
            var lstGroup = await _taskGroupList.ToListByMemoryAsync();
            lstGroup.TryGetValue(taskGroupId, out var taskGroup);
            
            if (taskGroup == null)
            {
                await runLogAdd.AddAsync(task.TaskGroupId, taskId, LogLevel.Warning, $"所属的任务组：{task.TaskGroupId} 不存在");
                return (CommandResponse) _ioc.Resolve<IClientResponse>().Print($"指定的TaskId：{taskId} 所属的任务组：{task.TaskGroupId} 不存在");
            }
            
            // 服务端当前任务ID与客户端请求的任务ID不一致
            if (task.Id > taskId)
            {
                await runLogAdd.AddAsync(taskGroup, taskId, LogLevel.Warning, $"与服务端正在处理的Task： {taskGroup.Caption}（{taskGroup.JobName}） 不一致");
                return (CommandResponse) _ioc.Resolve<IClientResponse>().Print($"指定的TaskId：{taskId} {taskGroup.Caption}（{taskGroup.JobName}） 与服务端正在处理的Task：{task.Id} 不一致");
            }

            var taskUpdate      = _ioc.Resolve<ITaskUpdate>();
            var clientResponse  = _ioc.Resolve<ClientResponse>();
            var clientRegister  = _ioc.Resolve<IClientRegister>();
            
            try
            {
                // 更新客户端的使用时间
                clientRegister.UpdateUseAt(serverHost, DateTime.Now);

                // 不相等，说明被覆盖了（JOB请求慢了。被调度重新执行了）
                if (task.ClientHost != serverHost)
                {
                    await runLogAdd.AddAsync(taskGroup, taskId, LogLevel.Warning, $"{task.ClientHost}与本次请求{serverHost} 不一致，忽略本次请求");
                    return (CommandResponse) clientResponse.Ignore($"任务ID： {taskGroup.Caption}（{taskGroup.JobName}） ，{task.ClientHost}与本次请求{serverHost} 不一致，忽略本次请求");
                }

                // 更新Task元信息
                if (task.Status == EumTaskType.Scheduler)
                {
                    task.RunAt = DateTime.Now; // 首次执行，记录时间
                }
                
                task.Status = EumTaskType.Working;
                //await taskUpdate.UpdateAsync(task); // 先不用更新缓存，下面再更新

                // 更新group元信息
                taskGroup = await _ioc.Resolve<ITaskGroupInfo>().ToInfoAsync(taskGroupId);
                taskGroup.RunCount++;
                taskGroup.ActivateAt = DateTime.Now;
                taskGroup.LastRunAt  = DateTime.Now;

                // 实时同步JOB执行状态
                await foreach (var jobRequest in requestStream.ReadAllAsync())
                {
                    // 更新Task
                    task.Progress = jobRequest.Progress;
                    task.Status   = (EumTaskType) jobRequest.Status;
                    task.RunSpeed = jobRequest.RunSpeed;

                    // 如果有日志
                    if (jobRequest.Log != null && !string.IsNullOrWhiteSpace(jobRequest.Log.Log))
                    {
                        await runLogAdd.AddAsync(taskGroup, task.Id, (LogLevel) jobRequest.Log.LogLevel, jobRequest.Log.Log);
                    }
                    
                    // 如果是成功、错误状态，则要立即更新数据库
                    switch (task.Status)
                    {
                        case EumTaskType.Fail:
                        case EumTaskType.Success:
                        case EumTaskType.ReScheduler:
                            taskGroup.LastRunAt  = DateTime.Now;
                            taskGroup.ActivateAt = DateTime.Now;
                            if (!string.IsNullOrWhiteSpace(jobRequest.Data)) taskGroup.Data = jobRequest.Data;
                            // 客户端设置了动态时间
                            if (jobRequest.NextTimespan > 0) taskGroup.NextAt = DateTime.Now.AddMilliseconds(jobRequest.NextTimespan);
                            await taskUpdate.SaveFinishAsync(task, taskGroup);
                            break;
                        default:
                            await taskUpdate.UpdateAsync(task);
                            break;
                    }
                }
                
                if (task.Status != EumTaskType.Success)
                {
                    return (CommandResponse) _ioc.Resolve<IClientResponse>().Print($"任务：{task.TaskGroupId}-{task.Id} 执行失败");
                }
                else
                {
                    return (CommandResponse) _ioc.Resolve<IClientResponse>().Print($"任务组：TaskGroupId={task.TaskGroupId}，Caption={taskGroup.Caption}，JobName={taskGroup.JobName}，TaskId={task.Id} 执行成功，耗时：{task.RunSpeed} ms");
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null) e = e.InnerException;
                task.Status = EumTaskType.Fail;
                await taskUpdate.SaveFinishAsync(task, taskGroup);
                logger.LogError(e.Message);
                return (CommandResponse) _ioc.Resolve<IClientResponse>().Print(e.Message);
            }
        }
    }
}