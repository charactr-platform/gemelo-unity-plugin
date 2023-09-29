using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Rest.Model;
using Gemelo.Voice.Tests.Preview;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PropertyAttribute = NUnit.Framework.PropertyAttribute;

namespace Gemelo.Voice.Tests
{
    public class VoicePreviewDatabase: TestBase
    {
        private const int PREVIEW_SAMPLE_RATE = 32000;
        private const int HEADER_SIZE = 76;
        private const string DATABASE_FILE = "VoicePreviewDatabase";
        [Test]
        public async Task GetVoicesRequest_Returns_NotEmpty()
        {
            var voices = await GetVoicesResponse();
            Assert.NotNull(voices);
            Assert.IsNotEmpty(voices.Data);
            Assert.NotNull(voices.Data.First());
        }

        private async Task<VoicesResponse> GetVoicesResponse()
        {
            return await EditorHttp.GetAsync<VoicesResponse>(Configuration.VOICES_API);
        }
        
        [Test]
        public async Task Load_Voice_Preview_ToWavBuilder_NotNull()
        {
            var data = await GetVoicesResponse();
            Assert.NotNull(data);
            var item = data.Data.First();
            Assert.NotNull(item);
            var buffer = await EditorHttp.GetDataAsync(item.PreviewUrl);
            Assert.IsNotEmpty(buffer);

            var headerBuffer = buffer.AsSpan(0, HEADER_SIZE).ToArray();
            var header = new WavHeaderData(headerBuffer);
            Assert.GreaterOrEqual(PREVIEW_SAMPLE_RATE,header.SampleRate);
            Assert.AreEqual(true, header.IsExtensibeWav);
            Assert.AreEqual(1, header.Channels);
            Assert.AreEqual(16, header.BitDepth);
            Assert.AreEqual(HEADER_SIZE, header.DataOffset);
            
            var builder = new WavBuilder(PREVIEW_SAMPLE_RATE, headerBuffer);
            Assert.NotNull(builder);
            var frames = builder.ToPcmFrames(buffer.AsSpan(HEADER_SIZE).ToArray());
            Assert.IsNotEmpty(frames);
        }

        [Test]
        public async Task Load_AllVoices_Header_SampleRate_Equals32000()
        {
            var data = await GetVoicesResponse();
            Assert.NotNull(data);
            var tasks = new List<Task<bool>>();
            
            for (var index = 0; index < data.Count; index++)
            {
                var voice = data[index];
                Assert.NotNull(voice);

                if (string.IsNullOrEmpty(voice.PreviewUrl))
                {
                    Debug.LogWarning($"No preview: voice index: {index}, {voice.Name}");
                    continue;
                }

                Debug.Log($"Voice index: {index}, {voice.Name}");
                
                var t = ValidateHeaderData(voice);
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
            Debug.Log("Finished");
        }

        [Test]
        public async Task VoicePreview_NotNull()
        {
            var data = await GetVoicesResponse();
            var item = data.Data.First();
            var preview = await CreateVoicePreviewFromVoiceItem(item);
            Assert.NotNull(preview);
        }
        
        [Test]
        public async Task VoicePreview_AudioClip_NotNull()
        {
            var data = await GetVoicesResponse();
            var item = data.Data.First();
            var preview = await CreateVoicePreviewFromVoiceItem(item);
            Assert.NotNull(preview);
            var clip = preview.GenerateAudioClip();
            Assert.NotNull(clip);
            await AudioPlayer.PlayClipStatic(clip);
        }
        
        private async Task<VoicePreview> CreateVoicePreviewFromVoiceItem(VoicePreviewItem item)
        {
            Assert.IsNotEmpty(item.PreviewUrl);
            var buffer = await VoicePreview.GetAudioPreviewData(item.PreviewUrl);
            Assert.IsNotEmpty(buffer);

            var preview = new VoicePreview(item);
            Assert.NotNull(preview);
            Assert.NotZero(preview.WriteAudioFrames(buffer));
            var size = preview.ConvertToBinary();
            return preview;
        }
        private async Task<bool> ValidateHeaderData(VoicePreviewItem item)
        {
            Assert.IsNotEmpty(item.PreviewUrl);
            
            var buffer = await VoicePreview.GetAudioPreviewData(item.PreviewUrl);
            var header = new WavHeaderData(buffer.AsSpan(0, HEADER_SIZE).ToArray());
            Assert.GreaterOrEqual(header.SampleRate, PREVIEW_SAMPLE_RATE);
            Assert.GreaterOrEqual(header.DataOffset, WavBuilder.HeaderSize);
            Assert.AreEqual(1, header.Channels);
            Debug.Log($"Header: {header.SampleRate}, bit:{header.BitDepth} offset:{header.DataOffset}");
            return true;
        }
        
        [Test]
        public void Create_VoicesPreviewDb_Instance_NotNull()
        {
            var instance = VoicesDatabase.CreateInstance();
            Assert.NotNull(instance);
            Assert.IsInstanceOf<VoicesDatabase>(instance);
            Assert.IsNull(instance.Voices);
        }

        [Test]
        public async Task Save_VoicePreviewDatabase_PcmData_Result_True()
        {
            var instance = VoicesDatabase.CreateInstance();
            Assert.NotNull(instance);
            var data = await GetVoicesResponse();
            var item = data.Data.First();
            var result = await instance.AddVoicePreview(item);
            Assert.IsTrue(result);

            var path = Configuration.GLOBAL_SAVE_PATH + DATABASE_FILE + ".asset";
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssetIfDirty(instance);
            AssetDatabase.ImportAsset(path);
            var database = AssetDatabase.LoadAssetAtPath<VoicesDatabase>(path);
            Assert.NotNull(database);
        }

        [Test]
        public async Task Load_VoicePreviewDatabase_VoicePreview_PcmData_NotNull()
        {
            var database = Resources.Load<VoicesDatabase>(DATABASE_FILE);
            Assert.NotNull(database);
            Assert.IsInstanceOf<VoicesDatabase>(database);
            var preview = database.GetVoicePreviewByName("Ivan");
            await AudioPlayer.PlayClipStatic(preview.GenerateAudioClip());
        }
    }

    public class VoicesListAttribute: PropertyAttribute {
    
    }
}