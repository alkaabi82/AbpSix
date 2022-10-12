using AbpSix.Localization;
using Volo.Abp.Application.Services;

namespace AbpSix;

/* Inherit your application services from this class.
 */
public abstract class AbpSixAppService : ApplicationService
{
    protected AbpSixAppService()
    {
        LocalizationResource = typeof(AbpSixResource);
    }
}
