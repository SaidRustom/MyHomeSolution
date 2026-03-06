window.CameraInterop = {
    _streams: new Map(),

    startCamera: async function (videoElementId) {
        try {
            const video = document.getElementById(videoElementId);
            if (!video) return { success: false, error: 'Video element not found.' };

            const stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 960 } }
            });

            video.srcObject = stream;
            await video.play();
            this._streams.set(videoElementId, stream);
            return { success: true, error: null };
        } catch (err) {
            return { success: false, error: err.message || 'Camera access denied.' };
        }
    },

    captureFrame: function (videoElementId) {
        const video = document.getElementById(videoElementId);
        if (!video || !video.srcObject) return null;

        const canvas = document.createElement('canvas');
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext('2d').drawImage(video, 0, 0);
        return canvas.toDataURL('image/jpeg', 0.92);
    },

    stopCamera: function (videoElementId) {
        const stream = this._streams.get(videoElementId);
        if (stream) {
            stream.getTracks().forEach(t => t.stop());
            this._streams.delete(videoElementId);
        }
        const video = document.getElementById(videoElementId);
        if (video) video.srcObject = null;
    },

    isCameraAvailable: function () {
        return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
    }
};
