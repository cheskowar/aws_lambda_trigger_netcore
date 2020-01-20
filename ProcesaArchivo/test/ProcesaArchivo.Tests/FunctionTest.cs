using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using ProcesaArchivo;
using Newtonsoft.Json;
using Amazon.Lambda.S3Events;
using System.IO;

namespace ProcesaArchivo.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestInvokeFunction()
        {

            // Invoke the lambda function and confirm the string was upper cased.
            var context = new TestLambdaContext();
            var s3_event = JsonConvert.DeserializeObject<S3Event>(await File.ReadAllTextAsync("event.json"));
            var resultTest = Program.FunctionHandler(s3_event, context);

            Assert.Equal("ok", resultTest);
        }
    }
}
