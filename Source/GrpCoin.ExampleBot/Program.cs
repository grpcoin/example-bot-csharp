// Copyright 2021 Ali Göl
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

var token = GetToken();
var channel = CreateAuthenticatedChannel(GetUrl(), token); ;

var authClient = new GrpCoin.Account.AccountClient(channel);
var authResponse = await authClient.TestAuthAsync(new GrpCoin.TestAuthRequest());
Console.WriteLine($"You are user {authResponse.UserId}");

var paperTradeClient = new GrpCoin.PaperTrade.PaperTradeClient(channel);
var portfolioResponse = await paperTradeClient.PortfolioAsync(new GrpCoin.PortfolioRequest(), null);
Console.WriteLine($"Cash Position:{portfolioResponse.CashUsd}");
foreach (var position in portfolioResponse.Positions)
{
	Console.WriteLine($"Coin amount: {position.Amount}");
}
var orderResponse = await paperTradeClient.TradeAsync(new GrpCoin.TradeRequest
{
	Action = GrpCoin.TradeAction.Buy,
	Currency = new GrpCoin.Currency { Symbol = "BTC" },
	Quantity = new GrpCoin.Amount { Units = 0, Nanos = 99_990_000 }
});
Console.WriteLine($"ORDER EXECUTED: {orderResponse.Action} [{orderResponse.Quantity}] coins at USD[{orderResponse.ExecutedPrice}]");

var tickerClient = new GrpCoin.TickerInfo.TickerInfoClient(channel);
await foreach (var item in tickerClient.Watch(new GrpCoin.TickerWatchRequest
{
	Currency = new GrpCoin.Currency { Symbol = "BTC" }
}, null).ResponseStream.ReadAllAsync())
{
	Console.Write(item.Price);
	Console.Write("---");
	Console.Write(item.T);
	Console.WriteLine();
}

string GetToken()
{
	var token = Environment.GetEnvironmentVariable("TOKEN");
	if (string.IsNullOrEmpty(token))
	{
		throw new Exception("Create a permissionless Personal Access Token on GitHub and set it to TOKEN environment variable");
	}
	return token;
}
string GetUrl()
{
	const string prod = "https://api.grpco.in:443";
	const string local = "http://localhost:8080";
	return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOCAL")) ? prod : local;
}
GrpcChannel CreateAuthenticatedChannel(string address, string token)
{
	var credentials = CallCredentials.FromInterceptor((context, metadata) =>
	{
		if (!string.IsNullOrEmpty(token))
		{
			metadata.Add("Authorization", $"Bearer {token}");
		}
		return Task.CompletedTask;
	});
	// SslCredentials is used here because this channel is using TLS.
	// CallCredentials can't be used with ChannelCredentials.Insecure on non-TLS channels.
	var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
	{
		Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
	});
	return channel;
}
