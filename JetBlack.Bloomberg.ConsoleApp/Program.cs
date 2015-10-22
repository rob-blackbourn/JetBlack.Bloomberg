using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                AuthenticationOptions = "AuthenticationMode=APPLICATION_ONLY;ApplicationAuthenticationType=APPNAME_AND_KEY;ApplicationName=XXXXXX", // Your server name
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
            _bloomberg.InitialisationStatus += (sender, eventArgs) =>
            {
                if (eventArgs.Args)
                    _bloomberg.Request(IntradayBarRequest.Create("VOD LN Equity", new DateTime(2015, 10, 16, 9, 0, 0), new DateTime(2015, 10, 16, 12, 00, 0), EventType.BID, 60))
                        .Done(intradayBarResponse => Console.WriteLine("{0}: [{1}]", intradayBarResponse.Ticker, string.Join(",", intradayBarResponse.IntradayBars.Select(intradayBar => string.Format("Open={0}, High={1}, Low={2}, Close={3}, Volume={4}, NumEvents={5}", intradayBar.Open, intradayBar.High, intradayBar.Low, intradayBar.Close, intradayBar.Volume, intradayBar.NumEvents)))));
            };

            _bloomberg.StartAsync();

            Console.WriteLine("Press <ENTER> to stop");
            Console.ReadLine();

            _bloomberg.StopAsync();

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();
        }

        private static void OnAuthenticationStatus(object sender, EventArgs<bool> eventArgs)
        {
            Debug.Print("OnAuthenticationStatus: {0}", eventArgs);
        }

        private static void OnInitialisationStatus(object sender, EventArgs<bool> eventArgs)
        {
            Debug.Print("OnInitialisationStatus: {0}", eventArgs);

            if (eventArgs.Args)
            {
                _bloomberg.ToObservable(new[] { "IBM US Equity" }, new[] { "BID", "ASK" })
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(tickerData => Console.WriteLine("{0} - [{1}]", tickerData.Ticker, string.Join(",", tickerData.Data.Select(x => string.Format("{0}: {1}", x.Key, x.Value)))));

                _bloomberg.Request(ReferenceDataRequest.Create(new[] {"VOD LN Equity"}, new[] {"PX_LAST"}))
                    .Done(response => response.ReferenceData.Values.ForEach(tickerData => Console.WriteLine("{0} - [{1}]", tickerData.Ticker, string.Join(",", tickerData.Data.Select(x => string.Format("{0}: {1}", x.Key, x.Value))))));

                //_bloomberg.Request(new SecurityEntitlementsRequest(new[] { "VOD LN Equity" }))
                //    .Done(ShowEntitlements);

                _bloomberg.Request(IntradayTickRequest.Create("VOD LN Equity", new[] { EventType.BID, EventType.ASK, EventType.TRADE }, new DateTime(2015, 10, 16, 9, 0, 0), new DateTime(2015, 10, 16, 9, 10, 0)))
                    .Done(ShowIntradayTicks);

                _bloomberg.Request(IntradayBarRequest.Create("VOD LN Equity", new DateTime(2015, 10, 16, 9, 0, 0), new DateTime(2015, 10, 16, 12, 00, 0), EventType.BID, 60))
                    .Done(intradayBarResponse =>Console.WriteLine("{0}: [{1}]", intradayBarResponse.Ticker, string.Join(",", intradayBarResponse.IntradayBars.Select(intradayBar => string.Format("Open={0}, High={1}, Low={2}, Close={3}, Volume={4}, NumEvents={5}", intradayBar.Open, intradayBar.High, intradayBar.Low, intradayBar.Close, intradayBar.Volume, intradayBar.NumEvents)))));

                //_bloomberg.Request(HistoricalDataRequest.Create(new[] { "VOD LN Equity", "TSCO LN Equity" }, new [] {"BID", "ASK" }, new DateTime(2015, 10, 12, 9, 0, 0), new DateTime(2015, 10, 16, 17, 30, 0), PeriodicitySelection.DAILY))
                //    .Done(ShowHistoricalData);

                Debug.Print("It's asynchronous");
            }
        }

        private static void ShowHistoricalData(HistoricalDataResponse historicalDataResponse)
        {
            Debug.Print("ShowHistoricalData");

            foreach (var historicalTickerData in historicalDataResponse.HistoricalTickerData.Values)
            {
                Debug.Print("Ticker={0}", historicalTickerData.Ticker);
                foreach (var dateAndTickerData in historicalTickerData.Data)
                {
                    Debug.Print("  Date: {0}", dateAndTickerData.Key);
                    foreach (var nameAndValue in dateAndTickerData.Value)
                        Debug.Print("    {0}: {1}", nameAndValue.Key, nameAndValue.Value);
                }
            }
        }

        private static void ShowIntradayBars(IntradayBarResponse intradayBarResponse)
        {
            Debug.Print("ShowIntradayBars");
            Debug.Print("Ticker: {0}", intradayBarResponse.Ticker);
            foreach (var intradayBar in intradayBarResponse.IntradayBars)
                Debug.Print("Open={0}, High={1}, Low={2}, Close={3}, Volume={4}, NumEvents={5}", intradayBar.Open, intradayBar.High, intradayBar.Low, intradayBar.Close, intradayBar.Volume, intradayBar.NumEvents);

        }

        private static void ShowIntradayTicks(IntradayTickResponse intradayTickResponse)
        {
            Debug.Print("ShowIntradayTicks");
            Debug.Print("Ticker: {0}", intradayTickResponse.Ticker);
            foreach (var intradayTick in intradayTickResponse.IntraDayTicks)
                Debug.Print("  {0:yyyy-MM-dd HH:mm:ss.fff} - {1}: {2}", intradayTick.Time, intradayTick.EventType, intradayTick.Value);
        }

        private static void ShowEntitlements(SecurityEntitlementsResponse entitlementsByTicker)
        {
            foreach (var item in entitlementsByTicker.SecurityEntitlements.Values)
                Debug.Print("Ticker={0}, Entitlements=[{1}]", item.Security, string.Join(",", item.EntitlementIds));
        }

        private static void DisplayReferenceData(ReferenceDataResponse referenceDataResponse)
        {
            foreach (var tickerData in referenceDataResponse.ReferenceData.Values)
            {
                Console.WriteLine("Ticker: {0}", tickerData.Ticker);
                foreach (var field in tickerData.Data)
                    Console.WriteLine("  {0}\t : {1}", field.Key, field.Value);
            }
        }

        private static void DisplayTicks(TickerData tickerData)
        {
            Console.WriteLine("Ticker: {0}", tickerData.Ticker);
            foreach (var field in tickerData.Data)
                Console.WriteLine("\t{0} : {1}", field.Key, field.Value);
            Console.WriteLine("{0} - [{1}]", tickerData.Ticker, string.Join(",", tickerData.Data.Select(x => string.Format("{0}: {1}", x.Key, x.Value))));
        }

        private static void DisplayError(Exception error)
        {
            Console.WriteLine("Subscription error: {0}", error);
        }

        private static void OnAdminStatus(object sender, EventArgs<AdminStatus> eventArgs)
        {
            Debug.Print("OnAdminStatus: {0}", eventArgs.Args);
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
