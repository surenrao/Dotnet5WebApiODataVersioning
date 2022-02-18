namespace Dotnet5WebApiODataVersioning
{
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

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
            //services.AddControllers();
            AddOdataVersioning(services, camelCase: false);
            ConfigureSwagger(services);
        }

        /// <summary>
        /// OData v7.5.8 (v8 has breaking changes)
        /// ApiVersioning, OData, AddControllers, AddNewtonsoftJson
        /// </summary>
        /// <param name="services"></param>
        /// <param name="camelCase"></param>
        public void AddOdataVersioning(IServiceCollection services, bool camelCase = true)
        {
            // https://devblogs.microsoft.com/odata/up-running-w-odata-in-asp-net-6/
            // https://devblogs.microsoft.com/odata/api-versioning-extension-with-asp-net-core-odata-8/
            // services.AddControllers().AddOData(options => options.Select().Filter().OrderBy().Count().SetMaxTop(10)).AddNewtonsoftJson();            

            //services.AddControllers(); // or services.AddMvcCore()
            //services.AddApiVersioning();
            //services.AddOData().EnableApiVersioning(); // if using OData
            //services.AddODataApiExplorer(); // if using OData
            //services.AddVersionedApiExplorer(); // if NOT using OData
            services.AddControllers().AddNewtonsoftJson(setupAction => {
                // By default Model properties are converted to camelCase, below will change it back to Pascalcase
                if (!camelCase)
                {
                    setupAction.SerializerSettings.ContractResolver = new DefaultContractResolver();
                }
            });

            //Add API versioning to application. https://github.com/microsoft/aspnet-api-versioning/wiki/New-Services-Quick-Start#aspnet-core-with-odata-v40
            services.AddApiVersioning(
                options =>
                {
                    //Reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    // https://github.com/microsoft/aspnet-api-versioning/tree/master/samples/aspnetcore/ByNamespaceSample
                    // automatically applies an api version based on the name of the defining controller's namespace
                    // 'v' | 'V' : [<year> '-' <month> '-' <day>] : [<major[.minor]>] : [<status>]
                    // ex: v2018_04_01_1_1_Beta
                    options.Conventions.Add(new VersionByNamespaceConvention());
                });
            services.AddVersionedApiExplorer(options =>
            {
                // Add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // NOTE: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";

                // NOTE: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
            });
            // [EnableQuery] is needed for Actions
            services.AddOData().EnableApiVersioning(options => {
                // https://github.com/microsoft/aspnet-api-versioning/tree/master/samples/aspnetcore/ByNamespaceSample
                // automatically applies an api version based on the name of the defining controller's namespace
                // 'v' | 'V' : [<year> '-' <month> '-' <day>] : [<major[.minor]>] : [<status>]
                // ex: v2018_04_01_1_1_Beta
                options.Conventions.Add(new VersionByNamespaceConvention());
            });  // https://github.com/OData/AspNetCoreOData
            services.AddODataApiExplorer(options =>
            {
                // Add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // NOTE: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";

                // NOTE: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddMvcCore(options =>
            {
                foreach (var outputFormatter in options.OutputFormatters.OfType<OutputFormatter>().Where(x => x.SupportedMediaTypes.Count == 0))
                {
                    outputFormatter.SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/odata"));
                }

                foreach (var inputFormatter in options.InputFormatters.OfType<InputFormatter>().Where(x => x.SupportedMediaTypes.Count == 0))
                {
                    inputFormatter.SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/odata"));
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(setupAction =>
            {   
                setupAction.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Test Core Api",
                    Version = "v1",
                    Description = "Test Core Api",
                    Contact = new OpenApiContact
                    {
                        Name = "Test",
                        Email = "test@example.com",
                        Url = new Uri("http://localhost")
                    }
                });
                setupAction.SwaggerDoc("v2", new OpenApiInfo
                {
                    Title = "My API - V2",
                    Version = "v2"
                });
                setupAction.EnableAnnotations();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                setupAction.IncludeXmlComments(xmlPath);
            });
            services.AddSwaggerGenNewtonsoftSupport(); // explicit opt-in - needs to be placed after AddSwaggerGen()
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger(setupAction =>
                {
                    setupAction.SerializeAsV2 = true;
                    setupAction.PreSerializeFilters.Add((swagger, httpReq) =>
                    {
                        swagger.Servers = new List<OpenApiServer>
                        {
                            new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                        };
                    });
                });
                app.UseSwaggerUI(setupAction =>
                {
                    setupAction.SwaggerEndpoint("/swagger/v1/swagger.json", "Test Core Api v1");
                    setupAction.SwaggerEndpoint("/swagger/v2/swagger.json", "Test Core Api v2");
                });
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // For Odata
                endpoints.EnableDependencyInjection();
                endpoints.Select().Filter().OrderBy().Count().MaxTop(10);
            });
        }
    }
}
