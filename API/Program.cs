using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Microsoft.AspNetCore.Identity;
using Domain;

namespace API
{
    public class Program
    {
        // The API needs to be hosted and .net comes with a webserver called Kestrel 
        // that is going to host our API application and it is responsible for running it
        // and serving it on port 5000 on localhost
        public static async Task Main(string[] args)
        {
            // CreateHostBuilder(args).Build().Run();
            // Below we want to check at runtime if we already have the database created
            // If not, then create it

            // Store in host the host builder withour running it
            var host = CreateHostBuilder(args).Build();

            // set using in front of the variable and what it does is once we finished with
            // this particular method then this scope variable is gonna be disposed of by the framework.
            // Scope will host any services that we'll create inside this particular method but as soon as
            // we finish starting the application we want this to be disposed because in it will store any of our services
            using var scope = host.Services.CreateScope();

            var services = scope.ServiceProvider;

            try
            {
                // our context is gonna be a typeof DataContext 
                // We get DataContext as a service because we added it in the StartUp class in ConfigureServices
                // and we use the service located pathern so we can populate context 
                var context = services.GetRequiredService<DataContext>();
                var userManager = services.GetRequiredService<UserManager<AppUser>>();

                await context.Database.MigrateAsync();
                await Seed.SeedData(context, userManager);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occured during migration");
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
