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
using MO.MODBApi.Attributes;
using MO.MODB;
using MO.MODBApi.DataModels.Sys;
using MO.MODBApi.SchemeFilter;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using FluentValidation.AspNetCore;
using FluentValidation;
using MO.MODBApi.Validators;
using ConsistentApiResponseErrors.Filters;

namespace MO.MODBApi
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
                opt.Filters.Add<ValidateModelStateAttribute>();
                opt.ModelBinderProviders.Insert(0, new CommaSeparatedArrayModelBinderProvider());
            }).AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<SetUserObjectValidator>())
                .AddJsonOptions(options => 
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MODBApi", Version = "v1" });
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
                c.SchemaFilter<EnumSchemaFilter>();
                // Security Requirement
                c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                    { securityScheme, Array.Empty<string>() }
                });
            });

            services.AddSingleton(opt => opt.GetRequiredService<IConfiguration>().Get<SystemSettings>());
            services.AddSingleton<IDBCollection>(opt => 
                new DBCollection(path: Path.Combine(opt.GetRequiredService<SystemSettings>().Path.Concat<string>(new string[]{"sys"}).ToArray())));
            services.AddSingleton(opt => 
                opt.GetRequiredService<IDBCollection>().Get("collections")
                    .All(page: 1, pageSize: int.MaxValue)
                    .Items
                    .ToDictionary(x => x, x => new DBCollection(path: Path.Combine(opt.GetRequiredService<SystemSettings>().Path.Concat<string>(new string[]{x}).ToArray()))));
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
