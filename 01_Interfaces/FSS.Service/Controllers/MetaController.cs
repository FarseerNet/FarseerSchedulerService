﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FS.Core;
using FS.Core.Net;
using FS.Extends;
using FSS.Application.Clients.Dto;
using FSS.Application.Clients.Interface;
using FSS.Application.Log.TaskLog.Entity;
using FSS.Application.Log.TaskLog.Interface;
using FSS.Application.Tasks.TaskGroup.Entity;
using FSS.Application.Tasks.TaskGroup.Interface;
using FSS.Domain.Tasks.TaskGroup.Entity;
using FSS.Service.Request;
using Microsoft.AspNetCore.Mvc;

namespace FSS.Service.Controllers
{
    /// <summary>
    /// 基础信息
    /// </summary>
    [ApiController]
    [Route("meta")]
    public class MetaController : ControllerBase
    {
        public IClientApp    ClientApp    { get; set; }
        public ITaskLogApp   TaskLogApp   { get; set; }
        public ITaskGroupApp TaskGroupApp { get; set; }

        /// <summary>
        /// 客户端拉取任务
        /// </summary>
        [HttpPost]
        [Route("GetClientList")]
        public async Task<ApiResponseJson<List<ClientDTO>>> GetClientList()
        {
            // 取出全局客户端列表
            var lst = await ClientApp.ToListAsync();
            return await ApiResponseJson<List<ClientDTO>>.SuccessAsync(lst);
        }

        /// <summary>
        /// 取出全局客户端数量
        /// </summary>
        [HttpPost]
        [Route("GetClientCount")]
        public async Task<ApiResponseJson<long>> GetClientCount()
        {
            // 取出全局客户端列表
            var count = await ClientApp.GetCountAsync();
            return await ApiResponseJson<long>.SuccessAsync(count);
        }

        /// <summary>
        /// 复制任务组
        /// </summary>
        [HttpPost]
        [Route("CopyTaskGroup")]
        public async Task<ApiResponseJson<int>> CopyTaskGroup(OnlyIdRequest request)
        {
            var taskGroupId = await TaskGroupApp.CopyTaskGroup(request.Id);
            return await ApiResponseJson<int>.SuccessAsync("复制成功", taskGroupId);
        }

        /// <summary>
        /// 删除任务组
        /// </summary>
        [HttpPost]
        [Route("DeleteTaskGroup")]
        public async Task<ApiResponseJson> DeleteTaskGroup(OnlyIdRequest request)
        {
            await TaskGroupApp.DeleteAsync(request.Id);
            return await ApiResponseJson.SuccessAsync("删除成功", request.Id);
        }

        /// <summary>
        /// 获取任务组信息
        /// </summary>
        [HttpPost]
        [Route("GetTaskGroupInfo")]
        public async Task<ApiResponseJson<TaskGroupDTO>> GetTaskGroupInfo(OnlyIdRequest request)
        {
            var info = await TaskGroupApp.ToEntityAsync(request.Id).MapAsync<TaskGroupDTO, TaskGroupDO>();
            return await ApiResponseJson<TaskGroupDTO>.SuccessAsync(info);
        }

        /// <summary>
        /// 同步缓存到数据库
        /// </summary>
        [HttpPost]
        [Route("SyncCacheToDb")]
        public async Task<ApiResponseJson<List<TaskGroupDO>>> SyncCacheToDb()
        {
            await TaskGroupApp.SyncTaskGroup();
            return await ApiResponseJson.SuccessAsync();
        }

        /// <summary>
        /// 获取全部任务组列表
        /// </summary>
        [HttpPost]
        [Route("GetTaskGroupList")]
        public async Task<ApiResponseJson<List<TaskGroupDTO>>> GetTaskGroupList()
        {
            var lst = await TaskGroupApp.ToListAsync().MapAsync<TaskGroupDTO, TaskGroupDO>();
            return await ApiResponseJson<List<TaskGroupDTO>>.SuccessAsync(lst);
        }

        /// <summary>
        /// 获取任务组数量
        /// </summary>
        [HttpPost]
        [Route("GetTaskGroupCount")]
        public async Task<ApiResponseJson<long>> GetTaskGroupCount()
        {
            var count = await TaskGroupApp.GetTaskGroupCount();
            return await ApiResponseJson<long>.SuccessAsync(count);
        }

