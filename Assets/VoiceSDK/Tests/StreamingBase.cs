using System;
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
	public class StreamingTestsData
	{
		protected const string Text = "Hello from Charactr Software Development Kit for Unity";

		protected const string LongText =
			"We use optional cookies to improve your experience on our websites, such as through social media connections," +
			" and to display personalized advertising based on your online activity. If you reject optional cookies," +
			" only cookies necessary to provide you the services will be used." +
			" You may change your selection by clicking “Manage Cookies” at the bottom of the page.";

		protected const int VoiceId = 151;
	}
	
	public class StreamingBase: StreamingTestsData
	{
		private const int ByteSize = 247084; //259672
		private const int Samples = 129814;
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

			var receiveBuffer = new byte[128];
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
			Assert.GreaterOrEqual(total, 1);
			Assert.IsTrue(result.EndOfMessage);
			Assert.IsTrue(result.CloseStatus == WebSocketCloseStatus.NormalClosure);
			Assert.AreEqual(ByteSize, total);

			Debug.Log("Total:" + total);
			Debug.Log("Total time:" + total / 2f / 44100f);
		}

		[Test]
		public async Task SendConvertMessage_AudioStreamWrapper_StatusNormalClosure()
		{
			var s = new AudioStream(_configuration, AudioDataType.Mp3, 44100);

			s.Connect();
			await s.WaitForData(ByteSize);
			s.Close();

			Assert.IsTrue(s.State == WebSocketState.Closed);
			Assert.AreEqual(ByteSize, s.Stream.Length);

			var buffer = s.Stream.GetBuffer();
			var header = new WavHeaderData(buffer);
			Assert.AreEqual(44100, header.SampleRate);

			var wav = new WavBuilder(header.SampleRate, buffer);
			var clip = wav.CreateAudioClip();
			Debug.Log(clip.samples);

			Assert.AreEqual(s.Length, s.Stream.Length);
			Assert.AreEqual(ByteSize, s.Length);
			Assert.AreEqual(clip.samples, (buffer.Length - header.DataOffset) / sizeof(short));
			await AudioPlayer.PlayClipStatic(clip);
		}

		[Test]
		public async Task SendConvertMessage_Loop_StatusNormalClosure()
		{
			var authCommand = AudioStreamingClientBase.GetAuthCommand(_configuration.ApiKey, _configuration.ApiClient);
			var convertCommand = AudioStreamingClientBase.GetConvertCommand(Text);

			for (int i = 0; i < 10; i++)
			{
				var bytesCount = 0;
				var start = DateTimeOffset.Now;

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

				w.OnClose += (s) =>
				{
					var end = DateTimeOffset.Now;
					Debug.Log($"Iteration closed: {i} Message: {s} {(end - start).Milliseconds}ms");
				};

				w.OnError += Debug.LogError;

				w.Connect();
				await Task.Delay(Timeout);
				Assert.AreEqual(WebSocketState.Closed, w.Status);
				Assert.AreEqual(ByteSize, bytesCount);
			}
		}

		[Test]
		public async Task SendConvertMessage_AudioStreamWrapper_Returns_Mp3Stream()
		{
			var readSize = 8192;
			var expectedSamples = 171648;
			var audio = new AudioStream(_configuration, AudioDataType.Mp3, 44100);

			/*audio.Connect(LongText);*/

			audio.Connect();

			MpegFile mp3 = null;

			await audio.WaitForData(readSize);

			var mp3Stream = new MemoryStream();
			long readPosition = audio.CopyTo(mp3Stream, 0);

			mp3 = new MpegFile(audio.Stream);
			Assert.AreEqual(1, mp3.Channels);
			Assert.AreEqual(audio.SampleRate, mp3.SampleRate);

			var pcmSamples = new float[expectedSamples];

			var index = 0;
			var pcmWriteCount = 0;

			void PcmCallback(float[] buffer)
			{
				var pcmDataSize = buffer.Length;
				
				if (readPosition < audio.Stream.Length)
				{
					readPosition += audio.CopyTo(mp3Stream, readPosition);
					Debug.Log($"OnCopy: {readPosition}/{audio.Stream.Length}");
				}
				
				var pcmReadCount = mp3.ReadSamples(pcmSamples , index, pcmDataSize);

				Debug.Log($"Samples Eof: {mp3.Reader.EndOfStream} ReadCount: {pcmReadCount}, Samples: {index} / {expectedSamples}");

				index += pcmReadCount;

				for (int i = 0; i < pcmDataSize; i++)
				{
					buffer[i] = pcmSamples[pcmWriteCount + i];
				}

				pcmWriteCount += pcmDataSize;
			}

			var clip = AudioClip.Create("name", expectedSamples, 1, audio.SampleRate, true, PcmCallback);

			await AudioPlayer.PlayClipStatic(clip);

			Assert.AreEqual(31181, audio.Stream.Length);
			Assert.AreEqual(31181, readPosition);
			Assert.AreEqual(mp3.Position, mp3.Length);
			Assert.AreEqual(expectedSamples, pcmWriteCount);
			Assert.AreEqual(clip.samples, pcmWriteCount);
		}
	}
}