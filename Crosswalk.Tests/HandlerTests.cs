using System;
using System.Collections.Generic;
using Crosswalk.Gate.Tests.Fakes;
using Crosswalk.Tests.Fakes;
using NUnit.Framework;

namespace Crosswalk.Gate.Tests
{
    [TestFixture]
    public class HandlerTests
    {
        FakeCrosswalkModule _module;
        IDisposable _moduleDispose;
        FakeApp _app;
        Handler _handler;

        [SetUp]
        public void Init()
        {
            _module = new FakeCrosswalkModule();
            _moduleDispose = CrosswalkModule.ReplaceCalls(_module);

        }

        [TearDown]
        public void Term()
        {
            _moduleDispose.Dispose();
        }

        void InitApp(string resultStatus)
        {
            _app = new FakeApp().ForSynchronousResult(
                resultStatus,
                new Dictionary<string, string> (),
                null
            );

            _handler = new Handler(_app.Call);
        }

        [Test]
        public void Handler_calls_app_delegate()
        {
            InitApp("200 TESTING");

            Assert.That(_app.CallCount, Is.EqualTo(0));
            Assert.That(_app.CallEnv, Is.Null);

            var transaction = new object();
            _handler.Execute(
                transaction,
                new string[9], 
                new string[41], 
                new string[0], 
                new string[0]);

            Assert.That(_app.CallCount, Is.EqualTo(1));
            Assert.That(_app.CallEnv, Is.Not.Null);
            Assert.That(_app.ResultStatus, Is.EqualTo("200 TESTING"));
        }

        [Test]
        public void Known_request_headers_appear_in_requestheaders_dictionary()
        {
            InitApp("200 OK");
            var transaction = new object();
            var knownRequestHeaders = new string[41];
            knownRequestHeaders[(int)CrosswalkModule.KnownRequestHeaders.HttpHeaderUserAgent] = "nunit";
            _handler.Execute(
                transaction,
                new string[9], 
                knownRequestHeaders, 
                new string[0], 
                new string[0]);

            var env = new global::Gate.Environment(_app.CallEnv);
            
            Assert.That(env.Headers.ContainsKey("User-Agent"));
            Assert.That(env.Headers["User-Agent"], Is.EqualTo("nunit"));
        }

        [Test]
        public void Unknown_request_headers_appear_in_requestheaders_dictionary()
        {
            InitApp("200 OK");
            var transaction = new object();
            _handler.Execute(
                transaction,
                new string[9], 
                new string[41], 
                new [] {"x-custom"}, 
                new [] {"foo"});

            var env = new global::Gate.Environment(_app.CallEnv);
            
            Assert.That(env.Headers.ContainsKey("x-custom"));
            Assert.That(env.Headers["x-custom"], Is.EqualTo("foo"));
        }

        [Test]
        public void Known_server_variables_appear_in_environment()
        {
            InitApp("200 OK");
            var transaction = new object();
            var knownServerVariables = new string[9];
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.RequestMethod] = "GET";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.ServerPort] = "80";
            _handler.Execute(
                transaction,
                knownServerVariables, 
                new string[41], 
                new string[0], 
                new string[0]);

            var env = new global::Gate.Environment(_app.CallEnv);
            
            Assert.That(env["server.REQUEST_METHOD"], Is.EqualTo("GET"));
            Assert.That(env["server.SERVER_PORT"], Is.EqualTo("80"));
        }

        
        [Test]
        public void OWIN_spec_appears_in_environment()
        {
            InitApp("200 OK");
            var transaction = new object();
            var knownServerVariables = new string[9];
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.RequestMethod] = "GET";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.ScriptName] = "/this/is/a/test.html";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.PathInfo] = "";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.QueryString] = "a=5&b=2";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.ContentType] = "";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.ContentLength] = "";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.ServerName] = "my.server.name";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.ServerPort] = "443";
            knownServerVariables[(int)CrosswalkModule.KnownServerVariables.ServerProtocol] = "HTTPS";

            _handler.Execute(
                transaction,
                knownServerVariables, 
                new string[41], 
                new string[0], 
                new string[0]);

            var env = new global::Gate.Environment(_app.CallEnv);            
            Assert.That(env.Version, Is.EqualTo("1.0"));
            Assert.That(env.Method, Is.EqualTo("GET"));
            Assert.That(env.Scheme, Is.EqualTo("HTTPS"));
            Assert.That(env.PathBase, Is.EqualTo(""));
            Assert.That(env.Path, Is.EqualTo("/this/is/a/test.html"));
            Assert.That(env.QueryString, Is.EqualTo("a=5&b=2"));
            Assert.That(env.Headers, Is.Not.Null);
            Assert.That(env.Body, Is.Null);
        }
    }
}
