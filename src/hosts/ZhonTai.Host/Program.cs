﻿using Autofac.Core;
using FreeRedis;
using FreeScheduler;
using Lazy.SlideCaptcha.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Savorboard.CAP.InMemoryMessageQueue;
using System.Linq;
using System.Reflection;
using ZhonTai;
using ZhonTai.Admin.Core;
using ZhonTai.Admin.Core.Captcha;
using ZhonTai.Admin.Core.Configs;
using ZhonTai.Admin.Core.Consts;
using ZhonTai.Admin.Core.Startup;
using ZhonTai.Admin.Tools.Cache;
using ZhonTai.Admin.Tools.TaskScheduler;
using ZhonTai.ApiUI;
using ZhonTai.Common.Helpers;

new HostApp(new HostAppOptions
{
	//配置后置服务
	ConfigurePostServices = context =>
	{
        //var appConfig = ConfigHelper.Get<AppConfig>("appconfig", context.Environment.EnvironmentName);
        //Assembly[] assemblies = DependencyContext.Default.RuntimeLibraries
        //    .Where(a => appConfig.AssemblyNames.Contains(a.Name))
        //    .Select(o => Assembly.Load(new AssemblyName(o.Name))).ToArray();
        //context.Services.AddCap(config =>
        //{
        //    config.UseInMemoryStorage();
        //    config.UseInMemoryMessageQueue();
        //    config.UseDashboard();
        //}).AddSubscriberAssembly(assemblies);

        //context.Services.AddTiDb(context);

        //添加任务调度
        context.Services.AddTaskScheduler(DbKeys.AppDb, options =>
        {
            options.ConfigureFreeSql = freeSql =>
            {
                freeSql.CodeFirst
                //配置任务表
                .ConfigEntity<TaskInfo>(a =>
                {
                    a.Name("app_task");
                })
                //配置任务日志表
                .ConfigEntity<TaskLog>(a =>
                {
                    a.Name("app_task_log");
                });
            };

            //模块任务处理器
            options.TaskHandler = new TaskHandler(options.FreeSql);
        });

        //oss文件上传
        //context.Services.AddOSS();

        //滑块验证码
        context.Services.AddSlideCaptcha(context.Configuration, options =>
        {
            options.StoreageKeyPrefix = CacheKeys.Captcha;
        });
        context.Services.AddScoped<ISlideCaptcha, SlideCaptcha>();

    },

    //配置Autofac容器
    ConfigureAutofacContainer = (builder, context) =>
    {

    },

    //配置Mvc
    ConfigureMvcBuilder = (builder, context) =>
    {
    },

	//配置后置中间件
	ConfigurePostMiddleware = context =>
    {
		var app = context.App;
		var env = app.Environment;
		var appConfig = app.Services.GetService<AppConfig>();

		#region 新版Api文档
		if (env.IsDevelopment() || appConfig.ApiUI.Enable)
		{
            app.UseApiUI(options =>
            {
                options.RoutePrefix = appConfig.ApiUI.RoutePrefix;
                var routePath = options.RoutePrefix.NotNull() ? $"{options.RoutePrefix}/" : "";
                appConfig.Swagger.Projects?.ForEach(project =>
                {
                    options.SwaggerEndpoint($"/{routePath}swagger/{project.Code.ToLower()}/swagger.json", project.Name);
                });
            });
		}
        #endregion
	}
}).Run(args);

#if DEBUG
public partial class Program { }
#endif