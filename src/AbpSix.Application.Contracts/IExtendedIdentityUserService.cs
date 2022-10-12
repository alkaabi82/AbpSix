using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpSix;

public interface IExtendedIdentityUserService : IApplicationService
{
    Task GetUsers();
}