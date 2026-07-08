using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WellMonitor.Device.Controllers;
using WellMonitor.Device.Hubs;
using WellMonitor.Device.Services;

namespace WellMonitor.Device
{
    public class WebStartup
    {
        public WebStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // This method is called from Program.cs where services are already registered
            // We don't need to register services here since they're handled in Program.cs
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // Serve static files from wwwroot
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                // Map API controllers
                endpoints.MapControllers();
                
                // Map SignalR hub
                endpoints.MapHub<DeviceStatusHub>("/devicestatushub");
            });
        }
    }
}
