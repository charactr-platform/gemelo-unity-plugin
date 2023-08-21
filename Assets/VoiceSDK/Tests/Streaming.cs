﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Streaming;
using NLayer;
using NUnit.Framework;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace Gemelo.Voice.Tests
{
	public class Streaming
	{
		private const string Text = "Hello from Charactr Software Development Kit for Unity";
		private const int VoiceId = 151;
		private const int ByteSize = 247084;
		private const int Timeout = 3000;
		private async Task<ClientWebSocket> GetClient(int voiceId = 151)
		{
			var ws = new ClientWebSocket();
			ws.Options.SetRequestHeader("user-agent", Configuration.USER_AGENT);
			
			var serverUri = new Uri(Configuration.STREAMING_API + $"?voiceId={voiceId}");
			var source = new CancellationTokenSource();
			source.CancelAfter(Timeout);

			await ws.ConnectAsync(serverUri, source.Token);
			return ws;
		}

		private Configuration _configuration;
		
		[SetUp]
		public void Setup()
		{
			_configuration = Configuration.Load();
			
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
		public async Task SendAuthMessage_WrongVoiceId_StatusPolicyViolation()
		{
			var ws = await GetClient(1);
			
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
			Assert.AreEqual(ByteSize,total);
			
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
			
			var w = new NativeSocketWrapper(Configuration.STREAMING_API + $"?voiceId={VoiceId}");
			
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
			await Task.Delay(Timeout);
			Assert.IsTrue(w.Status == WebSocketState.Closed);
			Assert.AreEqual(ByteSize , bytesCount);
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
				
				var w = new NativeSocketWrapper(Configuration.STREAMING_API+ $"?voiceId={VoiceId}");
				
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
				await Task.Delay(Timeout);
				Assert.AreEqual(WebSocketState.Closed,w.Status);
				Assert.AreEqual(ByteSize , bytesCount);
			}
		}
		
		[Test]
		public async Task SendConvertMessage_Wrapper_Returns_Mp3()
		{
			var bytesCount = 0;
			var closeStatus = string.Empty;

			var memory = new MemoryStream();
	
			
			var authCommand = AudioStreamingClientBase.GetAuthCommand(_configuration.ApiKey, _configuration.ApiClient);
			var convertCommand = AudioStreamingClientBase.GetConvertCommand(Text);
			
			var w = new NativeSocketWrapper(Configuration.STREAMING_API + $"?voiceId={VoiceId}&format=mp3&sr=44100");

			MpegFile mp3 = null;
			int pcmSamplesRead = 0;
			
			w.OnData += bytes =>
			{
				bytesCount += bytes.Length;
				Debug.Log($"OnData: {bytes.Length}/{bytesCount}");
				memory.Write(bytes);
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
			await Task.Delay((int)(Timeout*1.5f));
			Assert.IsTrue(w.Status == WebSocketState.Closed);
			
			
			mp3 = new MpegFile(memory);
			Assert.AreEqual(1, mp3.Channels);

			var readSize = 1024;
			var samples = mp3.Length / mp3.Channels / sizeof(float);
			
			//BUG: ReadSamples checks buffer size with read size together
			var pcmSamples = new float[samples + readSize];

			var index = 0;
			
			while (mp3.Position < mp3.Length)
			{
				var count = mp3.ReadSamples(pcmSamples, index, readSize);
				pcmSamplesRead += count;
				Debug.Log($"PCM length:{count}/{pcmSamplesRead}, position: {mp3.Position}");
				index = pcmSamplesRead;
			}
			
			var clip = AudioClip.Create("name", pcmSamplesRead, mp3.Channels, mp3.SampleRate, false);
			clip.SetData(pcmSamples.AsSpan(0, pcmSamplesRead).ToArray(),0);
			clip.LoadAudioData();
			Assert.AreEqual(pcmSamplesRead, clip.samples);
			await AudioPlayer.PlayClipStatic(clip);
		}
	}
}