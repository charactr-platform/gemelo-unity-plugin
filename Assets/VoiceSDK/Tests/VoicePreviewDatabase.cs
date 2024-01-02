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
using Debug = UnityEngine.Debug;

namespace Gemelo.Voice.Tests
{
    public class VoicePreviewDatabase: TestBase
    {
        private const int PREVIEW_SAMPLE_RATE = 32000;
        private const int HEADER_SIZE = 44;
        
        private async Task<IVoicesResponse> GetSystemVoicesResponse() =>
            await EditorHttp.GetAsync<SystemVoicesResponse>(Configuration.VOICES_API);

        private async Task<IVoicesResponse> GetAllVoicesResponse()
        {
            var cloned = await EditorHttp.GetAsync<ClonedVoicesResponse>(Configuration.CLONED_API);
            var system = await EditorHttp.GetAsync<SystemVoicesResponse>(Configuration.VOICES_API);
            var list = new List<IVoicePreviewItem>();
            list.AddRange(cloned.Items);
            list.AddRange(system.Items);
            return new VoicesResponse(list);
        }
        
        [Test]
        public async Task Load_16bit_Voice_Preview_ToWavBuilder_NotNull()
        {
            var data = await GetSystemVoicesResponse();
            Assert.NotNull(data);
            var item = data.Items.First();
            Assert.NotNull(item);
            var buffer = await EditorHttp.GetDataAsync(item.Url);
            Assert.IsNotEmpty(buffer);

            var headerBuffer = buffer.AsSpan(0, HEADER_SIZE).ToArray();
            var header = new WavHeaderData(headerBuffer);
            Assert.GreaterOrEqual(PREVIEW_SAMPLE_RATE,header.SampleRate);
            Assert.AreEqual(false, header.IsExtensibeWav);
            Assert.AreEqual(1, header.Channels);
            Assert.AreEqual(16, header.BitDepth);
            Assert.AreEqual(HEADER_SIZE, header.DataOffset);
            
            var builder = new WavBuilder(PREVIEW_SAMPLE_RATE, header.BitDepth, headerBuffer);
            Assert.NotNull(builder);
            Assert.AreEqual(16,builder.BitDepth);
            var frames = builder.ToPcmFrames(buffer.AsSpan(HEADER_SIZE).ToArray());
            Assert.IsNotEmpty(frames);
            Assert.AreEqual(16,frames[0].BitDepth);
        }

        [Test]
        public async Task Load_AllVoices_Header_SampleRate_Equals32000()
        {
            var response = await GetAllVoicesResponse();
            
            Assert.NotNull(response);
            
            var tasks = new List<Task<bool>>();

            var items = response.Items.ToArray();
            
            for (var index = 0; index < items.Length; index++)
            {
                var voice = items[index];
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
            var data = await GetSystemVoicesResponse();
            var item = data.Items.First();
            var preview = await CreateVoicePreviewFromVoiceItem(item);
            Assert.NotNull(preview);
        }
        
        [Test]
        public async Task VoicePreview_AudioClip_NotNull()
        {
            var data = await GetSystemVoicesResponse();
            var item = data.Items.First();
            var preview = await CreateVoicePreviewFromVoiceItem(item);
            Assert.NotNull(preview);
            var clip = preview.GenerateAudioClip();
            Assert.NotNull(clip);
            await AudioPlayer.PlayClipStatic(clip);
        }
        
        private async Task<VoicePreview> CreateVoicePreviewFromVoiceItem(IVoicePreviewItem item)
        {
            Assert.IsNotEmpty(item.Url);
            var buffer = await VoicePreview.GetAudioPreviewData(item.Url);
            Assert.IsNotEmpty(buffer);

            var preview = new VoicePreview(item);
            Assert.NotNull(preview);
            var data = preview.WriteAudioFrames(buffer);
            var size = preview.EncodePcmFramesToCache(data, out var fileName);
            Assert.IsNotEmpty(fileName);
            Assert.NotZero(size);
            return preview;
        }
        
        private async Task<bool> ValidateHeaderData(IVoicePreviewItem item)
        {
            Assert.IsNotEmpty(item.Url);
            
            var buffer = await VoicePreview.GetAudioPreviewData(item.Url);
            
            if (buffer == null)
            {
                Debug.Log($"DownloadError: {item.Id} - {item.Name} - {item.Url}");
                return false;
            }
            
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
            var data = await GetSystemVoicesResponse();
            var item = data.Items.First();
          
            var result = await instance.AddVoicePreview(item, 
                new Progress<float>((s)=> Debug.Log("Progress: " + s)));
            
            Assert.IsTrue(result);
            var path = Configuration.GLOBAL_SAVE_PATH + VoicesDatabase.FILE_ASSET + ".asset";
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssetIfDirty(instance);
            AssetDatabase.ImportAsset(path);
            var database = AssetDatabase.LoadAssetAtPath<VoicesDatabase>(path);
            Assert.NotNull(database);
            Assert.IsTrue(database.GetVoicePreviewById(item.Id, out var preview));
            Assert.IsTrue(preview.DecodeCacheDataToPcmFrames(out var frames));
            Assert.IsNotEmpty(frames);
        }

        [Test]
        public async Task Update_VoicesPreviewsDatabase_Save_Result_True()
        {
            var instance = VoicesDatabase.CreateInstance();
            Assert.NotNull(instance);
            
            var progress = 0f;
            
            var result = await instance.UpdatePreviewsDatabase(new Progress<float>((p) =>
            {
                progress = p;
                Debug.Log($"Progress {progress}");
            }));
            
            Assert.IsTrue(result.All(a=> a));
            Assert.GreaterOrEqual(progress, 0.99f);
            
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
            Assert.IsTrue(database.GetBestVoicePreview(out var preview));
            Assert.IsTrue(await preview.FetchVoicePreviewData());
            Assert.IsTrue(preview.DecodeCacheDataToPcmFrames(out var frames));
            Assert.IsNotEmpty(frames);
            var audioClip = preview.GenerateAudioClip(frames);
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
}