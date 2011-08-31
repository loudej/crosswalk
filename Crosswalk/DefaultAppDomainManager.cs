using System;
using System.Collections.Generic;
using System.IO;

namespace Crosswalk
{
    public class DefaultAppDomainManager : AppDomainManager
    {
        readonly object _crit = new object();
        CrosswalkModule.AppPoolInfo _info;
        readonly IDictionary<int, AppDomain> _appDomains = new Dictionary<int, AppDomain>();

        public override void InitializeNewDomain(AppDomainSetup setup)
        {
            _info.CreateAppDomain = CreateAppDomain;
            _info.UnloadAppDomain = UnloadAppDomain;
            CrosswalkModule.BindAppPoolInfo(ref _info);

            setup.ApplicationBase = Path.GetDirectoryName(_info.ClrConfigFile);
            setup.ConfigurationFile = _info.ClrConfigFile;
        }

        int CreateAppDomain(String applicationPhysicalPath, String applicationId, String appConfigPath)
        {
            var setup = new AppDomainSetup
            {
                ApplicationBase = applicationPhysicalPath,
                ConfigurationFile = Path.Combine(applicationPhysicalPath, "Web.config"),
                PrivateBinPath = "bin",
                AppDomainManagerType = typeof(WebAppDomainManager).FullName,
                AppDomainManagerAssembly = typeof(WebAppDomainManager).Assembly.FullName,
            };

            var appDomain = AppDomain.CreateDomain(applicationId, null, setup);
            lock (_crit)
            {
                _appDomains.Add(appDomain.Id, appDomain);
            }
            return appDomain.Id;
        }

        void UnloadAppDomain(int domainId)
        {
            AppDomain appDomain;
            lock (_crit)
            {
                appDomain = _appDomains[domainId];
                _appDomains.Remove(domainId);
            }
            AppDomain.Unload(appDomain);
        }
    }
}
