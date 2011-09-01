using System;
using Crosswalk.Tests.Fakes;
using NUnit.Framework;

namespace Crosswalk.Tests
{
    [TestFixture]
    public class DomainManagerTests
    {
        [Test]
        public void DefaultAppDomainManager_adjusts_setup_and_provides_management_methods()
        {
            var module = new FakeCrosswalkModule
            {
                AppPoolInfo =
                    {
                        AppPoolName = "Testing",
                        ClrConfigFile = @"x:\no-such-folder\testing.config"
                    }
            };

            using (CrosswalkModule.ReplaceCalls(module))
            {
                Assert.That(module.AppPoolInfo.CreateAppDomain, Is.Null);
                Assert.That(module.AppPoolInfo.UnloadAppDomain, Is.Null);

                var manager = new DefaultAppDomainManager();
                var setup = new AppDomainSetup();
                manager.InitializeNewDomain(setup);

                Assert.That(setup.ConfigurationFile, Is.EqualTo(@"x:\no-such-folder\testing.config"));
                Assert.That(setup.ApplicationBase, Is.EqualTo(@"x:\no-such-folder"));
                Assert.That(module.AppPoolInfo.CreateAppDomain, Is.Not.Null);
                Assert.That(module.AppPoolInfo.UnloadAppDomain, Is.Not.Null);
            }
        }

        [Test]
        public void DefaultAppDomainManager_setup_for_web_domains_follows_same_approximate_pattern_as_web_applications()
        {
            var module = new FakeCrosswalkModule
            {
                AppPoolInfo =
                    {
                        AppPoolName = "Testing",
                        ClrConfigFile = @"x:\no-such-folder\testing.config"
                    }
            };
            using (CrosswalkModule.ReplaceCalls(module))
            {
                var manager = new DefaultAppDomainManager();
                var setup = new AppDomainSetup();
                manager.InitializeNewDomain(setup);

                var domainId = module.AppPoolInfo.CreateAppDomain(@"x:\no-such-website\", "ID0", "CFG0");

                // the fake set of calls don't really create another domain
                Assert.That(domainId, Is.EqualTo(AppDomain.CurrentDomain.Id));

                // same default assumptions as a web app
                Assert.That(module.AppDomainSetup.ApplicationBase, Is.EqualTo(@"x:\no-such-website\"));
                Assert.That(module.AppDomainSetup.ConfigurationFile, Is.EqualTo(@"x:\no-such-website\Web.config"));
                Assert.That(module.AppDomainSetup.PrivateBinPath, Is.EqualTo(@"bin"));

                // specified a different class to finish initialization in the new domain
                Assert.That(module.AppDomainSetup.AppDomainManagerType, Is.StringEnding("WebAppDomainManager"));
            }
        }

        [Test]
        public void WebAppDomainManager_provides_binder_method_as_the_domain_is_loading()
        {
            var module = new FakeCrosswalkModule();

            using (CrosswalkModule.ReplaceCalls(module))
            {
                Assert.That(module.AppHandlerInfo.BindHandler, Is.Null);

                var manager = new WebAppDomainManager();
                var setup = new AppDomainSetup();
                manager.InitializeNewDomain(setup);

                Assert.That(module.AppHandlerInfo.BindHandler, Is.Not.Null);
            }
        }
    }
}
