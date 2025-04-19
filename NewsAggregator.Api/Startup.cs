using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NewsAggregator.Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Add gRPC services
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // Map gRPC services
                endpoints.MapGrpcService<Services.GrpcCategoryService>();
            });
        }
    }
} 