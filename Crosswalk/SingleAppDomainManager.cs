using System;
using System.IO;

namespace Crosswalk
{
    public class SingleAppDomainManager : AppDomainManager
    {
        CrosswalkModule.AppPoolInfo _appPoolInfo;
        CrosswalkModule.AppHandlerInfo _appHandlerInfo;
        readonly WebAppDomainManager _webAppDomainManager = new WebAppDomainManager();

        public override void InitializeNewDomain(AppDomainSetup setup)
        {
            _appPoolInfo.CreateAppDomain = CreateAppDomain;
            _appPoolInfo.UnloadAppDomain = UnloadAppDomain;
            CrosswalkModule.BindAppPoolInfo(ref _appPoolInfo);

            setup.ApplicationBase = Path.GetDirectoryName(_appPoolInfo.ClrConfigFile);
            setup.ConfigurationFile = Path.Combine(setup.ApplicationBase, "Web.config");
            setup.PrivateBinPath = "bin";
        }

        int CreateAppDomain(String applicationPhysicalPath, String applicationId, String appConfigPath)
        {
            _appHandlerInfo.BindHandler = _webAppDomainManager.BindHandler;
            CrosswalkModule.BindAppHandlerInfo(ref _appHandlerInfo);
            return AppDomain.CurrentDomain.Id;
        }

        void UnloadAppDomain(int domainId)
        {
            // TODO: demand process unloads
        }
    }
}
