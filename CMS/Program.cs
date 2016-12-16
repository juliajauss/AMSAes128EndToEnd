namespace CMS
{
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;

    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
            //.UseUrls("http://localhost:22539/")
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build();

            host.Run();
        }
    }
}
