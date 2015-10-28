# A Bloomberg Library

## Status

To make this compile you need the Bloomberg assembly which you can find here: http://www.bloomberglabs.com/api/libraries/

This is a pretty early version. I haven't handled all the error responses, and I've never used it in anger, so I don't know how thread safe it is.

I only have a B-Pipe service, so the SAPI stuff probably doesn't work.

## News

### 28-Oct-2015

#### Treatment of partial responses

Some of the response come as a series of partial responses anding in a final response. Previously I had
been aching the data until the final response, and delivering the consolidated data as the fullfilment
of a promise. I have changed this to an observable which receives each partial response ending in a
completion. I did this for two reasons.

1. In general I think it unwise to anticipate the requirements of the user, and do work that may not be required or desirable.
2. The cached data set might grow unreasonably large.

#### Handling of responses which mix data and errors

Some responses may mix data and errors (for example requesting reference data on a mixed set of tickers). This
was previously ignored or buggy. When an item may be either data or an error it is now returned as an Either monad.

#### Request/Response pattern

Previously some request/response calls used standard library object. These have all been replaced with specific
classes. This makes the intentions of overloaded methods clearer, and makes the interface more stable.

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

_bloomberg.InitialisationStatus += (sender, eventArgs) =>
{
    if (eventArgs.Args)
        _bloomberg.ToObservable(IntradayBarRequest.Create("VOD LN Equity", new DateTime(2015, 10, 16, 9, 0, 0), new DateTime(2015, 10, 16, 12, 00, 0), EventType.BID, 60))
            .Subscribe(
                intradayBarResponse => Debug.Print("{0}: [{1}]", intradayBarResponse.Ticker, string.Join(",", intradayBarResponse.IntradayBars.Select(intradayBar => string.Format("Open={0}, High={1}, Low={2}, Close={3}, Volume={4}, NumEvents={5}", intradayBar.Open, intradayBar.High, intradayBar.Low, intradayBar.Close, intradayBar.Volume, intradayBar.NumEvents)))),
                error => Debug.Print("Intraday Bar Error: {0}", error),
                () => Debug.WriteLine("Intraday Bars complete"));};

_bloomberg.StartAsync();
```

## Notes

I wanted to make the library entirely asynchronous. To keep this simple I have used
two techniques: Observables, and Promises.

The ticking data and requests which may provide partial responses are provided through an observable:

```cs
var disposable = _bloomberg.ToObservable(new[] { "IBM US Equity" }, new[] { "BID", "ASK" })
    .Subscribe(tickerData => Console.WriteLine("{0} - [{1}]", tickerData.Ticker, string.Join(",", tickerData.Data.Select(x => string.Format("{0}: {1}", x.Key, x.Value)))));

// ... to unsubscribe at some point later.
disposable.Dispose();
```

The "one-off" data is provided through a promise:

```cs
_bloomberg.RequestToken().Done(token => Console.WriteLine("Token={0}", token));
```

In both cases the callback is invoked on the thread of the event handler, so long running tasks should
be decoupled. For the observable you might use `.ObserveOn(TaskPoolScheduler.Default)`. For the
promise you could create a task.