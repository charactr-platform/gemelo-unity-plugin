using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Gemelo.Voice.Tests
{
	public class PcmFrame
	{
		private const int SamplesCount = 4096;
		
		[Test]
		public void NewFrame_Returns_EmptyFrame()
		{
			var frameOne = new Audio.PcmFrame(SamplesCount);
			
			Assert.NotNull(frameOne);
			Assert.AreEqual(SamplesCount, frameOne.SamplesSize);
			Assert.IsNotNull(frameOne.Samples);
			Assert.IsFalse(frameOne.HasData);
			Assert.AreEqual(0, frameOne.Samples.Length);
		}
		
		[Test]
		public void AddPcmData_Bytes_Returns_Samples_HalfFull()
		{
			var dataSize = SamplesCount;
			
			var frameOne = new Audio.PcmFrame(SamplesCount);
			
			var array = new ArraySegment<byte>(new byte[dataSize]);
			Assert.AreEqual(dataSize, array.Count);

			Assert.IsFalse(frameOne.AddPcmData(array, out var overflow));
			Assert.IsNull(overflow);
			
			Assert.IsNotEmpty(frameOne.Samples);
			Assert.AreEqual(dataSize / 2, frameOne.Samples.Length);
		}
		
		[Test]
		public void AddPcmData_Floats_Returns_Samples_HalfFull()
		{
			var dataSize = SamplesCount / 2;
			
			var frameOne = new Audio.PcmFrame(SamplesCount);
			
			var array = new ArraySegment<float>(new float[dataSize]);
			Assert.AreEqual(dataSize, array.Count);

			Assert.IsFalse(frameOne.AddPcmData(array, out var overflow));
			Assert.IsNull(overflow);
			
			Assert.IsNotEmpty(frameOne.Samples);
			Assert.AreEqual(dataSize, frameOne.Samples.Length);
		}
		
		[Test]
		public void AddPcmData_Floats_Returns_Samples_Overflow()
		{
			var dataSize = SamplesCount + 1;
			
			var frameOne = new Audio.PcmFrame(SamplesCount);
			
			var array = new ArraySegment<float>(new float[dataSize]);
			Assert.AreEqual(dataSize, array.Count);

			Assert.IsTrue(frameOne.AddPcmData(array, out var overflow));
			Assert.IsNotNull(overflow);
			Assert.AreEqual(1, overflow.Length);
			
			Assert.IsNotEmpty(frameOne.Samples);
			Assert.AreEqual(SamplesCount, frameOne.Samples.Length);
		}
		
		[Test]
		public void AddPcmData_Bytes_Returns_Samples_Overflow()
		{
			var dataSize = (SamplesCount + 1) * Audio.PcmFrame.BlockSize16; //Int16 = 2 bytes
			
			var frameOne = new Audio.PcmFrame(SamplesCount);
			
			var array = new ArraySegment<byte>(new byte[dataSize]);
			Assert.AreEqual(dataSize, array.Count);

			Assert.IsTrue(frameOne.AddPcmData(array, out var overflow));
			Assert.IsNotNull(overflow);
			Assert.AreEqual(2, overflow.Length);
			
			Assert.IsNotEmpty(frameOne.Samples);
			Assert.AreEqual(SamplesCount, frameOne.Samples.Length);
		}

		
		private Queue<Audio.PcmFrame> _queue;
		private Audio.PcmFrame _currentFrame;
		
		[Test]
		public void AddPcmData_Bytes_Returns_10_Frames()
		{
			_queue = new Queue<Audio.PcmFrame>();
			
			var count = 0;
			var byteSize = SamplesCount * Audio.PcmFrame.BlockSize16;//Int16
			var dataSize = byteSize * 11 - 1; 
			
			_currentFrame = new Audio.PcmFrame(0.ToString(), SamplesCount);
			
			var array = new ArraySegment<byte>(new byte[dataSize]);
			Assert.AreEqual(dataSize, array.Count);

			var frames = WritePcmFrames(array.ToArray(), ref count);
			Assert.AreEqual(0, _queue.Count);
			
			Assert.IsNotEmpty(frames);
			
			//Current frame is still active
			Assert.AreEqual(10, frames.Count);
			Assert.IsTrue(_currentFrame.HasData);
			Assert.AreEqual(SamplesCount - 1, _currentFrame.Samples.Length);
		}
		
		private List<Audio.PcmFrame> WritePcmFrames(byte[] samples, ref int count)
		{
			if (!_currentFrame.AddPcmData(samples, out var overflow))
				return DequeueLastFrames();

			_queue.Enqueue(_currentFrame);
			
			_currentFrame = new Audio.PcmFrame((++count).ToString(), SamplesCount);
			
			return WritePcmFrames(overflow, ref count);
		}
		
		private List<Audio.PcmFrame> DequeueLastFrames()
		{
			var list = new List<Audio.PcmFrame>();
			
			var queueCount = _queue.Count;
			
			for (int i = 0; i < queueCount; i++)
			{
				if (_queue.TryDequeue(out var frame))
					list.Add(frame);
			}
			
			return list;
		}
	}
}