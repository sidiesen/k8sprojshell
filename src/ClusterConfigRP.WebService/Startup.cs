// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using ClusterConfigRP.Service;
    using ClusterConfigRP.Shared.Configuration;
    using ClusterConfigRP.Shared.Logging.Loggers;
    using ClusterConfigRP.Shared.Logging.Structures;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            // while (!Debugger.IsAttached)
            // {
            //      Thread.Sleep(100);
            // }
            Configuration = configuration;
            logging = EnvironmentConfiguration.SetupLogging(httpContextAccessor);
            clusterConfigService = new ClusterConfigService(logging);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddApiVersioning(options => {
                options.UseApiBehavior = true;
                options.ReportApiVersions = true;
            });

            services.AddSingleton<ILogging>(logging);
            services.AddSingleton<ClusterConfigService>(clusterConfigService);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            this.logging.TrackTrace($"env.EnvironmentName is: {env.EnvironmentName}", LogLevel.Information);

            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        #region PrivateMembers
        private static readonly IHttpContextAccessor httpContextAccessor = new HttpContextAccessor();
        private readonly ClusterConfigService clusterConfigService;
        private readonly LoggerCollection logging;
        #endregion
    }
}
