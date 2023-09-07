using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Streaming;
using UnityEngine;

namespace Gemelo.Voice.Tests
{
	internal class AudioStream
	{
		public MemoryStream Stream { get; }

		public int SampleRate { get; }
		public int Length
		{
			get => _bytesCount;
		}

		public WebSocketState State
		{
			get => _socket.Status;
		}

		private const string Text = "Hello from Charactr Software Development Kit for Unity";
		private const int VoiceId = 151;

		private readonly List<string> _commands = new List<string>();
		private readonly NativeSocketWrapper _socket;
		private int _bytesCount;
		private string _closeStatus;
		private readonly BinaryWriter _writer;

		public AudioStream(Configuration configuration, AudioDataType dataType, int sampleRate)
		{
			SampleRate = sampleRate;
			Stream = new MemoryStream();
			_writer = new BinaryWriter(Stream);
			_commands.Add(AudioStreamingClientBase.GetAuthCommand(configuration.ApiKey, configuration.ApiClient));
			_commands.Add(AudioStreamingClientBase.GetConvertCommand(Text));

			var mp3Params = dataType == AudioDataType.Mp3 ? $"&format=mp3&sr={sampleRate}" : string.Empty;
			_socket = new NativeSocketWrapper(Configuration.STREAMING_API + $"?voiceId={VoiceId}" + mp3Params);
		}

		public void Connect(string text = "")
		{
			_socket.OnData += bytes =>
			{
				_bytesCount += bytes.Length;
				_writer.Write(bytes);
				_writer.Flush();
				Debug.Log("Ondata: " + Length);
			};

			_socket.OnOpen += () =>
			{
				if (!string.IsNullOrEmpty(text))
					_commands[1] = AudioStreamingClientBase.GetConvertCommand(text);

				foreach (var command in _commands)
				{
					_socket.SendText(command);
				}
			};

			_socket.OnClose += s =>
			{
				_closeStatus = s;
				var end = _writer.Seek(0, SeekOrigin.End);
				Debug.Log($"Data received: {Stream.Length}/{end} Close: " + _closeStatus);
			};

			_socket.OnError += s =>
			{
				_closeStatus = s;
				Debug.LogError("Error: " + _closeStatus);
			};

			_socket.Connect();
		}

		public Task WaitForData(int size)
		{
			return Task.Run(() =>
			{
				while (Stream.Length < size)
				{
					Task.Delay(100);
				}
			});
		}

		public Task WaitForClose()
		{
			return Task.Run(() =>
			{
				while (_socket.Status != WebSocketState.Closed)
				{
					Task.Delay(100);
				}
			});
		}

		public void Close()
		{
			_socket.Close();
		}

		public int CopyTo(Stream stream, long offset)
		{
			byte[] buffer = new byte[128];

			int bytesRead = 0, totalRead = 0;

			Stream.Seek(offset, SeekOrigin.Begin);
			stream.Seek(offset, SeekOrigin.Begin);

			while ((bytesRead = Stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				stream.Write(buffer, 0, bytesRead);
				totalRead += bytesRead;
			}
			stream.Flush();
			return totalRead;
		}
	}
}