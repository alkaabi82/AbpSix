using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AbpSix;

[Dependency(ReplaceServices = true)]
public class AbpSixBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "AbpSix";
}
