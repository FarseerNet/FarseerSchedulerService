using System;
using FS.DI;
using FS.Extends;
using FSS.Abstract.Entity;
using FSS.Abstract.Server.RegisterCenter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FSS.Service.Controllers
{
    public class BaseController : ControllerBase
    {
        protected IHttpContextAccessor HttpContextAccessor { get; }

        /// <summary>
        /// 客户端请求IP
        /// </summary>
        protected ClientVO Client { get; }

        /// <summary>
        /// 客户端请求IP
        /// </summary>
        protected string ReqIp { get; }

        /// <summary>
        /// 客户端浏览器信息
        /// </summary>
        protected string UserAgent { get; }

        /// <summary>
        /// 请求源
        /// </summary>
        protected string Origin { get; }

        /// <summary>
        /// 识别会员、代理、管理
        /// </summary>
        private readonly string _type;

        public BaseController(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
            Client = new ClientVO
            {
                ClientIp   = HttpContextAccessor.HttpContext.Request.Headers["ClientIp"].ToString().Split(',')[0].Trim(),
                ClientName = HttpContextAccessor.HttpContext.Request.Headers["ClientName"],
                Id         = HttpContextAccessor.HttpContext.Request.Headers["ClientId"].ToString().ConvertType(0L),
                Jobs       = HttpContextAccessor.HttpContext.Request.Headers["ClientJobs"].ToString().Split(','),
                ActivateAt = DateTime.Now
            };
            
            // 更新客户端的使用时间
            IocManager.Instance.Resolve<IClientRegister>().UpdateClient(Client);
        }
    }
}