using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;

namespace Crosswalk.Tests.Fakes
{
    public class FakeCrosswalkModule : CrosswalkModule.ICrosswalkModule
    {
        public CrosswalkModule.AppPoolInfo AppPoolInfo;
        public CrosswalkModule.AppHandlerInfo AppHandlerInfo;
        public AppDomainSetup AppDomainSetup;

        public int ResponseStartCalls { get; set; }
        public string ResponseStartStatus { get; set; }
        public int ResponseStartHeaderCount { get; set; }
        public string[] ResponseStartHeaderNames { get; set; }
        public string[] ResponseStartHeaderValues { get; set; }

        public int ResponseBodyCalls { get; set; }

        public int ResponseCompleteCalls { get; set; }
        public int ResponseCompleteHresultForException { get; set; }


        void CrosswalkModule.ICrosswalkModule.BindAppPoolInfo(ref CrosswalkModule.AppPoolInfo info)
        {
            // save incoming values
            AppPoolInfo.CreateAppDomain = info.CreateAppDomain;
            AppPoolInfo.UnloadAppDomain = info.UnloadAppDomain;

            // provide outgoing values
            info.AppPoolName = AppPoolInfo.AppPoolName;
            info.ClrConfigFile = AppPoolInfo.ClrConfigFile;
        }

        void CrosswalkModule.ICrosswalkModule.BindAppHandlerInfo(ref CrosswalkModule.AppHandlerInfo info)
        {
            // save incoming values
            AppHandlerInfo.BindHandler = info.BindHandler;
        }

        void CrosswalkModule.ICrosswalkModule.ResponseStart(object transaction, string status, int headerCount, string[] headerNames, string[] headerValues)
        {
            ResponseStartCalls++;
            ResponseStartStatus = status;
            ResponseStartHeaderCount = headerCount;
            ResponseStartHeaderNames = headerNames;
            ResponseStartHeaderValues = headerValues;
        }


        void CrosswalkModule.ICrosswalkModule.ResponseBody(object transaction, byte[] data, int offset, int count, CrosswalkModule.ContinuationDelegate continuation, out bool async)
        {
            ResponseBodyCalls++;
            async = false;
        }

        void CrosswalkModule.ICrosswalkModule.ResponseComplete(object transaction, int hresultForException)
        {
            ResponseCompleteCalls++;
            ResponseCompleteHresultForException = hresultForException;
        }



        AppDomain CrosswalkModule.ICrosswalkModule.AppDomain_CreateDomain(string friendlyName, Evidence securityInfo, AppDomainSetup info)
        {
            // save provided value
            AppDomainSetup = info;

            // return the current domain for testing purposes
            return AppDomain.CurrentDomain;
        }

        void CrosswalkModule.ICrosswalkModule.AppDomain_Unload(AppDomain domain)
        {
            // do nothing for testing purposes
        }
    }
}
