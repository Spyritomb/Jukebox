window.jukeboxPlayer = (() => {
    let audio = null;
    let dotnetRef = null;

    function get() {
        if (!audio) audio = document.getElementById('jukebox-audio');
        return audio;
    }

    return {
        init(ref) {
            dotnetRef = ref;
            const a = get();
            if (!a) return;

            a.addEventListener('ended', () => {
                dotnetRef?.invokeMethodAsync('OnSongEndedAsync');
            });

            a.addEventListener('timeupdate', () => {
                dotnetRef?.invokeMethodAsync('OnTimeUpdateAsync', a.currentTime, a.duration || 0);
            });
        },

        // Called every time we switch tracks — sets src, loads, and plays
        setSrc(url) {
            const a = get();
            if (!a || !url) return;
            a.src = url;
            a.load();
            a.play().catch(err => console.warn('Playback error:', err));
        },

        resume() {
            get()?.play();
        },

        pause() {
            get()?.pause();
        },

        stop() {
            const a = get();
            if (!a) return;
            a.pause();
            a.currentTime = 0;
        },

        seek(seconds) {
            const a = get();
            if (a) a.currentTime = seconds;
        },

        setVolume(vol) {
            const a = get();
            if (a) a.volume = vol;
        }
    };
})();
