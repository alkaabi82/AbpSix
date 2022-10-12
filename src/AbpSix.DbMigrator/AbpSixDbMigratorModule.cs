using AbpSix.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AbpSix.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpSixEntityFrameworkCoreModule),
    typeof(AbpSixApplicationContractsModule)
)]
public class AbpSixDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = false;
        });
    }
}
