using System;
using System.Runtime.InteropServices;

namespace Crosswalk
{
    public static class CrosswalkModule
    {
        [DllImport("CrosswalkModule.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void BindAppPoolInfo(ref AppPoolInfo info);

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



        [DllImport("CrosswalkModule.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void BindAppHandlerInfo(ref AppHandlerInfo info);

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
            [MarshalAs(UnmanagedType.LPWStr)] public string Name;
            [MarshalAs(UnmanagedType.LPWStr)] public string ManagedType;
            [MarshalAs(UnmanagedType.LPWStr)] public string ScriptProcessor;
        }

        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ExecuteHandlerDelegate(
            [MarshalAs(UnmanagedType.IUnknown)] object transaction,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 9)] IntPtr [] knownServerVariables,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 41)] IntPtr [] knownRequestHeaders,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] IntPtr [] unknownRequestHeaderNames,
            int unknownRequestHeaderNamesCount,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 6)] IntPtr [] unknownRequestHeaderValues,
            int unknownRequestHeaderValuesCount
            );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ContinuationDelegate();

        [DllImport("CrosswalkModule.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void ResponseStart(
            [MarshalAs(UnmanagedType.IUnknown)] object transaction,
            [MarshalAs(UnmanagedType.LPWStr)] string status,
            int headerCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] headerNames,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] headerValues);

        [DllImport("CrosswalkModule.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool ResponseBody(
            [MarshalAs(UnmanagedType.IUnknown)] object transaction,
            [MarshalAs(UnmanagedType.LPArray)] byte[] data,
            int offset,
            int count,
            ContinuationDelegate continuation,
            [MarshalAs(UnmanagedType.Bool)] out bool async);

        [DllImport("CrosswalkModule.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void ResponseComplete(
            [MarshalAs(UnmanagedType.IUnknown)] object transaction,
            int hresultForException);
    }
}