        /// <summary>
        /// 获取未执行的任务数量
        /// </summary>
        [HttpPost]
        [Route("GetTaskGroupUnRunCount")]
        public async Task<ApiResponseJson<long>> GetTaskGroupUnRunCount()
        {
            var count = await TaskGroupApp.ToUnRunCountAsync();
            return await ApiResponseJson<long>.SuccessAsync(count);
        }

        /// <summary>
        /// 添加任务组
        /// </summary>
        [HttpPost]
        [Route("AddTaskGroup")]
        public async Task<ApiResponseJson<int>> AddTaskGroup(TaskGroupDTO request)
        {
            if (request.Caption == null || request.Cron == null || request.Data == null || request.JobName == null)
            {
                return await ApiResponseJson<int>.ErrorAsync("标题、时间间隔、传输数据、Job名称 必须填写");
            }
            request.Id = await TaskGroupApp.AddAsync(request);
            return await ApiResponseJson<int>.SuccessAsync("添加成功", request.Id);
        }

        /// <summary>
        /// 修改任务组或设置Enable
        /// </summary>
        [HttpPost]
        [Route("SaveTaskGroup")]
        public async Task<ApiResponseJson> SaveTaskGroup(TaskGroupDTO request)
        {
            await TaskGroupApp.Save(request);
            return await ApiResponseJson.SuccessAsync();
        }

        /// <summary>
        /// 今日执行失败数量
        /// </summary>
        [HttpPost]
        [Route("TodayTaskFailCount")]
        public async Task<ApiResponseJson<int>> TodayTaskFailCount()
        {
            var count = await TaskGroupApp.TodayFailCountAsync();
            return await ApiResponseJson<int>.SuccessAsync(count);
        }

        /// <summary>
        /// 获取进行中的任务
        /// </summary>
        [HttpPost]
        [Route("GetTaskUnFinishList")]
        public async Task<ApiResponseJson<List<TaskDTO>>> GetTaskUnFinishList(OnlyTopRequest request)
        {
            var lst = await TaskGroupApp.GetTaskUnFinishList(request.Top);
            return await ApiResponseJson<List<TaskDTO>>.SuccessAsync(lst);
        }

        /// <summary>
        /// 获取在用的任务
        /// </summary>
        [HttpPost]
        [Route("GetEnableTaskList")]
        public async Task<ApiResponseJson<DataSplitList<TaskDTO>>> GetEnableTaskList(GetAllTaskListRequest request)
        {
            var lst = TaskGroupApp.GetEnableTaskList(request.Status, request.PageSize, request.PageIndex);
            return await ApiResponseJson<DataSplitList<TaskDTO>>.SuccessAsync(lst);
        }

        /// <summary>
        /// 获取指定任务组的任务列表
        /// </summary>
        [HttpPost]
        [Route("GetTaskList")]
        public async Task<ApiResponseJson<DataSplitList<TaskDTO>>> GetTaskList(GetTaskListRequest request)
        {
            var lst = await TaskGroupApp.ToListAsync(request.GroupId, request.PageSize, request.PageIndex);
            return await ApiResponseJson<DataSplitList<TaskDTO>>.SuccessAsync(lst);
        }

        /// <summary>
        /// 获取已完成的任务列表
        /// </summary>
        [HttpPost]
        [Route("GetTaskFinishList")]
        public async Task<ApiResponseJson<DataSplitList<TaskDTO>>> GetTaskFinishList(PageSizeRequest request)
        {
            var lst = await TaskGroupApp.ToFinishListAsync(request.PageSize, request.PageIndex);
            return await ApiResponseJson<DataSplitList<TaskDTO>>.SuccessAsync(lst);
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        [HttpPost]
        [Route("CancelTask")]
        public async Task<ApiResponseJson> CancelTask(OnlyIdRequest request)
        {
            await TaskGroupApp.CancelTask(request.Id);
            return await ApiResponseJson.SuccessAsync();
        }

        /// <summary>
        /// 获取日志
        /// </summary>
        [HttpPost]
        [Route("GetRunLogList")]
        public async Task<ApiResponseJson<DataSplitList<TaskLogDTO>>> GetRunLogList(GetRunLogRequest request)
        {
            var lst = TaskLogApp.GetList(request.JobName, request.LogLevel, request.PageSize, request.PageIndex);
            return await ApiResponseJson<DataSplitList<TaskLogDTO>>.SuccessAsync(lst);
        }
    }
}