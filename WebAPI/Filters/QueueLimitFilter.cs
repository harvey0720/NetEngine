﻿using Common;
using DistributedLock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebAPI.Libraries;

namespace WebAPI.Filters
{


    /// <summary>
    /// 队列过滤器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class QueueLimitFilter : Attribute, IActionFilter
    {


        /// <summary>
        /// 是否使用 参数
        /// </summary>
        public bool IsUseParameter { get; set; }


        /// <summary>
        /// 是否使用 Token
        /// </summary>
        public bool IsUseToken { get; set; }


        /// <summary>
        /// 是否阻断重复请求
        /// </summary>
        public bool IsBlock { get; set; }


        private IDisposable? LockHandle { get; set; }




        void IActionFilter.OnActionExecuting(ActionExecutingContext context)
        {
            string key = context.ActionDescriptor.DisplayName!;

            if (IsUseToken)
            {
                var token = context.HttpContext.Request.Headers.Where(t => t.Key == "Authorization").Select(t => t.Value).FirstOrDefault();
                key = key + "_" + token;
            }

            if (IsUseParameter)
            {
                var parameter = JsonHelper.ObjectToJson(context.HttpContext.GetParameter());
                key = key + "_" + parameter;
            }

            key = "QueueLimit_" + Common.CryptoHelper.GetMD5(key);

            try
            {
                var distLock = context.HttpContext.RequestServices.GetRequiredService<IDistributedLock>();

                while (true)
                {
                    var handle = distLock.TryLock(key);
                    if (handle != null)
                    {
                        LockHandle = handle;
                        break;
                    }
                    else
                    {
                        if (IsBlock)
                        {
                            context.Result = new BadRequestObjectResult(new { errMsg = "请勿频繁操作" });
                            break;
                        }
                        else
                        {
                            Thread.Sleep(200);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<QueueLimitFilter>>();
                logger.LogError(ex, "队列限制模块异常-In");
            }
        }



        void IActionFilter.OnActionExecuted(ActionExecutedContext context)
        {
            try
            {
                LockHandle?.Dispose();
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<QueueLimitFilter>>();
                logger.LogError(ex, "队列限制模块异常-Out");
            }
        }


    }
}
