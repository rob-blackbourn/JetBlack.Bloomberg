# A Bloomberg Library

## Status

To make this compile you need the Bloomberg assembly which you can find here: http://www.bloomberglabs.com/api/libraries/

This is a pretty early version. I haven't handled all the error responses, and I've never used it in anger, so I don't know how thread safe it is.

## Notes

I wanted to make the library entirely asynchronous. To keep this simple I have used
two techniques: Observables, and Promises.

The ticking data is provided through an observable:

```cs
_bloomberg.ToObservable(new[] { "IBM US Equity" }, new[] { "BID", "ASK" })
    .Subscribe(tickerData => Console.WriteLine("{0} - [{1}]", tickerData.Ticker, string.Join(",", tickerData.Data.Select(x => string.Format("{0}: {1}", x.Key, x.Value)))));
```

The "one-off" data is provided through a promise:

```cs
_bloomberg.Request(ReferenceDataRequest.Create(new[] {"VOD LN Equity"}, new[] {"PX_LAST"}))
    .Done(response => response.ReferenceData.Values.ForEach(tickerData => Console.WriteLine("{0} - [{1}]", tickerData.Ticker, string.Join(",", tickerData.Data.Select(x => string.Format("{0}: {1}", x.Key, x.Value))))));
```

## Usage

There's a program to look at, but here's a quick example:

```cs
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
```