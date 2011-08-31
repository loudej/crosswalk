using System;
using System.Collections.Generic;
using System.Text;
using Gate;

namespace Sandbox
{
    public class HelloWorldApp
    {
        public static AppDelegate Create()
        {
            // ReSharper disable AccessToModifiedClosure
            return (env, result, fault) =>
                result(
                    "200 OK",
                    new Dictionary<string, string> { { "Content-Type", "text/plain" }, { "X-Framework", "AdHoc" } },
                    (data, error, complete) =>
                    {
                        var sendCount = 0;

                        var stop = false;
                        Action guarded = () => { };
                        Action loop = () =>
                        {
                            while (!stop)
                            {
                                if (sendCount == 0)
                                {
                                    var message = string.Format("AppDomain.CurrentDomain.Id {0}", AppDomain.CurrentDomain.Id);
                                    data(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), null);
                                }

                                if (sendCount++ == 100)
                                {
                                    complete();
                                    return;
                                }

                                var text = "Hello World. ";
                                text = text + text;
                                text = text + text;
                                text = text + text;
                                text = text + text;
                                text = text + text;
                                text = text + text;
                                text = text + text;
                                text = text + text;
                                text = text + text;

                                if (data(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Chunk " + sendCount + " " + text + "\r\n")), guarded))
                                    return;
                            }
                        };
                        guarded = () =>
                        {
                            try
                            {
                                loop();
                            }
                            catch (Exception ex)
                            {
                                error(ex);
                            }
                        };
                        loop();

                        return () => stop = true;
                    });
        }
    }
}