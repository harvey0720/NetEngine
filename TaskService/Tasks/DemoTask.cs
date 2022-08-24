﻿using Common;
using DistributedLock;
using Repository.Database;
using TaskService.Libraries;

namespace TaskService.Tasks
{
    public class DemoTask : BackgroundService
    {

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IDHelper idHelper;


        public DemoTask(IServiceProvider serviceProvider, ILogger<DemoTask> logger, IDHelper idHelper)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.idHelper = idHelper;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CronSchedule.BatchBuilder(stoppingToken, this);

            await Task.Delay(-1, stoppingToken);
        }



        [CronSchedule(Cron = "0/1 * * * * ?")]
        public void Run()
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                var distLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();

                logger.LogInformation("HelloWord{Id}", idHelper.GetId());

                Console.WriteLine(DateTime.Now);

                GC.Collect();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DemoTask.Run");
            }

        }


    }
}
