using System;
using System.Collections.Generic;
using Gate;
using NUnit.Framework;

namespace Crosswalk.Gate.Tests.Fakes
{
    public class FakeApp : IApplication
    {
        public AppDelegate Create()
        {
            return Call;
        }

        
        public int CallCount { get; set; }
        public IDictionary<string, object> CallEnv { get; set; }
        public ResultDelegate CallResult { get; set; }
        public Action<Exception> CallFault { get; set; }

        public void Call(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
        {
            CallCount++;
            CallEnv = env;
            CallResult = result;
            CallFault = fault;
            if (CallResultSynchronously)
            {
                result(ResultStatus, ResultHeaders, ResultBody);
            }
        }

        
        public bool CallResultSynchronously { get; set; }
        public string ResultStatus { get; set; }
        public IDictionary<string, string> ResultHeaders { get; set; }
        public BodyDelegate ResultBody { get; set; }

        public FakeApp ForSynchronousResult(string status, IDictionary<string, string> headers, BodyDelegate body)
        {
            CallResultSynchronously = true;
            ResultStatus = status;
            ResultHeaders = headers;
            ResultBody = body;
            return this;
        }


    }
}