using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Charactr.VoiceSDK.Tests
{
	public class NativeSocketWrapper
	{
		public WebSocketState Status => _ws.State;
		public event Action OnOpen;
		public event Action<string> OnClose;
		public event Action<byte[]> OnData;
		public event Action<string> OnError;
		
		private readonly ClientWebSocket _ws;
		private readonly CancellationTokenSource _token;
		private readonly Uri _uri;
		private Task _dispatch;
		public NativeSocketWrapper(string url, int timeout = 5000)
		{
			_ws = new ClientWebSocket();
			_uri = new Uri(url);
			_token = new CancellationTokenSource();
			_token.CancelAfter(timeout);
		}

		public async void Connect()
		{
			await _ws.ConnectAsync(_uri, _token.Token);
			
			if (Status == WebSocketState.Open)
				OnOpen?.Invoke();
			else
				OnError?.Invoke(Status.ToString());

			_dispatch = Dispatch();
		}

		public async void SendText(string text)
		{
			var bytesToSendConvert = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));
			await _ws.SendAsync(bytesToSendConvert, WebSocketMessageType.Text, true, _token.Token);
		}

		public async void Close()
		{
			if (Status != WebSocketState.Open)
				throw new Exception("Can't close, status: " + Status);

			_dispatch.Dispose();
			var description = "Closing on user request";
			await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, description , _token.Token);
			OnClose?.Invoke(description);
		}
		
		private async Task Dispatch()
		{
			var rcvBytes = new byte[128];
			var rcvBuffer = new ArraySegment<byte>(rcvBytes);
			WebSocketReceiveResult result = null;
			
			do
			{
				result = await _ws.ReceiveAsync(rcvBuffer, _token.Token);
				var msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(result.Count).ToArray();
				OnData?.Invoke(msgBytes);
			} 
			while (result.CloseStatus == null);
			
			if (result.CloseStatus == WebSocketCloseStatus.NormalClosure)
			{
				OnClose?.Invoke(result.CloseStatusDescription);
			}
			else
			{
				OnError?.Invoke(result.CloseStatusDescription);
			}
		}
	}
}