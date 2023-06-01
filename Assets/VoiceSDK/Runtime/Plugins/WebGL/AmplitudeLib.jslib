// JavaScript library for accessing amplitude audio from Web Audio API directly from the web browser
var AmplitudeLib = {
    /** Contains all the currently running analyzers */
    $analyzers: {},
    $chunksBuffer:{},
    
    WebGL_Dispose: function() {

        for (let index = 0; index <= WEBAudio.audioInstanceIdCounter; index++) {

            var object = WEBAudio.audioInstances[index];
            console.log(object)
            
	        if (object) {
                console.log("Releasing for GC: " + object)
                object.release()
	        }
	// Let the GC free up the audio object.
	        delete WEBAudio.audioInstances[index];
        }
        WEBAudio.audioContext.close().then(()=>console.log("Context released."))
    },

	WebGL_GetBufferInstanceOfLastAudioClip: function() {
        if (WEBAudio && WEBAudio.audioInstanceIdCounter) {
         for (let index = WEBAudio.audioInstanceIdCounter; index > 0; --index) {
             var object = WEBAudio.audioInstances[index];
             if (object.buffer) return index;       
            }
        }
        return -1;
    },
    
    WebGL_Stats: function() {
        for (let index = 0; index <= WEBAudio.audioInstanceIdCounter; index++) {
    
            var object = WEBAudio.audioInstances[index];
            console.dir(object)
        }
    },
   
    WebGL_FillBuffer: function (array, size, index) {

        var buffer = new Uint8Array(Module.HEAPU8.buffer, array, Float32Array.BYTES_PER_ELEMENT * size);

        if (typeof chunksBuffer.chunks == "undefined")
        {
            chunksBuffer.chunks = [];
            console.log("chunks ready")
        }
        
        var floatArray = new Float32Array(buffer.buffer, buffer.byteOffset, size);

        chunksBuffer.chunks[index] = floatArray;
    },
   
    /** Create an analyzer and connect it to the audio source
     * @param uniqueName Analyzer name
     * @param sampleSize Float array sample size
     * @returns true on success, false on failure */
    WebGL_StartSampling: function (uniqueName, bufferInstance, sampleSize, streaming) {
       
        var analyzerName = UTF8ToString(uniqueName);
        var analyzer = null;
        var audioInstance = null;
        var source = null;
        var stream = {chunks: chunksBuffer.chunks};
        var samplesPerUpdate = sampleSize;

        var count = WEBAudio.audioInstanceIdCounter;
        
        var ctx = WEBAudio.audioContext;

        if (count < 2)
        {
            console.log("AudioInstances must contains 2 or more items, count = " + count);
            return false;
        }
        
        //Channel - Audio Source instance
        var channelIndex = 1;
        for (let index = WEBAudio.audioInstanceIdCounter; index > 0; --index) {
                 var object = WEBAudio.audioInstances[index];
                 if (object.gain) {
                    channelIndex = index;
                    break;
                 }      
            }
      
        var sound = WEBAudio.audioInstances[bufferInstance];
        var channel = WEBAudio.audioInstances[channelIndex];
        
        if (!sound || !sound.buffer) {
            console.error("buffer instance is not an sound instance: "+bufferInstance); 
            console.dir(sound);
            return;
        }
        
        var source2 = ctx.createBufferSource();

        source2.buffer = sound.buffer;
       
        if (channel != null) {

            source2.disconnect();
            channel.source = source2;
            channel.gain.disconnect();
            
            channel.gain.connect(ctx.destination);
            
            audioInstance = channel;
            source = channel.source;
        }
        
        if (source == null)
            return false;

        stream.processorNode = ctx.createScriptProcessor(samplesPerUpdate, 1, 1);
        stream.currentBufferIndex = 0;
        stream.bufferPosition = 0;
        stream.buffer = chunksBuffer.chunks[0];

        stream.processorNode.onaudioprocess = function(event) {

            if (!streaming) {
                var outputArray = event.outputBuffer.getChannelData(0);
                var inputArray = event.inputBuffer.getChannelData(0);
                outputArray = inputArray;
                return;
            }


            var chunksLoaded = chunksBuffer.chunks.length - 1;
            var outputArray = event.outputBuffer.getChannelData(0);
            
            var bufferReady = chunksLoaded > stream.currentBufferIndex;
            
            if (!bufferReady)
            {
                outputArray = event.inputBuffer;
                //console.log("No more data...");
                return;
            }
        
            for (let sample = 0; sample < samplesPerUpdate; sample++) {

                //Be cafefull of empty chunks where length = 0
                if (stream.buffer.length > 0)
                    outputArray[sample] = stream.buffer[stream.bufferPosition + sample];
                else
                    outputArray[sample] = 0.0;
            }

            stream.bufferPosition += samplesPerUpdate;
            //console.log("Processing..." +stream.currentBufferIndex + " / "  + chunksLoaded + " ("+ stream.buffer.length + ")");
    
            //Set new chunk into buffer 
            if (stream.bufferPosition + samplesPerUpdate > stream.buffer.length)
            {
                stream.currentBufferIndex++;
                stream.buffer = chunksBuffer.chunks[stream.currentBufferIndex];
                stream.bufferPosition = 0;
            }
        }
        
        channel.source.connect(stream.processorNode);
        
        stream.processorNode.connect(audioInstance.gain);
        
        analyzer = ctx.createAnalyser();
            
        analyzer.fftSize = sampleSize;
        
        audioInstance.gain.connect(analyzer);
        
        analyzers[analyzerName] = { analyzer: analyzer, source: source, stream: stream };
        
        //source.onended = () => onAudioPlayed();

        source.start();
        return true;

    },
    
    /** Delete the analyzer
     * @param uniqueName Analyzer name
     * @returns true on success, false on failure */
    WebGL_StopSampling: function (uniqueName) {
        var analyzerName = UTF8ToString(uniqueName);
        var analyzerObj = analyzers[analyzerName];
        
        if (analyzerObj != null) {
            try {
                analyzerObj.source.disconnect();
                analyzerObj.stream.processorNode.disconnect();
                chunksBuffer.chunks.length = 0;
                delete analyzers[analyzerName];
                return true;
            }
            catch (e) {
                console.log("Failed to delete analyzer (" + analyzerName + ") from source " + e);
            }
        }
      
        return false;
    },

    /** Fill the sample array with amplitude data
     * @param uniqueName Analyzer name
     * @param sample Float array pointer to hold amplitude data
     * @param sampleSize Float array sample size
     * @returns true on success, false on failure */
    WebGL_GetAmplitude: function (uniqueName, sample, sampleSize) {
        try {
            var analyzerName = UTF8ToString(uniqueName);
            var analyzerObj = analyzers[analyzerName];
            var buffer = new Uint8Array(Module.HEAPU8.buffer, sample, Float32Array.BYTES_PER_ELEMENT * sampleSize);
            buffer = new Float32Array(buffer.buffer, buffer.byteOffset, sampleSize);

            if (analyzerObj == null) {
                console.log("Could not find analyzer (" + analyzerName + ")");
                return false;
            }

            analyzerObj.analyzer.getFloatTimeDomainData(buffer);
            return true;
        }
        catch (e) {
            console.log("Failed to get sample data " + e);
        }
        return false;
    },

    /** Fill the sample array with frequency data
     * @param uniqueName Analyzer name
     * @param sample Float array pointer to hold amplitude data
     * @param sampleSize Float array sample size
     * @returns true on success, false on failure */
    WebGL_GetFrequency: function (uniqueName, sample) {
        try {
             var analyzerName = UTF8ToString(uniqueName);
             var analyzerObj = analyzers[analyzerName];
             if (analyzerObj == null) {
               
                console.log("Could not find analyzer (" + analyzerName + ")");
                return false;
             }

            var bufferLength = analyzerObj.analyzer.frequencyBinCount;
            var buffer = new Uint8Array(Module.HEAPU8.buffer, sample, Float32Array.BYTES_PER_ELEMENT * bufferLength);
            buffer = new Float32Array(buffer.buffer, buffer.byteOffset, bufferLength);
            analyzerObj.analyzer.smoothingTimeConstant = 0;

            analyzerObj.analyzer.getFloatFrequencyData(buffer);
            return true;
        }
        catch (e) {
            console.log("Failed to get sample data " + e);
        }
        return false;
    }
};
autoAddDeps(AmplitudeLib, '$chunksBuffer');
autoAddDeps(AmplitudeLib, '$analyzers');
mergeInto(LibraryManager.library, AmplitudeLib);