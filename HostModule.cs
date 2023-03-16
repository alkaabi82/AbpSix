using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyOrg.MyApp.EntityFrameworkCore;
using MyOrg.MyApp.MultiTenancy;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Auditing;
using DevExpress.AspNetCore;
using DevExpress.XtraReports.Web.Extensions;
using DevExpress.AspNetCore.Reporting.WebDocumentViewer;
using DevExpress.AspNetCore.Reporting;
using Volo.Abp.Timing;
using MyOrg.MyApp.Reports;
using DevExpress.DataAccess;
using DevExpress.XtraReports.Web.ReportDesigner.Services;
using Volo.Abp.AspNetCore.Serilog;
using DevExpress.XtraReports.Web.WebDocumentViewer;
using Hangfire;
using MyOrg.MyApp.ReportingServices;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.EventBus.RabbitMq;
using MyOrg.MyApp.ReportingServices.Payroll;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.RabbitMQ;
using Volo.Abp.Caching.StackExchangeRedis;
using DevExpress.Security.Resources;
using MyOrg.MyApp.EntityFrameworkCore.Oracle;
using MyOrg.MyApp.Extensions;
using Volo.Abp.BackgroundWorkers.Hangfire;
using MyOrg.Ordnance;
using Volo.Abp.Account.Public.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonX.Bundling;
using Volo.Abp.Identity;
using Volo.Abp.LeptonX.Shared;
using Volo.Abp.PermissionManagement;
using Volo.Saas.Host;

namespace MyOrg.MyApp
{
    [DependsOn(typeof(MyAppEntityFrameworkCoreOracleModule))] //Loading Hrms Oracle Db Context
    [DependsOn(
        typeof(MyAppHttpApiModule),
        typeof(AbpAutofacModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
        typeof(AbpIdentityAspNetCoreModule),
        typeof(MyAppApplicationModule),
        typeof(MyAppEntityFrameworkCoreModule),
        typeof(MyAppEntityFrameworkCoreDbMigrationsModule),
        typeof(AbpSwashbuckleModule),
        typeof(AbpBackgroundJobsModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(AbpBackgroundJobsHangfireModule),
        typeof(AbpBackgroundWorkersHangfireModule)//,
      // typeof(OrdnanceHttpApiHostModule)
        // typeof(AbpEventBusRabbitMqModule)
        )]
    public class MyAppHttpApiHostModule : AbpModule
    {

        private const string DefaultCorsPolicyName = "Default";

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostingEnvironment = context.Services.GetHostingEnvironment();

            if (!Convert.ToBoolean(configuration["App:DisablePPI"]))
            {
                Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            }


            //if (!Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]))
            //{
            //    Configure<IdentityServerAs>();
            //}

            ConfigureUrls(configuration);
            ConfigureBundles();
            ConfigureConventionalControllers();
            ConfigureAuthentication(context, configuration);
            ConfigureSwagger(context, configuration);
            ConfigureCache(configuration);
            ConfigureVirtualFileSystem(context);
            ConfigureDataProtection(context, configuration, hostingEnvironment);
            ConfigureCors(context, configuration);
            ConfigureAuditing();
            ConfigureHangfire(context, configuration);
            ConfigureTheme();

            //ConfigureReporting(context, configuration, hostingEnvironment);

            Configure<AbpClockOptions>(options =>
            {
                options.Kind = DateTimeKind.Local;
            });


            Configure<PermissionManagementOptions>(options =>
            {
                options.IsDynamicPermissionStoreEnabled = false;
            });


            //ConfigureHealthChecks(context);
            // ConfigureMessagingQueue();
        }

        private void ConfigureTheme()
        {
            Configure<LeptonXThemeOptions>(options =>
            {
                options.DefaultStyle = LeptonXStyleNames.Dark;
            });
        }
        private void ConfigureMessagingQueue()
        {
            Configure<AbpRabbitMqOptions>(options =>
            {
                options.Connections.Default.UserName = "guest";
                options.Connections.Default.Password = "guest";
                options.Connections.Default.HostName = "localhost";
                options.Connections.Default.Port = 5672;
            });

            Configure<AbpRabbitMqEventBusOptions>(options =>
            {
                options.ClientName = "MyApp-queue";
                options.ExchangeName = "MyApp-exchange";
            });
        }

        private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(configuration.GetConnectionString("Default"));

            });

