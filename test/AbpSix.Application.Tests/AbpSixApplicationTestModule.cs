using Volo.Abp.Modularity;

namespace AbpSix;

[DependsOn(
    typeof(AbpSixApplicationModule),
    typeof(AbpSixDomainTestModule)
    )]
public class AbpSixApplicationTestModule : AbpModule
{

}
