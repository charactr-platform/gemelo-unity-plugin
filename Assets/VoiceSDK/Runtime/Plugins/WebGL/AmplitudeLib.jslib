// JavaScript library for accessing amplitude audio from Web Audio API directly from the web browser
var AmplitudeLib = {
    /** Contains all the currently running analyzers */
    $analyzers: {},
    $pcmFramesBuffer:{},
    
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

        if (typeof pcmFramesBuffer.buffers == "undefined")
        {
            pcmFramesBuffer.buffers = []
            console.log("PCM frames buffer ready")
        }
        
        var buffer = new Uint8Array(Module.HEAPU8.buffer, array, Float32Array.BYTES_PER_ELEMENT * size);
        pcmFramesBuffer.buffers[index] = new Float32Array(buffer.buffer, buffer.byteOffset, size);
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
        var stream = {
            pcmBufferSize: 1024,
            currentBufferIndex: 0,
            bufferPosition: 0,
        };

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
    
       
        if (channel != null) {
            audioInstance = channel;
            source = ctx.createBufferSource();
            source.buffer = sound.buffer;
        }
        
        if (source == null)
            return false;

        stream.processorNode = ctx.createScriptProcessor(stream.pcmBufferSize, 1, 1);
        stream.processorNode.onaudioprocess = function(event) {

            var outputArray = event.outputBuffer.getChannelData(0);

            if (!streaming) {
                var inputArray = event.inputBuffer.getChannelData(0);
                outputArray = inputArray;
                return;
            }

            if (outputArray.length < stream.pcmBufferSize)
            {
                console.log("No output size...")
                return;
            }

            var framesLoadedCount =  pcmFramesBuffer.buffers.length - 1;
             
            if (framesLoadedCount < stream.currentBufferIndex)
            {
                outputArray = new Float32Array(stream.pcmBufferSize);
                console.log("No more data...");
                return;
            }
            else 
            {
                var buffer = pcmFramesBuffer.buffers[stream.currentBufferIndex];
                stream.buffer = buffer;
                stream.currentBufferIndex++;
            }
        
            console.log("OutputBuffer F:"+ stream.buffer[0]+" L:"+ stream.buffer[stream.buffer.length-1]+" S:"+  stream.buffer.length)
            
            for (let index = 0; index < stream.pcmBufferSize; index++) {

                if (stream.buffer.length < index)
                    outputArray[index] = 0.0;
                else
                    outputArray[index] = stream.buffer[index]; 

            }

            console.log("Processed samples [" +stream.buffer.length + " / " + outputArray.length +"] ["+stream.currentBufferIndex + " / "  + framesLoadedCount + "]");
        }
        
        source.connect(stream.processorNode);
        
        source.playbackRate.value = 0.72;

        console.dir(source);

        stream.processorNode.connect(ctx.destination);
        
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
        
        if (typeof pcmFramesBuffer.frames != "undefined"){
            delete pcmFramesBuffer.frames;
            console.log("Cleared PcmFramesBuffer")
        }

        if (analyzerObj != null) {
            try {
                analyzerObj.source.disconnect();
                analyzerObj.stream.processorNode.disconnect();
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
autoAddDeps(AmplitudeLib, '$pcmFramesBuffer');
autoAddDeps(AmplitudeLib, '$analyzers');
mergeInto(LibraryManager.library, AmplitudeLib);