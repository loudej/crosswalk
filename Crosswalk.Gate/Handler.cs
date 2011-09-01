using System;
using System.Runtime.InteropServices;
using Gate;
using Environment = Gate.Environment;

namespace Crosswalk.Gate
{
    public class Handler
    {
        readonly AppDelegate _app;

        public Handler(AppDelegate app)
        {
            _app = app;
        }

        public void Execute(
            object transaction,
            IntPtr[] knownServerVariables,
            IntPtr[] knownRequestHeaders,
            IntPtr[] unknownRequestHeaderNames,
            int unknownRequestHeaderNamesCount,
            IntPtr[] unknownRequestHeaderValues,
            int unknownRequestHeaderValuesCount)
        {
            Execute(
                transaction,
                Strings(knownServerVariables),
                Strings(knownRequestHeaders),
                Strings(unknownRequestHeaderNames),
                Strings(unknownRequestHeaderValues));
        }

        static unsafe string[] Strings(IntPtr[] ptrs)
        {
            if (ptrs == null)
            {
                return new string[0];
            }

            var strings = new string[ptrs.Length];
            for (var index = 0; index != ptrs.Length; ++index)
            {
                if (ptrs[index] != IntPtr.Zero)
                {
                    strings[index] = new string((sbyte*)ptrs[index]);
                }
            }
            return strings;
        }

        public void Execute(
            object transaction,
            String[] knownServerVariables,
            String[] knownRequestHeaders,
            String[] unknownRequestHeaderNames,
            String[] unknownRequestHeaderValues)
        {
            var env = new Environment
            {
                Scheme = "http"
            };
            _app(
                env,
                (status, headers, body) =>
                {
                    var headerNames = new string[headers.Count];
                    var headerValues = new string[headers.Count];
                    var headerCount = 0;
                    foreach (var kv in headers)
                    {
                        headerNames[headerCount] = kv.Key;
                        headerValues[headerCount] = kv.Value;
                        ++headerCount;
                    }
                    CrosswalkModule.Call.ResponseStart(transaction, status, headerCount, headerNames, headerValues);
                    body(
                        (data, continuation) =>
                        {
                            if (continuation == null)
                            {
                                bool ignored;
                                CrosswalkModule.Call.ResponseBody(transaction, data.Array, data.Offset, data.Count, null, out ignored);
                                return false;
                            }

                            var pins = new GCHandle[2];
                            CrosswalkModule.ContinuationDelegate callback = () =>
                            {
                                pins[0].Free();
                                pins[1].Free();
                                continuation();
                            };
                            pins[0] = GCHandle.Alloc(data.Array, GCHandleType.Pinned); // prevent byte[] from relocating while async send going on
                            pins[1] = GCHandle.Alloc(callback, GCHandleType.Normal); // prevent delegate from being collected while native callback pending

                            bool async;
                            CrosswalkModule.Call.ResponseBody(transaction, data.Array, data.Offset, data.Count, callback, out async);
                            if (async)
                            {
                                return true;
                            }

                            pins[0].Free();
                            pins[1].Free();
                            return false;
                        },
                        ex2 => CrosswalkModule.Call.ResponseComplete(transaction, Marshal.GetHRForException(ex2)),
                        () => CrosswalkModule.Call.ResponseComplete(transaction, 0));
                },
                ex => CrosswalkModule.Call.ResponseComplete(transaction, Marshal.GetHRForException(ex)));
        }
    }
}