using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace AbpSix;

public class ExtendedIdentityUserService : AbpSixAppService, IExtendedIdentityUserService
{
    private readonly IIdentityUserRepository _userRepository;

    public ExtendedIdentityUserService(IIdentityUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task GetUsers()
    {
        var users = await _userRepository.GetListAsync();

        foreach (var user in users)
        {
            var social = user.GetProperty("SocialSecurityNumber");
        }
    }
}