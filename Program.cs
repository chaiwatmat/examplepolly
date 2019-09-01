using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;

namespace console {
    class Program {
        static async Task Main (string[] args) {
            //create the http client
            var httpClient = new HttpClient ();

            //call the httpbin service with Polly
            var response = await Policy
                .HandleResult<HttpResponseMessage> (message => !message.IsSuccessStatusCode)
                .WaitAndRetryAsync (new [] {
                    TimeSpan.FromSeconds (5),
                        TimeSpan.FromSeconds (5),
                        TimeSpan.FromSeconds (5),
                        TimeSpan.FromSeconds (5),
                        TimeSpan.FromSeconds (5)
                }, (result, timeSpan, retryCount, context) => {
                    Console.WriteLine ($"Request failed with {result.Result.StatusCode}. Retry count = {retryCount}. Waiting {timeSpan} before next retry. ");
                })
                .ExecuteAsync (() => httpClient.PostAsync ("https://httpbin.org/status/200,401,500", null));

            if (response.IsSuccessStatusCode)
                Console.WriteLine ("Response was successful with 200");
            else
                Console.WriteLine ($"Response failed. Status code {response.StatusCode}");
        }
    }
}