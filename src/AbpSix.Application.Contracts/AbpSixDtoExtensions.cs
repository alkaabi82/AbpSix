using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace AbpSix;

public static class AbpSixDtoExtensions
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        OneTimeRunner.Run(() =>
        {
                /* You can add extension properties to DTOs
                 * defined in the depended modules.
                 *
                 * Example:
                 */
                  ObjectExtensionManager.Instance
                    .AddOrUpdateProperty<IdentityUserDto, string>("SocialSecurityNumber");
                 /*
                 * See the documentation for more:
                 * https://docs.abp.io/en/abp/latest/Object-Extensions
                 */
        });
    }
}
