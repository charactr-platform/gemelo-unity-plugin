using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Gemelo.Voice.Streaming;
using UnityEngine;

namespace Gemelo.Voice.Tests
{
	internal class AudioStream
		{
			public MemoryStream Stream { get; }
			public int Length { get => _bytesCount; }
			public WebSocketState State { get => _socket.Status; }
			
			private const string Text = "Hello from Charactr Software Development Kit for Unity";
			private const int VoiceId = 151;
			
			private readonly List<string> _commands = new List<string>();
			private readonly NativeSocketWrapper _socket;
			private int _bytesCount;
			private string _closeStatus;
			public AudioStream(Configuration configuration, bool useMp3 = false)
			{
				Stream = new MemoryStream();
				
				_commands.Add(AudioStreamingClientBase.GetAuthCommand(configuration.ApiKey, configuration.ApiClient));
				_commands.Add(AudioStreamingClientBase.GetConvertCommand(Text));
				
				var mp3 = useMp3 ? "&format=mp3&sr=44100" : string.Empty;
				_socket = new NativeSocketWrapper(Configuration.STREAMING_API + $"?voiceId={VoiceId}" + mp3);
			}

			public void Connect()
			{
				_socket.OnData += bytes =>
				{
					_bytesCount += bytes.Length;
					Stream.Write(bytes);
					Debug.Log($"OnData: {bytes.Length}/{_bytesCount}");
				};
			
				_socket.OnOpen += () =>
				{
					foreach (var command in _commands)
					{
						_socket.SendText(command);
					}
				};
			
				_socket.OnClose += s =>
				{
					_closeStatus = s;
					Debug.Log("Close: "+ _closeStatus);
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
		}
}