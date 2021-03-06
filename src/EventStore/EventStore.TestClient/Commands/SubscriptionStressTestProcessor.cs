﻿using System;
using System.Diagnostics;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.Core.Tests.Helpers;

namespace EventStore.TestClient.Commands
{
    internal class SubscriptionStressTestProcessor : ICmdProcessor
    {
        public string Usage { get { return "SST [<subscription-count>]"; } }
        public string Keyword { get { return "SST"; } }

        public bool Execute(CommandProcessorContext context, string[] args)
        {
            int subscriptionCount = 5000;

            if (args.Length > 0)
            {
                if (args.Length > 1)
                    return false;
                subscriptionCount = int.Parse(args[0]);
            }

            context.IsAsync();

            var conn = EventStoreConnection.Create(ConnectionSettings.Create()
                                                                     .UseCustomLogger(new ClientApiLoggerBridge(context.Log))
                                                                     .FailOnNoServerResponse()
                                                                     /*.EnableVerboseLogging()*/, 
                                                                     context.Client.TcpEndpoint);
            conn.Connect();

            long appearedCnt = 0;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < subscriptionCount; ++i)
            {
                conn.SubscribeToStream(
                    string.Format("stream-{0}", i),
                    false,
                    (s, e) =>
                    {
                        var c = Interlocked.Increment(ref appearedCnt);
                        if (c%1000 == 0) Console.Write('\'');
                        if (c%100000 == 0)
                        {
                            context.Log.Trace("Received total {0} events ({1} per sec)...", c, 100000.0/sw.Elapsed.TotalSeconds);
                            sw.Restart();
                        }
                    });
            }
            context.Log.Info("Subscribed to {0} streams...", subscriptionCount);
            return true;
        }
    }
}
