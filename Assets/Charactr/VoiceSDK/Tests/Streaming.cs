using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Streaming;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Charactr.VoiceSDK.Tests
{
	public class Streaming
	{
		private const string Text = "Hello from Charactr Software Development Kit for Unity";
		private async Task<ClientWebSocket> GetClient(int voiceId = 151)
		{
			var ws = new ClientWebSocket();
			var serverUri = new Uri(AudioStreamingManager.URL + $"?voiceId={voiceId}");
			var source = new CancellationTokenSource();
			source.CancelAfter(5000);

			await ws.ConnectAsync(serverUri, source.Token);
			return ws;
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
			var config = Configuration.LoadStreaming();
			Assert.NotNull(config);
			Assert.IsNotEmpty(config.ApiClient);
			Assert.IsNotEmpty(config.ApiKey);
			
			var ws = await GetClient(151);
			
			Assert.NotNull(ws);
			Assert.IsTrue(ws.State == WebSocketState.Open);
			
			var authCommand = AudioStreamingClientBase.GetAuthCommand(config.ApiKey, config.ApiClient);
			
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
			var config = Configuration.LoadStreaming();
			Assert.NotNull(config);
			Assert.IsNotEmpty(config.ApiClient);
			Assert.IsNotEmpty(config.ApiKey);
			
			var ws = await GetClient(112);
			
			Assert.NotNull(ws);
			Assert.IsTrue(ws.State == WebSocketState.Open);
			
			var authCommand = AudioStreamingClientBase.GetAuthCommand(config.ApiKey, config.ApiClient);
			
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
			Debug.Log("Total:" + total);
			Debug.Log("Total time:" + total / 2f / 44100f);
		}
	}
}