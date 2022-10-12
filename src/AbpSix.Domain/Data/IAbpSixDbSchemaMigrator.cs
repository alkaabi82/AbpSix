using System.Threading.Tasks;

namespace AbpSix.Data;

public interface IAbpSixDbSchemaMigrator
{
    Task MigrateAsync();
}
