## FSS是什么？
基于.NET CORE 5 开发的分布式的任务调度平台（跨语言），包含服务端、客户端。

服务端：会运行在docker环境下（唯一推荐使用）

客户端：就是我们自己的JOB程序，可以是控制台（Console）程序，也可以ASP.NET CORE程序

服务端与客户端之间采用Grpc通讯

客户端（JOB）可以多实例运行（比如K8S中副本设为3），由调度平台（服务端）根据时间策略进行远程调度。

与.NET CORE高度契合，利用Farseer.Net.Job组件，可以实现非常简单的接入

与Job完全解耦，只专注Job的业务逻辑编写。
## 服务端依赖环境
    Docker （运行在docker或k8s下）
    Net 5.0 （提供docker.hub镜像服务)
    Redis （用与客户端的实例注册）
    Sqlserver/MySql/Oracle（常用数据库任意选一个）
    Grpc （通讯协议，后期考虑REST API)
    Quartz.Net （使用该组件实现时间事件及调度）
## 客户端依赖环境
    netstandard2.0
    AppSettins.json （配置服务端通讯地址）
    Farseer.Net.Job 方便快速集成到业务系统（如不依赖，需自行对接）
    Grpc（Farseer.Net.Job会实现服务的创建）

## 痛点解决、调度框架的选型
FSS的设计初衷是为了实现分布式的调度，并且运行Job的程序不应该依赖调度策略，而只专注于开发自己的业务逻辑。

我希望实现Job的程序应该是高可用的，比如我利用k8s或Docker，跑3个实例。同一时间只能由其中1个实例来负载执行任务。

而目前市面上的调度，大部份是基于组件的（单体应用，在当前进程中实现的调度）。无法做到多实例下，远程调度。

就调度本身，市面上的调度框架已经非常优秀了，如果我又去写一套出来是没有意义的造轮子。

同时Quartz.Net在.NET体系中是一款非常流行且成熟的组件。因此我选用这个调度组件来实现我的调度功能。而FSS只要专注于分布式的实现即可。

## 设计目标
高可用（HA）：业务方提供多实例的job运行。同一个任务、同一个job实例只会被调度一次

快速搭建：运行于docker或k8s下，提供一键部署，1分钟即可把服务部署到您的生产环境中

轻量级：极低的内存、CPU消耗，快速运行，依赖少。

动态的执行触发器：可定时、间隔时间、或由业务方job动态返回下次执行时间。

可观性：job的执行日志、执行时间、耗时能全面记录在服务端，供运维了解任务的执行情况

快速上手：借助Farseer.Net.Job组件（开源在github，并提供nuget包），可以快速实现一个job

## 服务端与客户端通讯
如前面叙述，本项目希望是建立一套极简、依赖少的调度平台。

因此本项目采用的通讯方案：

    1、当调度器开始工作时，会通过客户端列表（客户端启动后注册进来），以轮询的方式取出其中一个客户端，然后使用Grpc进行调度通知。
    2、客户端在收到要执行的任务时，会实时通知服务端当前任务的状态、进度。

## 客户端使用
通过Farseer.Net.Job组件，运行job的程序在启动之后会做：

    1、向FSS调度平台注册（Grpc双流方式）客户端的信息。
    2、会启动Grpc服务（端口可自定义），用于接收FSS调度平台的消息通知

`appsettings.json配置`
```config
  "FSS": {
    "Server": "http://localhost", // FSS平台地址
    "GrpcServicePort": 5672,      // 本地打开的端口（用于FSS平台有任务要执行时通知到我）
    "ConnectFssServerTime": 1000  // 对FSS平台的心跳时间（毫秒）
  }
```
`Program.cs`
```c#
[Fss] // 开启后，才能注册到FSS平台
public class Program
{
    public static void Main()
    {
        // 初始化模块
        FarseerApplication.Run<StartupModule>().Initialize();
        Thread.Sleep(-1);
    }
}
```
`StartupModule.cs`
```c#
/// <summary>
/// 启动模块
/// </summary>
[DependsOn(typeof(JobModule))] // 依赖Job模块
public class StartupModule : FarseerModule
{
    public override void PreInitialize()
    {
    }
}
```
`HelloWorldJob.cs`
```c#
[FssJob(Name = "bbb")] // Name与FSS平台配置的JobTypeName保持一致
public class HelloWorldJob : IFssJob
{
    /// <summary>
    /// 执行任务
    /// </summary>
    public bool Execute(ReceiveContext context)
    {
        // 告诉FSS平台，当前进度执行了 20%
        context.SetProgress(20);
        
        // 让FSS平台，记录日志
        context.Logger(LogLevel.Information, "你好，世界！");
        
        // 下一次执行时间为10秒后（如果不设置，则使用任务组设置的时间）
        context.SetNextAt(DateTime.Now.AddSeconds(10));
        
        // 任务执行成功
        return true;
    }
}
```
## 任务组
任务组：是要执行任务的基本信息：任务名称、开始时间、执行次数、执行耗时、启用状态、下一次执行的任务ID。

任务：是在任务组设定并启用后动态创建并由系统自动维护的任务信息，可以知道某次的任务是由哪个客户端执行，执行状态（成功、失败），执行耗时等信息。

    当任务组启用后，会创建一条任务信息，并标记为该任务的执行开始时间，是否执行的状态。
    调度器通过这个任务建立时间轮，来轮询时间格。

## 服务端（FSS)开发进度
|  序号   | 功能  | 状态  |
|  ----  | ----  | ---- |
| 1  | 接收客户端的注册、激活 | 完成 |
| 2  | 实现服务发现SLB | 完成 |
| 3  | 定时移除10S未激活的客户端 | 完成 |
| 4  | 远程通知客户端执行JOB | 完成 |
| 5  | 接收客户端当前执行任务的进度、状态更新 | 完成 |
| 6  | 记录客户端的运行日志、异常日志 | 完成 |
| 7  | 实现任务组功能 |  |
| 8  | 动态创建任务 |  |
| 9  | 根据任务，基于Quartz.Net创建触发器、调度，并通知客户端 |  |
| 10  | 去中心化、分布式实现 |  |

## .NET客户端(Farseer.Net.Job)开发进度
|  序号   | 功能  | 状态  |
|  ----  | ----  | ---- |
| 1  | 向服务端注册、激活 | 完成 |
| 2  | 开启GRPC服务 | 完成 |
| 3  | 实现服务端的任务执行通知服务 | 完成 |
| 4  | 实现IJob Abs接口 | 完成 |
| 5  | 向服务端实时推送JOB的进度、状态、日志 | 完成 |

## 下一个版本要实现的功能
|  序号   | 功能  |
|  ----  | ----  |
| 1  | 踢除客户端 |
| 2  | 日志改用Redis作消费写入 |