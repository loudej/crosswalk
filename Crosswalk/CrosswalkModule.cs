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

            void ResponseBody(
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

            void ICrosswalkModule.ResponseBody(object transaction, byte[] data, int offset, int count, ContinuationDelegate continuation, out bool async)
            {
                _ResponseBody(transaction, data, offset, count, continuation, out async);
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
            static extern void _ResponseBody(
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

        public enum KnownServerVariables
        {
            RequestMethod,
            ScriptName,
            PathInfo,
            QueryString,
            ContentType,
            ContentLength,
            ServerName,
            ServerPort,
            ServerProtocol,
        };

        public static string[] KnownServerVariableNames =
        {
            "REQUEST_METHOD",
            "SCRIPT_NAME",
            "PATH_INFO",
            "QUERY_STRING",
            "CONTENT_TYPE",
            "CONTENT_LENGTH",
            "SERVER_NAME",
            "SERVER_PORT",
            "SERVER_PROTOCOL",
        };

        public enum KnownRequestHeaders
        {
            HttpHeaderCacheControl = 0,    // general-header [section 4.5]
            HttpHeaderConnection = 1,    // general-header [section 4.5]
            HttpHeaderDate = 2,    // general-header [section 4.5]
            HttpHeaderKeepAlive = 3,    // general-header [not in rfc]
            HttpHeaderPragma = 4,    // general-header [section 4.5]
            HttpHeaderTrailer = 5,    // general-header [section 4.5]
            HttpHeaderTransferEncoding = 6,    // general-header [section 4.5]
            HttpHeaderUpgrade = 7,    // general-header [section 4.5]
            HttpHeaderVia = 8,    // general-header [section 4.5]
            HttpHeaderWarning = 9,    // general-header [section 4.5]
            HttpHeaderAllow = 10,   // entity-header  [section 7.1]
            HttpHeaderContentLength = 11,   // entity-header  [section 7.1]
            HttpHeaderContentType = 12,   // entity-header  [section 7.1]
            HttpHeaderContentEncoding = 13,   // entity-header  [section 7.1]
            HttpHeaderContentLanguage = 14,   // entity-header  [section 7.1]
            HttpHeaderContentLocation = 15,   // entity-header  [section 7.1]
            HttpHeaderContentMd5 = 16,   // entity-header  [section 7.1]
            HttpHeaderContentRange = 17,   // entity-header  [section 7.1]
            HttpHeaderExpires = 18,   // entity-header  [section 7.1]
            HttpHeaderLastModified = 19,   // entity-header  [section 7.1]
            HttpHeaderAccept = 20,   // request-header [section 5.3]
            HttpHeaderAcceptCharset = 21,   // request-header [section 5.3]
            HttpHeaderAcceptEncoding = 22,   // request-header [section 5.3]
            HttpHeaderAcceptLanguage = 23,   // request-header [section 5.3]
            HttpHeaderAuthorization = 24,   // request-header [section 5.3]
            HttpHeaderCookie = 25,   // request-header [not in rfc]
            HttpHeaderExpect = 26,   // request-header [section 5.3]
            HttpHeaderFrom = 27,   // request-header [section 5.3]
            HttpHeaderHost = 28,   // request-header [section 5.3]
            HttpHeaderIfMatch = 29,   // request-header [section 5.3]
            HttpHeaderIfModifiedSince = 30,   // request-header [section 5.3]
            HttpHeaderIfNoneMatch = 31,   // request-header [section 5.3]
            HttpHeaderIfRange = 32,   // request-header [section 5.3]
            HttpHeaderIfUnmodifiedSince = 33,   // request-header [section 5.3]
            HttpHeaderMaxForwards = 34,   // request-header [section 5.3]
            HttpHeaderProxyAuthorization = 35,   // request-header [section 5.3]
            HttpHeaderReferer = 36,   // request-header [section 5.3]
            HttpHeaderRange = 37,   // request-header [section 5.3]
            HttpHeaderTe = 38,   // request-header [section 5.3]
            HttpHeaderTranslate = 39,   // request-header [webDAV, not in rfc 2518]
            HttpHeaderUserAgent = 40,   // request-header [section 5.3]
        }

        public static string[] KnownRequestHeaderNames =
        {
            "Cache-Control",
            "Connection",
            "Date",
            "Keep-Alive",
            "Pragma",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
            "Via",
            "Warning",
            "Allow",
            "Content-Length",
            "Content-Type",
            "Content-Encoding",
            "Content-Language",
            "Content-Location",
            "Content-MD5",
            "Content-Range",
            "Expires",
            "Last-Modified",
            "Accept",
            "Accept-Charset",
            "Accept-Encoding",
            "Accept-Language",
            "Authorization",
            "Cookie",
            "Expect",
            "From",
            "Host",
            "If-Match",
            "If-Modified-Since",
            "If-None-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Max-Forwards",
            "Proxy-Authorization",
            "Referer",
            "Range",
            "TE",
            "Translate",
            "User-Agent",
        };
    }
}

