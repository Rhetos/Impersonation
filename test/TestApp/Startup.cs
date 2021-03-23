using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Rhetos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestApp", Version = "v1" });
                // Adding Rhetos REST API to Swagger with document name "rhetos".
                c.SwaggerDoc("rhetos", new OpenApiInfo { Title = "Rhetos REST API", Version = "v1" });

            });

            // Adding Rhetos to AspNetCore application.
            services.AddRhetos(rhetosHostBuilder => ConfigureRhetosHostBuilder(rhetosHostBuilder, Configuration))
                .UseAspNetCoreIdentityUser()
                .AddImpersonation()
                .AddRestApi(o =>
                {
                    o.BaseRoute = "rest";
                    o.GroupNameMapper = (conceptInfo, name) => "rhetos"; // OpenAPI document name.
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApp v1");
                    // Add Swagger endpoint for Rhetos REST API.
                    c.SwaggerEndpoint("/swagger/rhetos/swagger.json", "Rhetos REST API");
                });
            }

            app.UseRhetosRestApi();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        // This is extracted to separate public static method so it can be used BOTH from Startup class
        // and any other code that wishes to recreate RhetosHost specific for this web application
        // Common use is to call this from Program.CreateRhetosHostBuilder method which is by convention consumed by
        // Rhetos tools.
        public static void ConfigureRhetosHostBuilder(IRhetosHostBuilder rhetosHostBuilder, IConfiguration configuration)
        {
            rhetosHostBuilder
                .ConfigureRhetosHostDefaults()
                .UseBuilderLogProvider(new Rhetos.Host.Net.Logging.RhetosBuilderDefaultLogProvider()) // Delegate RhetosHost logging to standard NetCore targets.
                .ConfigureConfiguration(cfg => cfg
                    .AddJsonFile("ConnectionString.local.json")
                    .MapNetCoreConfiguration(configuration));
        }
    }
}
