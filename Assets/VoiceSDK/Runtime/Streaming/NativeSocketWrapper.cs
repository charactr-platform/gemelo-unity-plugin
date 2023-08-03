using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Gemelo.VoiceSDK.Streaming
{
	public class NativeSocketWrapper
	{
		const string Description = "Closing on user request";

		public WebSocketState Status => _ws.State;
		public event Action OnOpen;
		public event Action<string> OnClose;
		public event Action<byte[]> OnData;
		public event Action<string> OnError;
		
		private readonly ClientWebSocket _ws;
		private readonly CancellationTokenSource _token;
		private readonly Uri _uri;
		public NativeSocketWrapper(string url, int timeout = 5000)
		{
			_ws = new ClientWebSocket();
			_ws.Options.SetRequestHeader("user-agent", Configuration.USER_AGENT);
			
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

			Dispatch();
		}

		public async void SendText(string text)
		{
			var bytesToSendConvert = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));
			
			if (_ws.CloseStatus == null && _ws.State == WebSocketState.Open)
			{
				await _ws.SendAsync(bytesToSendConvert, WebSocketMessageType.Text, true, _token.Token);
			}
			else
			{
				throw new Exception("Can't send data: " + _ws.State);
			}
		}

		public async Task Close()
		{
			if (_ws == null)
			{
				Debug.LogError("Websocket disposed already");
				return;
			}
			
			await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, Description, _token.Token);
			
			_ws.Dispose();
			OnClose?.Invoke(Description);
		}
		
		private async Task Dispatch()
		{
			var rcvBytes = new byte[1024 * 1024 * 10];
			var rcvBuffer = new ArraySegment<byte>(rcvBytes);
			WebSocketReceiveResult result = null;
			
			do
			{
				result = await _ws.ReceiveAsync(rcvBuffer, _token.Token);
				
				if (result.MessageType == WebSocketMessageType.Binary)
				{
					var msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(result.Count).ToArray();
					OnData?.Invoke(msgBytes);
				}
				else
				{
					Debug.LogWarning("Bad data:"+ result.MessageType);
				}
			} 
			while (result.CloseStatus == null);

			rcvBytes = null;
			rcvBuffer = null;
			GC.Collect();
			
			if (result.CloseStatus == WebSocketCloseStatus.NormalClosure)
			{
				await Close();
			}
			else
			{
				_ws.Dispose();
				OnError?.Invoke(result.CloseStatusDescription);
			}
		}
	}
}