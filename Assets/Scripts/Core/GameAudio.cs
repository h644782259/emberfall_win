using System;
using UnityEngine;

namespace Emberfall
{

    /// <summary>Eight bounded effect voices plus one quiet, seamless background loop.</summary>
    public sealed partial class GameAudio : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private const int CueCount = 11;
        private const int SampleRate = 22050;
        private const float DefaultVolume = .5f;
        private const float BackgroundVolume = .14f;
        private const double Tau = Math.PI * 2.0;
        private static readonly float[] Durations = { .16f, .34f, .13f, .22f, .38f, .64f, .95f, .65f, .09f, .17f, .42f };
        private static readonly float[] MinimumIntervals = { .075f, .12f, .075f, .15f, .16f, .25f, .5f, .5f, .055f, .085f, .12f };
        private static GameAudio instance;
        private static bool muted;
        private static bool quitting;
        private static readonly AudioLifecycleGate lifecycle=new AudioLifecycleGate();
        private bool ambientPaused;
        private int ambientResumeSample=-1;
        private static float masterVolume = 1f;
        private AudioSource[] voices;
        private AudioClip[] clips;
        private AudioSource ambientSource;
        private AudioClip ambientClip;
        private AudioListener listener;
        private float[] lastPlayed;
        private int nextVoice;
        private float lastImpact = float.NegativeInfinity;
        private float nextListenerCheck;

        public struct Diagnostics
        {
            public bool Initialized, Muted, ListenerPresent, ListenerPaused, AmbientPlaying;
            public int EffectSources, PlayingEffects, CachedCues, OutputSampleRate;
            public float MasterVolume, EffectVolume, AmbientVolume, ListenerVolume;
        }

