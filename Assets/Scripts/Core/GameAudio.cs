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
        private readonly AudioClip[] musicClips=new AudioClip[12];
        private readonly AudioClip[] skillClips=new AudioClip[40];
        private int musicTheme=-1,requestedTheme=-1;
        private float musicFade=1f;
        private static readonly string[] MusicNames={"营火微光","荒野行旅","沉星残响","林庭守望","烬河疾行","蚀星战鼓","回廊迷途","林庭复明","赤岩炉心","星台封印","巨影交锋","城镇夜曲"};
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

        public static void PlaySkill(HeroClass hero,int skill)
        {
            if(muted||quitting||lifecycle.BackgroundPaused||!Application.isPlaying||(int)hero<0||(int)hero>3||skill<0||skill>=10)return;
            EnsureInstance();int index=(int)hero*10+skill;
            if(instance.skillClips[index]==null)instance.skillClips[index]=SynthesizeSkill(hero,skill);
            instance.PlayInternal((int)SoundCue.Cast,instance.skillClips[index]);
        }
        private static int CurrentMusicTheme()
        {
            var game=GameSession.Instance;if(game==null||!game.HasStarted)return 0;
            if(!game.InDungeon)return game.CurrentHub>0?11:game.IsInCamp?0:1;
            if(game.InCombat)foreach(var enemy in game.Enemies)if(enemy!=null&&!enemy.IsDead&&enemy.IsBoss&&enemy.gameObject.activeInHierarchy)return 10;
            if(game.ChapterActive)return 7+(int)game.ActiveChapterNode;
            if(game.RoomChainRun!=null)return 6;
            return game.ModeRun==null?2:3+(int)game.ModeRun.Mode;
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
            ambientSource.volume = BackgroundVolume * masterVolume * musicFade;
        }

        private void PlayInternal(int cue,AudioClip skillClip=null)
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
            voice.mute = false;voice.enabled=true;
            voice.clip = skillClip!=null?skillClip:clips[cue];
            voice.Play();
        }

        private void Update()
        {
            if (instance != this || lifecycle.BackgroundPaused) return;
            if (Time.unscaledTime >= nextListenerCheck)
            {
                nextListenerCheck = Time.unscaledTime + .5f;
                EnsureListener();requestedTheme=CurrentMusicTheme();
            }
            if(!muted&&ambientSource!=null)
            {
                bool changing=requestedTheme>=0&&requestedTheme!=musicTheme;
                musicFade=Mathf.MoveTowards(musicFade,changing?0f:1f,Time.unscaledDeltaTime/ .65f);
                if(changing&&musicFade<=0){ambientSource.Stop();musicTheme=requestedTheme;ambientClip=MusicClip(musicTheme);ambientSource.clip=ambientClip;ambientResumeSample=-1;ambientSource.Play();}
                ambientSource.volume=BackgroundVolume*masterVolume*musicFade;
            }
            if (!muted && !quitting && !lifecycle.BackgroundPaused && ambientSource != null && !ambientSource.isPlaying) StartBackground();
        }

        private void EnsureListener()
        {
            if(!muted&&!quitting&&!lifecycle.BackgroundPaused)
            {AudioListener.pause=false;AudioListener.volume=1f;}
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
            EnsurePool();EnsureListener();
            ambientSource.enabled=true;
            if (ambientClip == null){musicTheme=requestedTheme=CurrentMusicTheme();ambientClip=MusicClip(musicTheme);}
            ambientSource.clip = ambientClip;
            ambientSource.volume = BackgroundVolume * masterVolume * musicFade;
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
            if (ambientSource != null) ambientSource.volume = BackgroundVolume * masterVolume * musicFade;
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

        private AudioClip MusicClip(int theme)
        {if(musicClips[theme]==null)musicClips[theme]=SynthesizeBackground(theme);return musicClips[theme];}
        private static AudioClip SynthesizeBackground(int theme)
        {
            double[] beats={.75,.6,.8,.6,.4,.375,.65,.7,.45,.55,.32,.8};
            int[][] progression={new[]{0,5,7,0,9,5,7,0},new[]{0,7,9,4,5,0,7,0},new[]{0,3,-2,5,0,-5,3,0},new[]{0,5,3,7,0,3,5,7},new[]{0,0,-2,3,0,5,3,-2},new[]{0,-1,3,0,-5,3,-1,0},new[]{0,3,7,6,0,-2,3,6},new[]{0,4,7,9,5,4,7,0},new[]{0,-5,0,3,-2,0,5,3},new[]{0,7,3,10,5,7,3,0},new[]{0,0,-1,3,0,-5,3,-1},new[]{0,4,5,7,9,5,4,0}};
            int[][] motifs={new[]{0,4,7,12,7,4,2,0},new[]{0,7,9,12,14,9,7,4},new[]{0,3,7,10,7,3,-2,0},new[]{0,7,5,3,10,7,5,3},new[]{0,0,7,3,0,10,7,3},new[]{0,12,7,0,3,7,10,7},new[]{0,6,3,10,7,6,3,-2},new[]{0,4,9,7,12,9,7,4},new[]{0,3,0,7,3,10,7,0},new[]{0,7,14,10,19,14,10,7},new[]{0,0,3,7,0,10,7,3},new[]{0,4,7,11,9,7,4,2}};
            double beat=beats[theme],chordDuration=beat*4,duration=chordDuration*8;
            var samples=new float[(int)(duration*SampleRate)];float peak=.001f;bool percussion=theme==4||theme==5||theme==8||theme==10;
            for(int i=0;i<samples.Length;i++)
            {
                double t=i/(double)SampleRate;int chord=Math.Min(7,(int)(t/chordDuration)),step=(int)(t/beat);double ct=t-chord*chordDuration,nt=t-step*beat;
                double root=(theme==10?82.41:theme==8?98:theme==7?164.81:130.81)*Math.Pow(2,progression[theme][chord]/12.0);
                double envelope=Math.Min(1,ct/.15)*Math.Min(1,(chordDuration-ct)/.25);
                double pad=(Math.Sin(Tau*root*ct)*.16+Math.Sin(Tau*root*1.5*ct)*.08)*Math.Max(0,envelope);
                double frequency=root*2*Math.Pow(2,motifs[theme][step%8]/12.0);
                double melody=Bell(nt,frequency)*(theme==2||theme==6?.15:.26)*Math.Max(0,Math.Min(1,(beat-nt)/.04));
                double drum=percussion?Math.Sin(Tau*(55*nt+1.7*(1-Math.Exp(-nt*30))))*Math.Exp(-nt*24)*(step%4==0?.35:.14):0;
                double fade=Math.Min(1,t/.025)*Math.Min(1,(duration-t)/.08);
                samples[i]=(float)((pad+melody+drum)*Math.Max(0,fade));peak=Math.Max(peak,Math.Abs(samples[i]));
            }
            for(int i=0;i<samples.Length;i++)samples[i]*=.62f/peak;
            var clip=AudioClip.Create("Emberfall "+MusicNames[theme],samples.Length,1,SampleRate,false);clip.hideFlags=HideFlags.DontSave;clip.SetData(samples,0);return clip;
        }
        private static AudioClip SynthesizeSkill(HeroClass hero,int skill)
        {
            double duration=.28+(skill%3)*.11+(skill>=7?.2:0);var samples=new float[(int)(duration*SampleRate)];uint state=(uint)(1979+(int)hero*101+skill*337);float peak=.001f;
            for(int i=0;i<samples.Length;i++)
            {
                double t=i/(double)SampleRate,p=t/duration;state^=state<<13;state^=state>>17;state^=state<<5;double noise=state/(double)uint.MaxValue*2-1;
                double tone=180+skill*43,value;
                if(hero==HeroClass.Vanguard)value=noise*Math.Exp(-p*(4+skill%3))*.55+Math.Sin(Tau*(tone*t-90*t*t))*.3*Math.Exp(-p*5);
                else if(hero==HeroClass.Arcanist)
                {
                    bool fire=skill==1||skill==5||skill==8;
                    value=fire?(noise*.6+Math.Sin(Tau*(65+skill*8)*t)*.3)*Math.Sin(Math.PI*p)*Math.Exp(-p*1.5):(Bell(t,900+skill*115)+Bell(t-.06,1500+skill*95)*.6+noise*.12*Math.Exp(-p*9));
                }
                else if(hero==HeroClass.Ranger)value=Math.Sin(Tau*(tone*2*t+50*t*t))*Math.Exp(-p*13)*.65+noise*Math.Sin(Math.PI*p)*Math.Exp(-p*4)*.45;
                else value=Bell(t,660+skill*64)*.6+Bell(t-.055,990+skill*91)*.4+Bell(t-.12,1320+skill*110)*.25+noise*.035*Math.Sin(Math.PI*p)*Math.Exp(-p*5);
                double envelope=Math.Min(1,t/.004)*Math.Min(1,(duration-t)/.025);samples[i]=(float)(value*Math.Max(0,envelope));peak=Math.Max(peak,Math.Abs(samples[i]));
            }
            for(int i=0;i<samples.Length;i++)samples[i]*=.7f/peak;
            var clip=AudioClip.Create("Emberfall "+hero+" Skill "+skill,samples.Length,1,SampleRate,false);clip.hideFlags=HideFlags.DontSave;clip.SetData(samples,0);return clip;
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
                instance.EnsureListener();instance.ApplyVolumes();instance.ResetThrottle();
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
            for(int i=0;i<musicClips.Length;i++){if(musicClips[i]!=null)Destroy(musicClips[i]);musicClips[i]=null;}
            for(int i=0;i<skillClips.Length;i++){if(skillClips[i]!=null)Destroy(skillClips[i]);skillClips[i]=null;}
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
            EnsureListener();ApplyVolumes();ResetThrottle();
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
