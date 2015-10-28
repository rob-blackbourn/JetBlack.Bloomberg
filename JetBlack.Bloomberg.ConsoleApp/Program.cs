using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Bloomberglp.Blpapi;
using JetBlack.Bloomberg.Authenticators;
using JetBlack.Bloomberg.Messages;
using JetBlack.Bloomberg.Models;
using JetBlack.Bloomberg.Requests;

namespace JetBlack.Bloomberg.ConsoleApp
{
    class Program
    {
        private static BloombergController _bloomberg;

        static void Main(string[] args)
        {
            var sessionOptions = new SessionOptions
            {
                ServerHost = "192.168.0.1", // Your server ip address
                ServerPort = 8194, // Your server port
                AuthenticationOptions = "AuthenticationMode=APPLICATION_ONLY;ApplicationAuthenticationType=APPNAME_AND_KEY;ApplicationName=XXXXXX", // Your application name
                AutoRestartOnDisconnection = true
            };

            _bloomberg = new BloombergController(sessionOptions, x => new BpipeAuthenticator(x));
            
            _bloomberg.SessionStatus += (sender, eventArgs) =>
            {
                if (eventArgs.Args == SessionStatus.Started)
                    _bloomberg.AuthenticateAsync();
            };

            _bloomberg.AdminStatus += OnAdminStatus;
            _bloomberg.AuthenticationStatus += OnAuthenticationStatus;
            _bloomberg.InitialisationStatus += OnInitialisationStatus;

            _bloomberg.StartAsync();

            Console.WriteLine("Press <ENTER> to stop");
            Console.ReadLine();

            _bloomberg.StopAsync();

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();
        }

        private static void OnAuthenticationStatus(object sender, EventArgs<bool> eventArgs)
        {
            Console.WriteLine("OnAuthenticationStatus: {0}", eventArgs);
        }

        private static void OnInitialisationStatus(object sender, EventArgs<bool> eventArgs)
        {
            Console.WriteLine("OnInitialisationStatus: {0}", eventArgs);

            if (eventArgs.Args)
            {
                _bloomberg.ToObservable(new[] { new SubscriptionRequest("VOD LN Equity", new[] { "BID", "ASK" }) })
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(
                        response =>
                        {
                            Console.WriteLine("Subscription Received.");
                            Console.WriteLine("{0} - [{1}]", response.Ticker, string.Join(",", response.Data.Select(x => string.Format("{0}: {1}", x.Key, x.Value))));
                        },
                        error => Console.WriteLine("Subscription Error: {0}", error),
                        () => Console.WriteLine("Subscription Completed."));

                _bloomberg.ToObservable(ReferenceDataRequest.Create(new[] { "VOD LN Equity", "TSCO LN Equity" }, new[] { "PX_LAST" }))
                    .Subscribe(
                        response =>
                        {
                            Console.WriteLine("Reference Data Received.");
                            foreach (var item in response)
                            {
                                if (item.Value.IsLeft)
                                    Console.WriteLine("Ticker={0}, Error={1}", item.Key, item.Value.Left);
                                else
                                    Console.WriteLine("{0} - [{1}]", item.Key, string.Join(",", item.Value.Right.Select(x => string.Format("{0}: {1}", x.Key, x.Value))));
                            }
                        },
                        error => Console.WriteLine("Reference Data Error: {0}", error),
                        () => Console.WriteLine("Reference Data Completed."));

                _bloomberg.ToObservable(new SecurityEntitlementsRequest(new[] { "VOD LN Equity", "IBM US Equity" }))
                    .Subscribe(
                        response =>
                        {
                            Console.WriteLine("Security Entitlements Received.");
                            foreach (var item in response)
                                Console.WriteLine("Ticker={0}, Entitlements=[{1}]", item.Key, string.Join(",", item.Value.EntitlementIds));
                        },
                        error => Console.WriteLine("Security Entitlements Error={0}", error),
                        () => Console.WriteLine("Security Entitlements Completed."));

                _bloomberg.ToObservable(IntradayTickRequest.Create("VOD LN Equity", new[] { EventType.BID, EventType.ASK, EventType.TRADE }, new DateTime(2015, 10, 16, 9, 0, 0), new DateTime(2015, 10, 16, 9, 10, 0)))
                    .Subscribe(response =>
                    {
                        Console.WriteLine("Intraday Ticks Received.");
                        Console.WriteLine("Ticker: {0}", response.Ticker);
                        foreach (var intradayTick in response.IntraDayTicks)
                            Console.WriteLine("  {0:yyyy-MM-dd HH:mm:ss.fff} - {1}: {2}", intradayTick.Time, intradayTick.EventType, intradayTick.Value);
                    },
                        error => Console.WriteLine("Intraday Tick Error: {0}", error),
                        () => Console.WriteLine("Intraday Tick Completed."));

                _bloomberg.ToObservable(IntradayBarRequest.Create("VOD LN Equity", new DateTime(2015, 10, 16, 9, 0, 0), new DateTime(2015, 10, 16, 12, 00, 0), EventType.BID, 60))
                    .Subscribe(
                        intradayBarResponse =>
                        {
                            Console.WriteLine("Intraday Bar Received.");
                            Console.WriteLine("{0}: [{1}]", intradayBarResponse.Ticker, string.Join(",", intradayBarResponse.IntradayBars.Select(intradayBar => string.Format("Open={0}, High={1}, Low={2}, Close={3}, Volume={4}, NumEvents={5}", intradayBar.Open, intradayBar.High, intradayBar.Low, intradayBar.Close, intradayBar.Volume, intradayBar.NumEvents))));
                        },
                        error => Console.WriteLine("Intraday Bar Error: {0}", error),
                        () => Console.WriteLine("Intraday Bars complete"));

                _bloomberg.ToObservable(HistoricalDataRequest.Create(new[] { "VOD LN Equity", "TSCO LN Equity" }, new[] { "PX_LAST" }, DateTime.Today.AddMonths(-2), DateTime.Today, PeriodicitySelection.DAILY))
                    .Subscribe(
                        response =>
                        {
                            Console.WriteLine("Historical Data Received");
                            foreach (var item in response)
                            {
                                if (item.Value.IsLeft)
                                {
                                    Console.WriteLine("Ticker={0}, SecurityError={1}", item.Key, item.Value.Left);
                                }
                                else
                                {
                                    Console.WriteLine("Ticker={0}", item.Key);
                                    foreach (var dateAndFields in item.Value.Right)
                                    {
                                        Console.WriteLine("  Date: {0}", dateAndFields.Key);
                                        foreach (var nameAndValue in dateAndFields.Value)
                                            Console.WriteLine("    {0}: {1}", nameAndValue.Key, nameAndValue.Value);
                                    }
                                }
                            }
                        },
                        error => Console.WriteLine("Historical Data Error: {0}", error),
                        () => Console.WriteLine("Historical Data Completed."));

                _bloomberg.RequestToken().Done(token => Console.WriteLine("Token={0}", token));

                Console.WriteLine("It's asynchronous");
            }
        }

        private static void OnAdminStatus(object sender, EventArgs<AdminStatus> eventArgs)
        {
            Console.WriteLine("OnAdminStatus: {0}", eventArgs.Args);
        }
    }

    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
                action(item);
        }
    }
}
