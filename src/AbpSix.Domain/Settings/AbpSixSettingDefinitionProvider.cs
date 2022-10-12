using Volo.Abp.Settings;

namespace AbpSix.Settings;

public class AbpSixSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AbpSixSettings.MySetting1));
    }
}
