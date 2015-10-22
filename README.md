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

