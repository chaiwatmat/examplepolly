using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace console {
    class Program {
        static async Task Main(string[] args) {
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());

            var tasks = new List<Task>();
            var task1 = RetryEvery5Seconds();
            var task2 = RetryUntilSuccess();
            var task3 = RunProgramRunExample();

            tasks.Add(task1);
            tasks.Add(task2);
            tasks.Add(task3);

            Task.WhenAll(tasks);

            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();
        }

        static async Task RetryEvery5Seconds() {
            //create the http client
            var httpClient = new HttpClient();

            //call the httpbin service with Polly
            var response = await Policy
                .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                .WaitAndRetryAsync(new [] {
                    TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(5)
                }, (result, timeSpan, retryCount, context) => {
                    Console.WriteLine($"Request failed with {result.Result.StatusCode}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ");
                })
                .ExecuteAsync(() => httpClient.PostAsync("https://httpbin.org/status/200,401,500", null));

            if (response.IsSuccessStatusCode)
                Console.WriteLine("Response was successful with 200");
            else
                Console.WriteLine($"Response failed. Status code {response.StatusCode}");
        }

        static async Task RetryUntilSuccess() {
            var httpClient = new HttpClient();

            var response = await Policy
                .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
                .WaitAndRetryForeverAsync(
                    retryCount => TimeSpan.FromSeconds(5),
                    (result, timeSpan, context) => {
                        Console.WriteLine($"Retry count = {timeSpan}. Request failed with {result.Result.StatusCode}.");
                    })
                .ExecuteAsync(() => httpClient.PostAsync("https://httpbin.org/status/200,400,401,404,500", null));

            if (response.IsSuccessStatusCode)
                Console.WriteLine("Response was successful with 200");
            else
                Console.WriteLine($"Response failed. Status code {response.StatusCode}");
        }

        private static async Task RunProgramRunExample() {
            // try {
            // Grab the Scheduler instance from the Factory
            NameValueCollection props = new NameValueCollection { { "quartz.serializer.type", "binary" } };
            StdSchedulerFactory factory = new StdSchedulerFactory(props);
            IScheduler scheduler = await factory.GetScheduler();

            // and start it off
            await scheduler.Start();

            // Trigger the job to run now, and then repeat every 10 seconds
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithCronSchedule("*/5 * * * * ?")
                .ForJob("job1", "group1")
                .Build();

            // define the job and tie it to our HelloJob class
            IJobDetail job = JobBuilder.Create<HelloJob>()
                .WithIdentity("job1", "group1")
                .Build();

            // Tell quartz to schedule the job using our trigger
            await scheduler.ScheduleJob(job, trigger);

            Console.WriteLine("Schedule job done");
            Console.WriteLine("Task delay done");
        }

        // simple log provider to get something to the console
        private class ConsoleLogProvider : ILogProvider {
            public Logger GetLogger(string name) {
                return (level, func, exception, parameters) => {
                    if (level >= LogLevel.Info && func != null) {
                        Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
                    }
                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message) {
                throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, string value) {
                throw new NotImplementedException();
            }
        }
    }

    public class HelloJob : IJob {
        public async Task Execute(IJobExecutionContext context) {
            await Console.Out.WriteLineAsync($"{DateTime.Now.ToLongTimeString()} Greetings from HelloJob!");
        }
    }

}