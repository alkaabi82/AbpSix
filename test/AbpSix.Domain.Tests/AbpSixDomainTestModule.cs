using AbpSix.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace AbpSix;

[DependsOn(
    typeof(AbpSixEntityFrameworkCoreTestModule)
    )]
public class AbpSixDomainTestModule : AbpModule
{

}
