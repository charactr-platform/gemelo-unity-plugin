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

    WebGL_Initialize: function (bufferSize, allocationSize, sampleRate) {
        
        if (typeof pcmFramesBuffer.buffer == "undefined")
        {
            pcmFramesBuffer.buffer = _malloc(allocationSize);       
        }
        else
        {
           _free(pcmFramesBuffer.buffer);
            pcmFramesBuffer.buffer = _malloc(allocationSize);
        }
       
        //Create new instance of audio context with proper sampleRate      
        WEBAudio.audioContext.close().then(()=>{
            WEBAudio.audioContext = new AudioContext({
            latencyHint: "interactive",
            sampleRate: sampleRate});

            console.log("Audio Context created, SampleRate: "+ sampleRate);
        });

        var bytes = new Uint8Array(pcmFramesBuffer.buffer, pcmFramesBuffer.buffer.byteOffset, Float32Array.BYTES_PER_ELEMENT * allocationSize);
        pcmFramesBuffer.stream = new Float32Array(bytes);
        pcmFramesBuffer.size = bufferSize;
        pcmFramesBuffer.samplesLength = 0;

        console.log("PCM frames buffer ready, size: "+bufferSize+", heap size: "+Module.HEAPU8.buffer.byteLength)
    },

    WebGL_FillBuffer: function (array, size, index) {
      
        var buffer = new Uint8Array(Module.HEAPU8.buffer, array, Float32Array.BYTES_PER_ELEMENT * size);
        var samples = new Float32Array(buffer.buffer, buffer.byteOffset, size);

        pcmFramesBuffer.stream.set(samples, pcmFramesBuffer.samplesLength);
        pcmFramesBuffer.samplesLength += samples.length;

        console.log("Added buffer, total length: "+pcmFramesBuffer.samplesLength);
    },
   
    WebGL_GetChannelIndex: function() {
        var channelIndex = 0;

         for (let index = WEBAudio.audioInstanceIdCounter; index > 0; --index) {
                 var object = WEBAudio.audioInstances[index];
                 if (object.gain) {
                    channelIndex = index;
                    break;
                 }      
            }

        return channelIndex;
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
            bufferIndex: 0,
            position: 0,
        };

        var ctx = WEBAudio.audioContext;
        var count = WEBAudio.audioInstanceIdCounter;
         
        if (count < 2)
        {
            console.log("AudioInstances must contains 2 or more items, count = " + count);
            return false;
        }
        
        //Channel - Unity Audio Source
        var channelIndex = WebGL_GetChannelIndex();
       
        var sound = WEBAudio.audioInstances[bufferInstance];
        var channel = WEBAudio.audioInstances[channelIndex];
        
        if (!sound || !sound.buffer) {
            console.error("buffer instance is not an sound instance: "+bufferInstance); 
            console.dir(sound);
            return;
        }

        if (channel != null) {
            audioInstance = channel;
            audioInstance.gain = ctx.createGain();
            audioInstance.panner = ctx.createPanner();
            source = ctx.createBufferSource();
            source.buffer = sound.buffer;
            console.dir(ctx);
            console.dir(audioInstance);
        }
        
        stream.processorNode = ctx.createScriptProcessor(pcmFramesBuffer.size, 1, 1);
        stream.processorNode.onaudioprocess = function(event) {

            var size = pcmFramesBuffer.size;
            var outputArray = event.outputBuffer.getChannelData(0);

            if (!streaming) {
                var inputArray = event.inputBuffer.getChannelData(0);
                outputArray = inputArray;
                return;
            }

            if (outputArray.length < size)
            {
                console.log("No output size...")
                return;
            }

            if (pcmFramesBuffer.samplesLength <= stream.position)
            {
                outputArray = new Float32Array(size);
                console.log("No more data...");
                return;
            }
        
            stream.buffer = pcmFramesBuffer.stream.slice(stream.position, stream.position + size);
      
            for (let index = 0; index < size; index++) {

                outputArray[index] = stream.buffer[index]; 
            }

            stream.position += size;
            console.log("Processed samples ["+ stream.position + " / " + pcmFramesBuffer.samplesLength + "]");
        }
        
        source.connect(stream.processorNode);
        
        console.dir(source);

        analyzer = ctx.createAnalyser();
            
        analyzer.fftSize = sampleSize;
        
        stream.processorNode.connect(analyzer);
    
        analyzer.connect(ctx.destination);

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
};
autoAddDeps(AmplitudeLib, '$pcmFramesBuffer');
autoAddDeps(AmplitudeLib, '$analyzers');
mergeInto(LibraryManager.library, AmplitudeLib);