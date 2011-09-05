using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Crosswalk.Tests
{
    [TestFixture]
    public class CrosswalkModuleTests
    {
        [Test]
        public void Known_request_header_constants_sanity_check()
        {
            var enumValues = Enum.GetValues(typeof(CrosswalkModule.KnownRequestHeaders))
                .Cast<CrosswalkModule.KnownRequestHeaders>()
                .ToArray();

            // sizes are the same
            Assert.That(enumValues.Count(), Is.EqualTo(CrosswalkModule.KnownRequestHeaderNames.Count()));

            // enum is in order
            for (var index = 0; index != CrosswalkModule.KnownRequestHeaderNames.Count(); ++index)
            {
                Assert.That((int)enumValues[index], Is.EqualTo(index));
            }

            // string contents match enum names
            for (var index = 0; index != CrosswalkModule.KnownRequestHeaderNames.Count(); ++index)
            {
                var tableString = CrosswalkModule.KnownRequestHeaderNames[index];
                var enumName = enumValues[index].ToString();

                var adjustedTableString = "HttpHeader" + tableString.Replace("-", "").Replace("MD5","Md5").Replace("TE","Te");

                Assert.That(adjustedTableString, Is.EqualTo(enumName));
            }
        }
    }
}