            GlobalJobFilters.Filters.Add(new DisableConcurrentExecutionAttribute(15));

        }

        private void ConfigureReporting(ServiceConfigurationContext context,
            IConfiguration configuration,
            IWebHostEnvironment hostingEnvironment)
        {
            AccessSettings.StaticResources.SetRules(DirectoryAccessRule.Allow(), UrlAccessRule.Allow()); // to access   image url  from report
            var customReportConfigProvider = new CustomReportConfigProvider(hostingEnvironment);

            context.Services.AddDevExpressControls();
            DefaultConnectionStringProvider.AssignConnectionStrings(customReportConfigProvider.GetGlobalConnectionStrings);
            context.Services.AddTransient<ReportStorageWebExtension, MyAppReportStorageWebExtension>();
            context.Services.AddSession();

            // To Remove default Devexpress Controllers
            context.Services.AddMvc().ConfigureApplicationPartManager(x =>
            {
                var parts = x.ApplicationParts;
                var aspNetCoreReportingAssemblyName = typeof(WebDocumentViewerController).Assembly.GetName().Name;
                var reportingPart = parts.FirstOrDefault(part => part.Name == aspNetCoreReportingAssemblyName);
                if (reportingPart != null)
                {
                    parts.Remove(reportingPart);
                }
            });

            context.Services.ConfigureReportingServices(configurator =>
            {
                configurator.ConfigureWebDocumentViewer(viewerConfigurator =>
                {
                    viewerConfigurator.UseCachedReportSourceBuilder();
                });
            });

            //Report Common Services such as Data sources
            ReportServiceRegistrator.AddCommonServices(context.Services);

            //This will register a singlton to be used for Object Datasource after HTTP life time
            context.Services.AddSingleton(typeof(IScopedServiceProvider<ReportingDataSourceService>), typeof(ScopedServiceProvider<ReportingDataSourceService>));
            context.Services.AddSingleton(typeof(IScopedServiceProvider<PayrollReportingService>), typeof(ScopedServiceProvider<PayrollReportingService>));

            /*
             * Abp uses Registration by convention, and devExpress controller are not registered by convention
             * Any controller that inherit from AbpController is registered automatically
             * We need to register these controllers manually
             */

            context.Services.AddTransient<MyAppWebDocumentViewerController>();
            context.Services.AddTransient<MyAppReportDesignerController>();
            context.Services.AddTransient<MyAppQueryBuilderController>();

            // Register SeviceProvider as signlton to be used outside of HTTP request Context Lifetime

            // This requires a scoped lifetime because we need after http request lifetime
            context.Services.AddScoped<IObjectDataSourceInjector, ObjectDataSourceInjector>();
            context.Services.AddScoped<IWebDocumentViewerReportResolver, WebDocumentViewerReportResolver>();
            context.Services.AddScoped<PreviewReportCustomizationService, CustomPreviewReportCustomizationService>();
        }

        //private void ConfigureHealthChecks(ServiceConfigurationContext context)
        //{
        //    context.Services.MyAppAppHealthChecks();
        //}

        private void ConfigureAuditing()
        {
            Configure<AbpAuditingOptions>(options =>
            {
                options.ApplicationName = "MyAppBe";
            });
        }

        private void ConfigureUrls(IConfiguration configuration)
        {
            Configure<AppUrlOptions>(options =>
            {
                options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
                options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
                options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
                options.Applications["Angular"].Urls[AccountUrlNames.EmailConfirmation] = "account/email-confirmation";
                options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"].Split(','));
            });
        }

        private void ConfigureBundles()
        {
            Configure<AbpBundlingOptions>(options =>
            {
                options.StyleBundles.Configure(
                    LeptonXThemeBundles.Styles.Global,
                    bundle =>
                    {
                        bundle.AddFiles("/global-styles.css");
                    }
                );
            });
        }

        private void ConfigureCache(IConfiguration configuration)
        {
            Configure<AbpDistributedCacheOptions>(options =>
            {
                options.KeyPrefix = "MyApp:";
            });
        }

        private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
        {
            var hostingEnvironment = context.Services.GetHostingEnvironment();

            if (hostingEnvironment.IsDevelopment())
            {
                Configure<AbpVirtualFileSystemOptions>(options =>
                {
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAppDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}MyOrg.MyApp.Domain.Shared", Path.DirectorySeparatorChar)));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAppDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}MyOrg.MyApp.Domain", Path.DirectorySeparatorChar)));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAppApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}MyOrg.MyApp.Application.Contracts", Path.DirectorySeparatorChar)));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAppApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}MyOrg.MyApp.Application", Path.DirectorySeparatorChar)));
                    options.FileSets.ReplaceEmbeddedByPhysical<MyAppHttpApiModule>(Path.Combine(hostingEnvironment.ContentRootPath, string.Format("..{0}..{0}src{0}MyOrg.MyApp.HttpApi", Path.DirectorySeparatorChar)));
                });
            }
        }

        private void ConfigureConventionalControllers()
        {
            Configure<AbpAspNetCoreMvcOptions>(options =>
            {
                options.ConventionalControllers.Create(typeof(MyAppApplicationModule).Assembly);
                options.ConventionalControllers.Create(typeof(OrdnanceApplicationModule).Assembly);
            });
        }

        private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = configuration["AuthServer:Authority"];
                    options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                    options.Audience = "MyApp";
                });

        }

        private static void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddAbpSwaggerGenWithOAuth(
                configuration["AuthServer:Authority"],
                new Dictionary<string, string>
                {
                    {"MyApp", "MyApp API"}
                },
                options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MyApp API", Version = "v1" });
                    options.DocInclusionPredicate((docName, description) => true);
                    options.CustomSchemaIds(type => type.FullName);
                    options.UseAllOfToExtendReferenceSchemas();
                });
        }

        private void ConfigureDataProtection(
            ServiceConfigurationContext context,
            IConfiguration configuration,
            IWebHostEnvironment hostingEnvironment)
        {

            var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("MyApp");
            if (!hostingEnvironment.IsDevelopment())
            {
                var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
                dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "MyApp-Protection-Keys");
            }
        }

        private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithOrigins(
                            configuration["App:CorsOrigins"]
                                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                                .Select(o => o.Trim().RemovePostFix("/"))
                                .ToArray()
                        )
                        .WithAbpExposedHeaders()
                        .WithExposedHeaders("Content-Disposition")
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }


        private void ConfigureImpersonation(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.Configure<AbpAccountOptions>(options =>
            {
                options.TenantAdminUserName = "admin";
                options.ImpersonationTenantPermission = SaasHostPermissions.Tenants.Impersonation;
                options.ImpersonationUserPermission = IdentityPermissions.Users.Impersonation;
            });
        }
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            app.UseCustomHttpHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAbpRequestLocalization();

            if (!env.IsDevelopment())
            {
                app.UseErrorPage();
            }

            app.UseAbpSecurityHeaders();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(DefaultCorsPolicyName);
            app.UseAuthentication();

            if (MultiTenancyConsts.IsEnabled)
            {
                app.UseMultiTenancy();
            }
            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});

            app.UseUnitOfWork();

            //app.UseSession();
            app.UseAuthorization();
            app.UseSwagger();
            app.UseAbpSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyApp API");
                var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
                options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
                options.OAuthClientSecret(configuration["AuthServer:SwaggerClientSecret"]);
            });

            app.UseHangfireDashboard("/hangfire", new DashboardOptions()
            {
                IsReadOnlyFunc = (dashboardContext) => false,// !env.IsDevelopment(),
                AsyncAuthorization = new[] { new HangfireDashboardAuthorizationFilter() },
                IgnoreAntiforgeryToken = true

            });
            app.UseAuditing();
            app.UseAbpSerilogEnrichers();
            app.UseConfiguredEndpoints(options =>
            {

            });
        }
    }
}
