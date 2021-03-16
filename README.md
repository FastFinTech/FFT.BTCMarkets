# FFT.BTCMarkets

[![Source code](https://img.shields.io/static/v1?style=flat&label=&message=Source%20Code&logo=read-the-docs&color=informational)](https://github.com/FastFinTech/FFT.BTCMarkets)
[![NuGet
package](https://img.shields.io/nuget/v/FFT.BTCMarkets.svg)](https://nuget.org/packages/FFT.BTCMarkets)
[![Full documentation](https://img.shields.io/static/v1?style=flat&label=&message=Documentation&logo=read-the-docs&color=green)](https://fastfintech.github.io/FFT.BTCMarkets/)

`FFT.BTCMarkets` is a .Net client for the [BTCMarkets
api](https://api.btcmarkets.net/doc/v3)

[Under construction] Features are being added as needed.

Use the latest version 3.x.x package to connect to the BTCMarkets V3 api. When BTCMarkets
releases new api versions, this package will adjust new major versions to match
the BTCMarkets api version. For example, when Binance releases their V4 version, you
can use the latest 4.x.x package to connect to it.

### Usage
The basic idea is to create a long-lived singleton instance of the api client
which you reuse throughout your application. It is threadsafe.

```csharp
// TODO: basic code sample;
```

[See complete documentation including the list of `BTCApiClient` methods.](https://fastfintech.github.io/FFT.BTCMarkets/api/FFT.BTCMarkets.BTCApiClient.html)