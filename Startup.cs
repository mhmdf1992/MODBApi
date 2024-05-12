using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MODB.Api.Attributes;
using MODB.Api.Middleware;
using MODB.FlatFileDB;

namespace MODB.Api
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
            services.AddControllers(opt => {
                opt.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
                opt.ModelBinderProviders.Insert(0, new CommaSeparatedArrayModelBinderProvider());
                opt.InputFormatters.Insert(0, new RawRequestBodyFormatter());
            });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MODB", Version = "v1" });
                var securityScheme = new OpenApiSecurityScheme {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    },
                    Description = "Api key",
                    In = ParameterLocation.Header,
                    Name = "ApiKey",
                    Type = SecuritySchemeType.ApiKey
                };
                c.AddSecurityDefinition("ApiKey", securityScheme);

                // Security Requirement
                c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                    { securityScheme, Array.Empty<string>() }
                });
            });

            services.AddSingleton<Settings>(opt => opt.GetRequiredService<IConfiguration>().Get<Settings>());
            services.AddSingleton<IKeyValDB>(opt => 
                new FlatFileKeyValDB(path: Path.Combine(opt.GetRequiredService<Settings>().Path.Concat<string>(new string[]{"clients_db"}).ToArray())));
            services.AddSingleton<ConcurrentDictionary<string, ConcurrentDictionary<string, FlatFileKeyValDB>>>(opt => {
                var clientsDB = opt.GetRequiredService<IKeyValDB>();
                var keys = clientsDB.GetKeys(page: 1, pageSize: int.MaxValue).Items;
                var allDBs = new ConcurrentDictionary<string, ConcurrentDictionary<string, FlatFileKeyValDB>>();
                foreach(var key in keys){
                    var clientPath = Path.Combine(opt.GetRequiredService<Settings>().Path.Concat<string>(new string[]{key}).ToArray());
                    if(!Directory.Exists(clientPath)){
                        allDBs.TryAdd(key, new ConcurrentDictionary<string, FlatFileKeyValDB>());
                        continue;
                    }
                    var dbs = Directory.GetDirectories(Path.Combine(opt.GetRequiredService<Settings>().Path.Concat<string>(new string[]{key}).ToArray()))
                        .Select(path => new FlatFileKeyValDB(path: path)).ToDictionary(x => x.Name, x => x);
                    allDBs.TryAdd(key, new ConcurrentDictionary<string, FlatFileKeyValDB>(dbs));
                }

                return allDBs;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // app.Use(async (context, next) => {
            //     context.Request.EnableBuffering();
            //     await next();
            // });
            
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MODB v1"));
            //app.UseHttpsRedirection();
            app.UseMiddleware<ConsistentApiResponseErrors.Middlewares.ExceptionHandlerMiddleware>();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