        public static float MasterVolume
        {
            get { return masterVolume; }
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) return;
                masterVolume = Mathf.Clamp01(value);
                if (instance != null) instance.ApplyVolumes();
            }
        }

        public static bool Muted
        {
            get { return muted; }
            set
            {
                muted = value;
                if (muted)
                {
                    if (instance != null) instance.StopVoices();
                }
                else if (Application.isPlaying && !quitting)
                {
                    EnsureInstance();
                    instance.ResetThrottle();
                    instance.StartBackground();
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // With domain reload disabled, Play can rediscover any surviving scene instance.
            if (instance != null)
            {
                instance.StopVoices();
                instance.ResetThrottle();
            }
            instance = null;
            muted = false;
            masterVolume = 1f;
            quitting = false;
            lifecycle.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (quitting) return;
            // These are this player's Unity audio controls, never OS mixer settings.
            // Do not overwrite Muted here: an isolated test may mute before scene load.
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            EnsureInstance();
            instance.StartBackground();
        }

        public static void Play(SoundCue cue)
        {
            int index = (int)cue;
            if (muted || quitting || lifecycle.BackgroundPaused || index < 0 || index >= CueCount || !Application.isPlaying) return;
            EnsureInstance();
            instance.PlayInternal(index);
        }

        private static void EnsureInstance()
        {
            if (instance == null)
            {
#if UNITY_6000_0_OR_NEWER
                instance = FindAnyObjectByType<GameAudio>();
#else
                instance = FindObjectOfType<GameAudio>();
#endif
                if (instance == null)
                {
                    GameObject root = new GameObject("Emberfall Audio");
                    instance = root.AddComponent<GameAudio>();
                }
            }
            if (!instance.enabled) instance.enabled = true;
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsurePool();
        }

        private void EnsurePool()
        {
            if (voices != null) return;
            voices = new AudioSource[VoiceCount];
            clips = new AudioClip[CueCount];
            lastPlayed = new float[CueCount];
            for (int i = 0; i < CueCount; i++) lastPlayed[i] = float.NegativeInfinity;
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
                voice.dopplerLevel = 0f;
                voice.ignoreListenerPause = true;
                voice.priority = 96;
                voice.volume = DefaultVolume * masterVolume;
                voices[i] = voice;
            }
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
            ambientSource.dopplerLevel = 0f;
            ambientSource.ignoreListenerPause = true;
            ambientSource.priority = 192;
            ambientSource.volume = BackgroundVolume * masterVolume;
        }

        private void PlayInternal(int cue)
        {
            EnsurePool();
            EnsureListener();
            float now = Time.unscaledTime;
            if (now - lastPlayed[cue] < MinimumIntervals[cue]) return;
            if (AudioVoicePolicy.IsImpact((SoundCue)cue) && now - lastImpact < .1f) return;
            int busy = 0;
            for(int i=0;i<VoiceCount;i++) if(voices[i] == null || voices[i].isPlaying) busy |= 1 << i;
            int selected = AudioVoicePolicy.Select((SoundCue)cue, busy, nextVoice);
            if (selected < 0 || voices[selected] == null) return;
            AudioSource voice = voices[selected];
            if (voice.isPlaying) voice.Stop(); // Only a reserved critical channel can be replaced.
            nextVoice = (nextVoice + 1) % VoiceCount;
            if (AudioVoicePolicy.IsImpact((SoundCue)cue)) lastImpact = now;
            if (clips[cue] == null) clips[cue] = Synthesize((SoundCue)cue);
            lastPlayed[cue] = now;
            voice.volume = DefaultVolume * masterVolume * (cue == (int)SoundCue.UI ? .55f : 1f);
            voice.mute = false;
            voice.clip = clips[cue];
            voice.Play();
        }

        private void Update()
        {
            if (instance != this || lifecycle.BackgroundPaused) return;
            if (Time.unscaledTime >= nextListenerCheck)
            {
                nextListenerCheck = Time.unscaledTime + .5f;
                EnsureListener();
            }
            if (!muted && !quitting && !lifecycle.BackgroundPaused && ambientSource != null && !ambientSource.isPlaying) StartBackground();
        }

        private void EnsureListener()
        {
            if (listener != null && listener.isActiveAndEnabled) return;
#if UNITY_6000_6_OR_NEWER
            AudioListener[] existing = FindObjectsByType<AudioListener>();
#elif UNITY_6000_0_OR_NEWER
            AudioListener[] existing = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
#else
            AudioListener[] existing = FindObjectsOfType<AudioListener>();
#endif
            foreach (AudioListener candidate in existing)
                if (candidate.isActiveAndEnabled) { listener = candidate; return; }
            Camera camera = Camera.main;
            if (camera == null) return; // Let the scene bootstrap create its camera first.
            listener = camera.GetComponent<AudioListener>();
            if (listener == null) listener = camera.gameObject.AddComponent<AudioListener>();
            listener.enabled = true;
        }

        private void StartBackground()
        {
            if (muted || quitting || lifecycle.BackgroundPaused || !isActiveAndEnabled) return;
            EnsurePool();
            if (ambientClip == null) ambientClip = SynthesizeBackground();
            ambientSource.clip = ambientClip;
            ambientSource.volume = BackgroundVolume * masterVolume;
            ambientSource.mute = false;
            if (!ambientSource.isPlaying)
            {
                if(ambientResumeSample>=0&&ambientClip.samples>0)ambientSource.timeSamples=ambientResumeSample%ambientClip.samples;
                ambientResumeSample=-1;ambientPaused=false;ambientSource.Play();
            }
        }

        private void ApplyVolumes()
        {
            ApplyNpcVoiceVolume();
            if (voices != null)
                foreach (AudioSource voice in voices)
                    if (voice != null) voice.volume = DefaultVolume * masterVolume *
                        (clips != null && voice.clip == clips[(int)SoundCue.UI] && voice.clip != null ? .55f : 1f);
            if (ambientSource != null) ambientSource.volume = BackgroundVolume * masterVolume;
        }

        public static Diagnostics GetDiagnostics()
        {
            var result = new Diagnostics
            {
                Initialized = instance != null, Muted = muted, MasterVolume = masterVolume,
                EffectVolume = DefaultVolume * masterVolume, AmbientVolume = BackgroundVolume * masterVolume,
                ListenerPaused = AudioListener.pause, ListenerVolume = AudioListener.volume,
                OutputSampleRate = AudioSettings.outputSampleRate
            };
            if (instance == null) return result;
            instance.EnsureListener();
            result.ListenerPresent = instance.listener != null && instance.listener.isActiveAndEnabled;
            result.AmbientPlaying = instance.ambientSource != null && instance.ambientSource.isPlaying;
            if (instance.voices != null)
                foreach (AudioSource voice in instance.voices)
                    if (voice != null) { result.EffectSources++; if (voice.isPlaying) result.PlayingEffects++; }
            if (instance.clips != null) foreach (AudioClip clip in instance.clips) if (clip != null) result.CachedCues++;
            return result;
        }

        private static AudioClip Synthesize(SoundCue cue)
        {
            float duration = Durations[(int)cue];
            int length = Math.Max(1, (int)(duration * SampleRate));
            var samples = new float[length];
            uint noiseState = 0x6d2b79f5u + (uint)cue * 997u;
            double phase = 0;
            double smoothedNoise = 0;
            float peak = .001f;
            for (int i = 0; i < samples.Length; i++)
            {
                double time = i / (double)SampleRate;
                double progress = time / duration;
                noiseState ^= noiseState << 13;
                noiseState ^= noiseState >> 17;
                noiseState ^= noiseState << 5;
                double noise = (noiseState / (double)uint.MaxValue) * 2 - 1;
                smoothedNoise += (noise - smoothedNoise) * .18;
                double frequency;
                double value;
                switch (cue)
                {
                    case SoundCue.Attack:
                        frequency = 450 - 300 * progress;
                        phase += Tau * frequency / SampleRate;
                        value = (Math.Sin(phase) * .28 + noise * .55) * Math.Exp(-progress * 4);
                        break;
                    case SoundCue.Judgment:
                    case SoundCue.Cast:
                        frequency = 350 + progress * 850;
                        phase += Tau * frequency / SampleRate;
                        value = (Math.Sin(phase) + Math.Sin(phase * 1.5) * .25) * Math.Sin(Math.PI * progress) * .55;
                        break;
                    case SoundCue.Hit:
                        // A fast bright contact followed by a descending low body separates
                        // a landed hit from Attack's longer, airy weapon swing.
                        frequency = 58 + 160 * Math.Exp(-time * 52);
                        phase += Tau * frequency / SampleRate;
                        double impactBody = (Math.Sin(phase) * .95 + Math.Sin(phase * .5) * .18) * Math.Exp(-time * 31);
                        double contact = (noise - smoothedNoise) * .68 * Math.Exp(-time * 155);
                        double edge = Math.Sin(Tau * 2700 * time) * .19 * Math.Exp(-time * 110);
                        value = impactBody + contact + edge;
                        break;
                    case SoundCue.CriticalHit:
                        // An actual critical impact has a short bright crack and metallic ring.
                        // It shares the bounded voice pool but never aliases the ordinary Hit clip.
                        frequency = 82 + 230 * Math.Exp(-time * 65);
                        phase += Tau * frequency / SampleRate;
                        double criticalBody = Math.Sin(phase) * .9 * Math.Exp(-time * 29);
                        double criticalCrack = (noise - smoothedNoise) * .85 * Math.Exp(-time * 170);
                        double criticalRing = (Math.Sin(Tau * 1680 * time) + Math.Sin(Tau * 2520 * time) * .4) * .3 * Math.Exp(-time * 39);
                        value = criticalBody + criticalCrack + criticalRing;
                        break;
                    case SoundCue.Dodge:
                        value = (noise - smoothedNoise) * Math.Sin(Math.PI * progress) * Math.Exp(-progress * 1.5);
                        break;
                    case SoundCue.Loot:
                        value = Bell(time, 740) + Bell(time - .10, 1110) * .8;
                        break;
                    case SoundCue.LevelUp:
                        value = Bell(time, 523.25) + Bell(time - .10, 659.25) + Bell(time - .20, 783.99) + Bell(time - .30, 1046.5);
                        break;
                    case SoundCue.Victory:
                        value = Bell(time, 523.25) * .65 + Bell(time - .10, 659.25) * .65 + Bell(time - .20, 783.99) * .65;
                        value += Bell(time - .34, 523.25) + Bell(time - .34, 659.25) * .7 + Bell(time - .34, 1046.5) * .7;
                        break;
                    case SoundCue.UI:
                        value = Bell(time, 880) * .7 + Bell(time - .022, 1320) * .35;
                        break;
                    default:
                        frequency = 230 - progress * 155;
                        phase += Tau * frequency / SampleRate;
                        value = (Math.Sin(phase) + Math.Sin(phase * .5) * .4) * Math.Exp(-progress * 2.2);
                        break;
                }
                // Short ramps eliminate discontinuities at both ends of every clip.
                double attackRamp = (cue == SoundCue.Hit || cue == SoundCue.CriticalHit) ? .0014 : .004;
                double envelope = Math.Min(1, time / attackRamp) * Math.Min(1, (duration - time) / .025);
                samples[i] = (float)(value * Math.Max(0, envelope));
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            float normalizer = .8f / peak;
            for (int i = 0; i < samples.Length; i++) samples[i] *= normalizer;
            AudioClip clip = AudioClip.Create("Emberfall " + cue, length, 1, SampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            if (!clip.SetData(samples, 0)) Debug.LogError("Emberfall could not upload PCM for " + cue);
            return clip;
        }

        private static AudioClip SynthesizeBackground()
        {
            const int seconds = 24;
            var samples = new float[seconds * SampleRate];
            double[] roots = { 146.83, 116.54, 174.61, 130.81, 146.83, 116.54, 130.81, 146.83 };
            double[] melodySteps = { 2, 3, 4, 3, 2, 2.5, 3, 2.5 };
            float peak = .001f;
            for (int i = 0; i < samples.Length; i++)
            {
                double time = i / (double)SampleRate;
                int chord = Math.Min(roots.Length - 1, (int)(time / 3));
                double chordTime = time - chord * 3;
                double root = roots[chord];
                double padEnvelope = Math.Min(1, chordTime / .35) * Math.Min(1, (3 - chordTime) / .45);
                double pad = (Math.Sin(Tau * root * chordTime) * .15 + Math.Sin(Tau * root * 1.5 * chordTime) * .08 +
                    Math.Sin(Tau * root * 2 * chordTime) * .055) * Math.Max(0, padEnvelope);
                int beat = (int)(time / .75);
                double noteTime = time - beat * .75;
                double noteEnvelope = Math.Min(1, (.75 - noteTime) / .04);
                double melody = Bell(noteTime, root * melodySteps[beat % melodySteps.Length]) * .30 * Math.Max(0, noteEnvelope);
                // The last note/pad fades to zero and the first attacks from zero,
                // keeping the loop seam quiet without an audio-thread generator.
                samples[i] = (float)(pad + melody);
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            float gain = .62f / peak;
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
            AudioClip clip = AudioClip.Create("Emberfall Quiet Hearth", samples.Length, 1, SampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            if (!clip.SetData(samples, 0)) Debug.LogError("Emberfall could not upload background PCM");
            return clip;
        }

        private static double Bell(double time, double frequency)
        {
            if (time < 0) return 0;
            double attack = Math.Min(1, time / .007);
            return (Math.Sin(Tau * frequency * time) + .18 * Math.Sin(Tau * frequency * 2 * time)) * Math.Exp(-time * 8.5) * attack;
        }

        public static void SetBackgroundPaused(bool paused)
        {
            AudioLifecycleTransition transition=lifecycle.Observe(paused);
            if(transition==AudioLifecycleTransition.None||instance==null)return;
            if(transition==AudioLifecycleTransition.Suspend)
            {
                StopNpcGreeting();
                // Discard old one-shots. Preserve the loop cursor, including the
                // ambient source which deliberately ignores AudioListener.pause.
                if(instance.voices!=null)foreach(var voice in instance.voices)if(voice!=null)voice.Stop();
                instance.ambientPaused=instance.ambientSource!=null&&instance.ambientSource.isPlaying;
                if(instance.ambientPaused)instance.ambientSource.Pause();
            }
            else if(!muted&&instance.isActiveAndEnabled)
            {
                if(instance.ambientPaused&&instance.ambientSource!=null)
                {instance.ambientSource.UnPause();instance.ambientPaused=false;}
                else instance.StartBackground();
            }
        }

        private void StopVoices()
        {
            StopNpcGreeting();
            ambientPaused=false;ambientResumeSample=-1;
            if (voices == null) return;
            foreach (AudioSource voice in voices) if (voice != null) voice.Stop();
            if (ambientSource != null) ambientSource.Stop();
        }

        private void ResetThrottle()
        {
            lastImpact = float.NegativeInfinity;
            if (lastPlayed == null) return;
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = float.NegativeInfinity;
        }

        private void ReleaseClips()
        {
            if (ambientSource != null) ambientSource.clip = null;
            if (ambientClip != null) Destroy(ambientClip);
            ambientClip = null;
            if (clips == null) return;
            if (voices != null) foreach (AudioSource voice in voices) if (voice != null) voice.clip = null;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) Destroy(clips[i]);
                clips[i] = null;
            }
        }

        private void OnEnable()
        {
            if (instance != this) return;
            ResetThrottle();
            ApplyVolumes();
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
            StartBackground();
        }

        private void OnAudioConfigurationChanged(bool deviceChanged)
        {
            listener = null;
            nextListenerCheck = 0;
            if (ambientSource != null)
            {
                if(ambientResumeSample<0&&ambientSource.clip!=null)ambientResumeSample=ambientSource.timeSamples;
                ambientSource.Stop();ambientPaused=false;
            }
            // StartBackground is gated during suspension; resume seeks to the
            // preserved sample rather than restarting the loop from zero.
            StartBackground();
        }

        private void OnDisable()
        {
            // Unity invokes OnDisable before a script/domain reload as well as on scene teardown.
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
            StopVoices();
            ReleaseClips();
        }

        private void OnApplicationQuit() { quitting = true; }

        private void OnDestroy()
        {
            StopVoices();
            ReleaseClips();
            if (instance == this) instance = null;
        }
    }
}
