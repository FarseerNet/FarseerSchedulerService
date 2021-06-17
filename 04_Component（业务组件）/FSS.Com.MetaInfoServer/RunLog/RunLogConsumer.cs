using System;
using System.Threading.Tasks;
using FS.Cache.Redis;
using FS.DI;
using FS.MQ.RedisStream;
using FS.MQ.RedisStream.Attr;
using FSS.Com.MetaInfoServer.Abstract;
using FSS.Com.MetaInfoServer.RunLog.Dal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace FSS.Com.MetaInfoServer.RunLog
{
    /// <summary>
    /// 写入日志
    /// </summary>
    [Consumer(Enable = true, RedisName = "default", GroupName = "insert", QueueName = "RunLogQueue", PullCount = 1, ConsumeThreadNums = 1)]
    public class RunLogConsumer : IListenerMessage
    {
        public          IRunLogAgent RunLogAgent { get; set; }
        public          IIocManager  IocManager  { get; set; }
        static readonly bool         UseEs;

        static RunLogConsumer()
        {
            var configurationSection = FS.DI.IocManager.Instance.Resolve<IConfigurationRoot>().GetSection("ElasticSearch:0:Server").Value;
            UseEs = !string.IsNullOrWhiteSpace(configurationSection);
        }

        /// <summary>
        /// 消费
        /// </summary>
        public async Task<bool> Consumer(StreamEntry[] messages, ConsumeContext ea)
        {
            foreach (var message in messages)
            {
                var runLogPO = JsonConvert.DeserializeObject<RunLogPO>(message.Values[0].Value.ToString());
                if (UseEs)
                {
                    IocManager.Logger<RunLogConsumer>().LogWarning("runLogPO写入ES");
                    if (!await LogContext.Data.RunLog.InsertAsync(runLogPO))
                    {
                        return false;
                    }
                }
                else await RunLogAgent.AddAsync(runLogPO);
            }

            return true;
        }

        public Task<bool> FailureHandling(StreamEntry[] messages, ConsumeContext ea) => throw new System.NotImplementedException();
    }
}