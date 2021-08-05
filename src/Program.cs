using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using PuppyPdfGenerator.Tools;

namespace PuppyPdfGenerator
{
    public class Program
    {
        /// <summary>
        /// Init all the things..
        /// </summary>
        public static void Main(string[] args)
        {
            // Load config from disk.
            Config.Load();

            // Initiate the host.
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .Build()
                .Run();
        }
    }
}