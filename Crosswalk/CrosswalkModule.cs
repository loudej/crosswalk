using System;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace Crosswalk
{
    public static class CrosswalkModule
    {
        static ICrosswalkModule _crosswalkModule = new CrosswalkModuleDll();

        public static IDisposable ReplaceCalls(ICrosswalkModule crosswalkModule)
        {
            var prior = _crosswalkModule;
            _crosswalkModule = crosswalkModule;
            return new Disposable(() => _crosswalkModule = prior);
        }

        class Disposable : IDisposable
        {
            readonly Func<ICrosswalkModule> _dispose;
            public Disposable(Func<ICrosswalkModule> dispose) { _dispose = dispose; }
            void IDisposable.Dispose() { _dispose(); }
        }

        public static ICrosswalkModule Call { get { return _crosswalkModule; } }

        /// <summary>
        /// Interface of methods exported by native CrosswalkModule.dll
        /// Called from managed code via CrosswalkModule.Call property
        /// Unit tests may hook these via CrosswalkModule.ReplaceCalls method
        /// </summary>
        public interface ICrosswalkModule
        {
            void BindAppPoolInfo(
                ref AppPoolInfo info);

            void BindAppHandlerInfo(
                ref AppHandlerInfo info);

            void ResponseStart(
                object transaction,
                string status,
                int headerCount,
                string[] headerNames,
                string[] headerValues);

            bool ResponseBody(
                object transaction,
                byte[] data,
                int offset,
                int count,
                ContinuationDelegate continuation,
                out bool async);

            void ResponseComplete(
                object transaction,
                int hresultForException);


            /// <summary>
            /// This is a pass-through to AppDomain.CreateDomain provided here
            /// so tests may hook those calls
            /// </summary>
            AppDomain AppDomain_CreateDomain(
                string friendlyName, 
                Evidence securityInfo, 
                AppDomainSetup info);
            
            /// <summary>
            /// This is a pass-through to AppDomain.Unload provided here
            /// so tests may hook those calls
            /// </summary>
            void AppDomain_Unload(
                AppDomain domain);
        }

        public class CrosswalkModuleDll : ICrosswalkModule
        {
            void ICrosswalkModule.BindAppPoolInfo(ref AppPoolInfo info)
            {
                _BindAppPoolInfo(ref info);
            }

            void ICrosswalkModule.BindAppHandlerInfo(ref AppHandlerInfo info)
            {
                _BindAppHandlerInfo(ref info);
            }

            void ICrosswalkModule.ResponseStart(object transaction, string status, int headerCount, string[] headerNames, string[] headerValues)
            {
                _ResponseStart(transaction, status, headerCount, headerNames, headerValues);
            }

            bool ICrosswalkModule.ResponseBody(object transaction, byte[] data, int offset, int count, ContinuationDelegate continuation, out bool async)
            {
                return _ResponseBody(transaction, data, offset, count, continuation, out async);
            }

            void ICrosswalkModule.ResponseComplete(object transaction, int hresultForException)
            {
                _ResponseComplete(transaction, hresultForException);
            }

            AppDomain ICrosswalkModule.AppDomain_CreateDomain(string friendlyName, Evidence securityInfo, AppDomainSetup info)
            {
                return AppDomain.CreateDomain(friendlyName, securityInfo, info);
            }

            void ICrosswalkModule.AppDomain_Unload(AppDomain domain)
            {
                AppDomain.Unload(domain);
            }

            [DllImport("CrosswalkModule.dll", EntryPoint = "BindAppPoolInfo", CallingConvention = CallingConvention.StdCall)]
            static extern void _BindAppPoolInfo(ref AppPoolInfo info);

            [DllImport("CrosswalkModule.dll", EntryPoint = "BindAppHandlerInfo", CallingConvention = CallingConvention.StdCall)]
            static extern void _BindAppHandlerInfo(ref AppHandlerInfo info);

            [DllImport("CrosswalkModule.dll", EntryPoint = "ResponseStart", CallingConvention = CallingConvention.StdCall)]
            static extern void _ResponseStart(
                [MarshalAs(UnmanagedType.IUnknown)] object transaction,
                [MarshalAs(UnmanagedType.LPWStr)] string status,
                int headerCount,
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] headerNames,
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] headerValues);

            [DllImport("CrosswalkModule.dll", EntryPoint = "ResponseBody", CallingConvention = CallingConvention.StdCall)]
            static extern bool _ResponseBody(
                [MarshalAs(UnmanagedType.IUnknown)] object transaction,
                [MarshalAs(UnmanagedType.LPArray)] byte[] data,
                int offset,
                int count,
                ContinuationDelegate continuation,
                [MarshalAs(UnmanagedType.Bool)] out bool async);

            [DllImport("CrosswalkModule.dll", EntryPoint = "ResponseComplete", CallingConvention = CallingConvention.StdCall)]
            static extern void _ResponseComplete(
                [MarshalAs(UnmanagedType.IUnknown)] object transaction,
                int hresultForException);
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct AppPoolInfo
        {
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public CreateAppDomainDelegate CreateAppDomain;

            [MarshalAs(UnmanagedType.FunctionPtr)]
            public UnloadAppDomainDelegate UnloadAppDomain;

            [MarshalAs(UnmanagedType.BStr)]
            public string AppPoolName;

            [MarshalAs(UnmanagedType.BStr)]
            public string ClrConfigFile;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CreateAppDomainDelegate(
            [MarshalAs(UnmanagedType.LPWStr)] String applicationPhysicalPath,
            [MarshalAs(UnmanagedType.LPWStr)] String applicationId,
            [MarshalAs(UnmanagedType.LPWStr)] String appConfigPath
            );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UnloadAppDomainDelegate(
            int domainId
            );




        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct AppHandlerInfo
        {
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public BindHandlerDelegate BindHandler;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void BindHandlerDelegate(
            [In] ref BindHandlerContext context,
            [MarshalAs(UnmanagedType.FunctionPtr), Out] out ExecuteHandlerDelegate executeHandler
        );

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BindHandlerContext
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string ManagedType;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string ScriptProcessor;
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ExecuteHandlerDelegate(
            [MarshalAs(UnmanagedType.IUnknown)] object transaction,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 9)] IntPtr[] knownServerVariables,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 41)] IntPtr[] knownRequestHeaders,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] IntPtr[] unknownRequestHeaderNames,
            int unknownRequestHeaderNamesCount,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)] IntPtr[] unknownRequestHeaderValues,
            int unknownRequestHeaderValuesCount
            );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ContinuationDelegate();

    }
}

