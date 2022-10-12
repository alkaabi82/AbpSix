using AbpSix.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AbpSix.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AbpSixController : AbpControllerBase
{
    protected AbpSixController()
    {
        LocalizationResource = typeof(AbpSixResource);
    }
}
