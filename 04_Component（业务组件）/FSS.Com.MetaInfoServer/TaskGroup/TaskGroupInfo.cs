using System.Threading.Tasks;
using FS.Cache.Redis;
using FS.Extends;
using FSS.Abstract.Entity.MetaInfo;
using FSS.Abstract.Server.MetaInfo;
using FSS.Com.MetaInfoServer.Abstract;
using FSS.Com.MetaInfoServer.TaskGroup.Dal;

namespace FSS.Com.MetaInfoServer.TaskGroup
{
    public class TaskGroupInfo : ITaskGroupInfo
    {
        public ITaskGroupAgent    TaskGroupAgent    { get; set; }
        public IRedisCacheManager RedisCacheManager { get; set; }

        /// <summary>
        /// 获取任务信息
        /// </summary>
        public Task<TaskGroupVO> ToInfoAsync(int id)
        {
            return RedisCacheManager.CacheManager.ToEntityAsync<TaskGroupVO>(TaskGroupCache.Key,
                id.ToString(),
                _ => TaskGroupAgent.ToEntityAsync(id).MapAsync<TaskGroupVO, TaskGroupPO>(),
                o => o.Id);
        }

        /// <summary>
        /// 从数据库中取出并保存
        /// </summary>
        public async Task<TaskGroupVO> ToInfoByDbAsync(int id)
        {
            var entity = await TaskGroupAgent.ToEntityAsync(id).MapAsync<TaskGroupVO, TaskGroupPO>();
            await RedisCacheManager.CacheManager.SaveAsync(TaskGroupCache.Key,
                entity,
                id.ToString());
            return entity;
        }
    }
}