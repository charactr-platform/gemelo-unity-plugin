using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Editor.Preview;
using Gemelo.Voice.Rest.Model;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using PropertyAttribute = NUnit.Framework.PropertyAttribute;

namespace Gemelo.Voice.Tests
{
    public class VoicePreviewDatabase: TestBase
    {
        private const int PREVIEW_SAMPLE_RATE = 32000;
        private const int HEADER_SIZE = 76;
        
        [Test]
        public async Task GetVoicesRequest_Returns_NotEmpty()
        {
            var voices = await GetVoicesResponse();
            Assert.NotNull(voices);
            Assert.IsNotEmpty(voices.Data);
            Assert.NotNull(voices.Data.First());
        }

        private async Task<VoicesResponse> GetVoicesResponse(bool all = true)
        {
            var url = Configuration.VOICES_API + (all ? "?show=all" : string.Empty);  
            return await EditorHttp.GetAsync<VoicesResponse>(url);
        }
        
        [Test]
        public async Task Load_Voice_Preview_ToWavBuilder_NotNull()
        {
            var data = await GetVoicesResponse();
            Assert.NotNull(data);
            var item = data.Data.First();
            Assert.NotNull(item);
            var buffer = await EditorHttp.GetDataAsync(item.Url);
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

                if (string.IsNullOrEmpty(voice.Url))
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
            Assert.IsNotEmpty(item.Url);
            var buffer = await VoicePreview.GetAudioPreviewData(item.Url);
            Assert.IsNotEmpty(buffer);

            var preview = new VoicePreview(item);
            Assert.NotNull(preview);
            Assert.NotZero(preview.WriteAudioFrames(buffer));
            var size = preview.EncodePcmFramesToData(out var fileName);
            Assert.IsNotEmpty(fileName);
            Assert.NotZero(size);
            return preview;
        }
        
        private async Task<bool> ValidateHeaderData(VoicePreviewItem item)
        {
            Assert.IsNotEmpty(item.Url);
            
            var buffer = await VoicePreview.GetAudioPreviewData(item.Url);
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
            var path = Configuration.GLOBAL_SAVE_PATH + VoicesDatabase.FILE_ASSET + ".asset";
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssetIfDirty(instance);
            AssetDatabase.ImportAsset(path);
            var database = AssetDatabase.LoadAssetAtPath<VoicesDatabase>(path);
            Assert.NotNull(database);
            Assert.IsTrue(database.GetVoicePreviewById(item.Id, out var preview));
            Assert.NotZero(preview.DecodeCacheDataToPcmFrames(preview.CacheFileName));
        }

        [Test]
        public async Task Update_VoicesPreviewsDatabase_Save_Result_True()
        {
            var instance = VoicesDatabase.CreateInstance();
            Assert.NotNull(instance);
            var result= await instance.UpdatePreviewsDatabase();
            
            Assert.IsTrue(result.All(a=>a == true));
            
            var path = Configuration.GLOBAL_SAVE_PATH + VoicesDatabase.FILE_ASSET + ".asset";
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssetIfDirty(instance);
            AssetDatabase.ImportAsset(path);
            var database = AssetDatabase.LoadAssetAtPath<VoicesDatabase>(path);
            Assert.NotNull(database);
        }

        [Test]
        public async Task Load_VoicePreviewDatabase_VoicePreview_PcmData_NotNull()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var database = VoicesDatabase.Load();
            Assert.NotNull(database);
            Assert.IsInstanceOf<VoicesDatabase>(database);
            Assert.IsTrue(database.GetVoicePreviewByName("Will", out var preview));
            Assert.NotZero(preview.DecodeCacheDataToPcmFrames(preview.CacheFileName));
            var audioClip = preview.GenerateAudioClip();
            stopWatch.Stop();
            Debug.Log($"Time: {stopWatch.ElapsedMilliseconds}ms");
            await AudioPlayer.PlayClipStatic(audioClip);
        }

        [Test]
        public void Purge_CachePath_Files_NotEmpty()
        {
            var count = VoicesDatabase.PurgeCache();
            Assert.NotZero(count);
        }
    }

    public class VoicesListAttribute: PropertyAttribute {
    
    }
}