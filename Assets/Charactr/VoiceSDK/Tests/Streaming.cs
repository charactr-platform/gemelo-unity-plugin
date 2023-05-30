using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Streaming;
using NUnit.Framework;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Charactr.VoiceSDK.Tests
{
	public class Streaming
	{
		private const string Text = "Hello from Charactr Software Development Kit for Unity";
		private const int VoiceId = 112;
		private async Task<ClientWebSocket> GetClient(int voiceId = 151)
		{
			var ws = new ClientWebSocket();
			var serverUri = new Uri(AudioStreamingManager.URL + $"?voiceId={voiceId}");
			var source = new CancellationTokenSource();
			source.CancelAfter(5000);

			await ws.ConnectAsync(serverUri, source.Token);
			return ws;
		}

		private Configuration _configuration;
		
		[SetUp]
		public void Setup()
		{
			_configuration = Configuration.LoadStreaming();
			
			Assert.NotNull(_configuration);
			Assert.IsNotEmpty(_configuration.ApiClient);
			Assert.IsNotEmpty(_configuration.ApiKey);
		}
		
		[Test]
		public async Task ConnectToWebSocket_StatusOpen()
		{
			using (var ws = await GetClient())
			{
				Assert.IsTrue(ws.State == WebSocketState.Open);
			}
		}

		[Test]
		public async Task SendAuthMessage_StatusPolicyViolation()
		{
			var ws = await GetClient(151);
			
			Assert.NotNull(ws);
			Assert.IsTrue(ws.State == WebSocketState.Open);
			
			var authCommand = AudioStreamingClientBase.GetAuthCommand(_configuration.ApiKey, _configuration.ApiClient);
			
			var bytesToSendAuth = new ArraySegment<byte>(Encoding.UTF8.GetBytes(authCommand));
			await ws.SendAsync(bytesToSendAuth, WebSocketMessageType.Text, true, CancellationToken.None);

			Assert.IsTrue(ws.State == WebSocketState.Open);
			
			var receiveBuffer = new byte[200];

			var offset = 0;
			var dataPerPacket = 100;
			var bytesReceived = new ArraySegment<byte>(receiveBuffer, offset, dataPerPacket);
			var result = await ws.ReceiveAsync(bytesReceived, CancellationToken.None);
			
			Debug.Log(result.CloseStatusDescription);
			Assert.IsTrue(result.EndOfMessage);
			Assert.IsTrue(result.CloseStatus == WebSocketCloseStatus.PolicyViolation);
		}

		[Test]
		public async Task SendConvertMessage_StatusNormalClosure()
		{
			var ws = await GetClient(VoiceId);
			
			Assert.NotNull(ws);
			Assert.IsTrue(ws.State == WebSocketState.Open);
			
			var authCommand = AudioStreamingClientBase.GetAuthCommand(_configuration.ApiKey, _configuration.ApiClient);
			
			var bytesToSendAuth = new ArraySegment<byte>(Encoding.UTF8.GetBytes(authCommand));
			await ws.SendAsync(bytesToSendAuth, WebSocketMessageType.Text, true, CancellationToken.None);
			
			var convertCommand = AudioStreamingClientBase.GetConvertCommand(Text);
			var bytesToSendConvert = new ArraySegment<byte>(Encoding.UTF8.GetBytes(convertCommand));
			await ws.SendAsync(bytesToSendConvert, WebSocketMessageType.Text, true, CancellationToken.None);
			
			Assert.IsTrue(ws.State == WebSocketState.Open);
			
			var receiveBuffer = new byte[1024];
			var total = 0;
			var dataPerPacket = receiveBuffer.Length;
			WebSocketReceiveResult result = null;
			
			while (result?.CloseStatus == null)
			{
				var bytesReceived = new ArraySegment<byte>(receiveBuffer, 0, dataPerPacket);
				result = await ws.ReceiveAsync(bytesReceived, CancellationToken.None);
				total += result.Count;
			}
			
			Debug.Log(result.CloseStatusDescription);
			Assert.GreaterOrEqual(total,1);
			Assert.IsTrue(result.EndOfMessage);
			Assert.IsTrue(result.CloseStatus == WebSocketCloseStatus.NormalClosure);
			Assert.AreEqual(219180,total);
			
			Debug.Log("Total:" + total);
			Debug.Log("Total time:" + total / 2f / 44100f);
		}

		[Test]
		public async Task SendConvertMessage_Wrapper_StatusNormalClosure()
		{
			var bytesCount = 0;
			var closeStatus = string.Empty;
			var authCommand = AudioStreamingClientBase.GetAuthCommand(_configuration.ApiKey, _configuration.ApiClient);
			var convertCommand = AudioStreamingClientBase.GetConvertCommand(Text);
			
			var w = new NativeSocketWrapper(AudioStreamingManager.URL + $"?voiceId={VoiceId}");
			
			w.OnData += bytes =>
			{
				bytesCount += bytes.Length;
				Debug.Log($"OnData: {bytes.Length}/{bytesCount}");
			};
			
			w.OnOpen += () =>
			{
				w.SendText(authCommand);
				w.SendText(convertCommand);
			};
			
			w.OnClose += s =>
			{
				closeStatus = s;
				Debug.Log("Close: "+ closeStatus);
			};
			
			w.OnError += s =>
			{
				closeStatus = s;
				Debug.LogError("Error: " + closeStatus);
			};
			
			w.Connect();
			await Task.Delay(3000);
			Assert.IsTrue(w.Status == WebSocketState.CloseReceived);
			Assert.AreEqual(219180, bytesCount);
		}

		[Test]
		public async Task SendConvertMessageLoop_StatusNormalClosure()
		{
			var authCommand = AudioStreamingClientBase.GetAuthCommand(_configuration.ApiKey, _configuration.ApiClient);
			var convertCommand = AudioStreamingClientBase.GetConvertCommand(Text);

			for (int i = 0; i < 10; i++)
			{
				var bytesCount = 0;
				var start =  DateTimeOffset.Now;
				
				var w = new NativeSocketWrapper(AudioStreamingManager.URL + $"?voiceId={181}");
				
				w.OnData += bytes =>
				{
					bytesCount += bytes.Length;
					Debug.Log($"OnData: {bytes.Length}/{bytesCount}");
				};
			
				w.OnOpen += () =>
				{
					w.SendText(authCommand);
					w.SendText(convertCommand);
				};
				
				w.OnClose += (s) =>
				{
					var end = DateTimeOffset.Now;
					Debug.Log($"Iteration closed: {i} Message: {s} {(end - start).Milliseconds}ms");
				};
				
				w.OnError += Debug.LogError;
				
				w.Connect();
				await Task.Delay(750);
				Assert.AreEqual(WebSocketState.CloseReceived,w.Status);
				Assert.AreEqual(219180, bytesCount);
			}
		}
	}
}