﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Cassandra.Tests
{
    [TestFixture]
    public class LoggingTests
    {
        [Test]
        public void FactoryBasedLoggerHandler_Methods_Not_Throw()
        {
            UseAllMethods(new Logger.FactoryBasedLoggerHandler(typeof(int)));
        }

        [Test]
        public void FactoryBasedLoggerHandler_Methods_Should_Output_To_Trace()
        {
            var originalLevel = Diagnostics.CassandraTraceSwitch.Level;
            Diagnostics.CassandraTraceSwitch.Level = TraceLevel.Verbose;
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            UseAllMethods(new Logger.TraceBasedLoggerHandler(typeof(int)));
            Trace.Listeners.Remove(listener);
            Assert.AreEqual(6, listener.Messages.Count);
            Diagnostics.CassandraTraceSwitch.Level = originalLevel;
            var expectedMessages = new[]
            {
                "Test exception 1",
                "Message 1",
                "Message 2 Param1",
                "Message 3 Param2",
                "Message 4 Param3",
                "Message 5 Param4"
            };
            var messages = listener.Messages.Keys.OrderBy(k => k).Select(k => listener.Messages[k]).ToArray();
            for (var i = 0; i < expectedMessages.Length; i++)
            {
                StringAssert.Contains(expectedMessages[i], messages[i]);
            }
        }

        private void UseAllMethods(Logger.ILoggerHandler loggerHandler)
        {
            loggerHandler.Error(new Exception("Test exception 1"));
            loggerHandler.Error("Message 1", new Exception("Test exception 1"));
            loggerHandler.Error("Message 2 {0}", "Param1");
            loggerHandler.Info("Message 3 {0}", "Param2");
            loggerHandler.Verbose("Message 4 {0}", "Param3");
            loggerHandler.Warning("Message 5 {0}", "Param4");
        }

        private class TestTraceListener : TraceListener
        {
            public readonly ConcurrentDictionary<int, string> Messages = new ConcurrentDictionary<int, string>();
            private int _counter = -1;

            public override void Write(string message)
            {
            }

            public override void WriteLine(string message)
            {
                Messages.AddOrUpdate(Interlocked.Increment(ref _counter), message, (k, v) => message);
            }
        }
    }
}