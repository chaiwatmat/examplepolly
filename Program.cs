using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace console {
    class Program {
        static async Task Main (string[] args) {
            //create the http client
            var httpClient = new HttpClient ();

            //call the httpbin service
            var response = await httpClient.PostAsync ("https://httpbin.org/status/200,408", null);

            if (response.IsSuccessStatusCode)
                Console.WriteLine ("Response was successful with 200");
            else
                Console.WriteLine ($"Response failed. Status code {response.StatusCode}");
        }
    }
}