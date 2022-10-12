using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AbpSix.Data;

/* This is used if database provider does't define
 * IAbpSixDbSchemaMigrator implementation.
 */
public class NullAbpSixDbSchemaMigrator : IAbpSixDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
