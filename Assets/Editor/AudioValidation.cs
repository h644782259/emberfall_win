using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Tests the real Unity audio sources and PCM; never changes the OS mixer.</summary>
    public static class AudioValidation
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        // Yields null deliberately: RuntimeValidation manually advances nested enumerators.
        // Run after the title camera exists, and forward Check/Append for shared reporting.
        public static IEnumerator Validate(Action<bool, string> check, Action<string> log)
        {
            bool originalMuted = GameAudio.Muted;
            float originalVolume = GameAudio.MasterVolume;
            try
            {
                GameAudio.Muted = true;
                GameAudio.MasterVolume = .04f;
                GameAudio.Muted = false;
                GameAudio.Diagnostics initial = GameAudio.GetDiagnostics();
                check(initial.Initialized && initial.EffectSources == 8, "Audio: initialized bounded pool has eight effect sources");
                check(initial.ListenerPresent && !initial.ListenerPaused && initial.ListenerVolume > 0, "Audio: enabled listener is present, unpaused and audible");
                check(initial.OutputSampleRate > 0, "Audio: Unity reports an active output sample rate");

                GameAudio owner = (GameAudio)typeof(GameAudio).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                AudioSource[] voices = Read<AudioSource[]>(owner, "voices");
                AudioSource background = Read<AudioSource>(owner, "ambientSource");
                AudioClip backgroundClip = Read<AudioClip>(owner, "ambientClip");
                check(background != null && background.clip == backgroundClip && background.loop && background.isPlaying,
                    "Audio: background clip is explicitly assigned and looping in Unity");
                check(background.spatialBlend == 0 && !background.mute && background.volume > 0,
                    "Audio: background uses an audible 2D source");
                ValidatePcm(backgroundClip, "background", check);

                int firstBackgroundSample = background.timeSamples;
                double dspBefore = AudioSettings.dspTime;
                float mixedPeak = 0;
                float[] output = new float[1024];
                // Native audio may warm up asynchronously after the editor enters Play Mode.
                double deadline = EditorApplication.timeSinceStartup + 3;
                while (EditorApplication.timeSinceStartup < deadline &&
                    (mixedPeak <= .000001f || AudioSettings.dspTime <= dspBefore || background.timeSamples == firstBackgroundSample))
                {
                    background.GetOutputData(output, 0);
                    foreach (float value in output) mixedPeak = Mathf.Max(mixedPeak, Mathf.Abs(value));
                    yield return null;
                }
                log("AUDIO diagnostics: DSP=" + dspBefore + " -> " + AudioSettings.dspTime + "; samples=" + firstBackgroundSample + " -> " + background.timeSamples + "; peak=" + mixedPeak + "; editorPaused=" + EditorApplication.isPaused + "; editorMute=" + typeof(EditorUtility).GetProperty("audioMasterMute", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null));
                check(AudioSettings.dspTime > dspBefore, "Audio: Unity DSP clock advances during playback");
                check(background.timeSamples != firstBackgroundSample, "Audio: native background playback advances through its PCM samples");
                check(mixedPeak > .000001f, "Audio: playing background source produces nonzero native output samples");
                log("AUDIO Unity output: " + initial.OutputSampleRate + " Hz; background output peak=" + mixedPeak.ToString("G5") + ". This does not certify physical speakers.");

                foreach (SoundCue cue in new[] { SoundCue.UI, SoundCue.Attack, SoundCue.Hit, SoundCue.Cast, SoundCue.CriticalHit })
                {
                    GameAudio.Muted = true;
                    GameAudio.Muted = false;
                    GameAudio.Play(cue);
                    AudioClip clip = Read<AudioClip[]>(owner, "clips")[(int)cue];
                    AudioSource playing = null;
                    foreach (AudioSource voice in voices)
                        if (voice.clip == clip && voice.isPlaying) { playing = voice; break; }
                    check(clip != null && playing != null, "Audio: " + cue + " binds its clip and starts an effect source");
                    check(playing != null && playing.spatialBlend == 0 && !playing.mute && !playing.loop && playing.volume > 0,
                        "Audio: " + cue + " uses positive-gain 2D one-shot playback");
                    ValidatePcm(clip, cue.ToString(), check);
                    deadline = EditorApplication.timeSinceStartup + .035;
                    while (EditorApplication.timeSinceStartup < deadline) yield return null;
                }

                AudioClip[] clips = Read<AudioClip[]>(owner, "clips");
                float[] hit = new float[clips[(int)SoundCue.Hit].samples];
                float[] attack = new float[clips[(int)SoundCue.Attack].samples];
                clips[(int)SoundCue.Hit].GetData(hit, 0);
                clips[(int)SoundCue.Attack].GetData(attack, 0);
                double hitEarly = 0, hitLate = 0, difference = 0;
                for (int i = 0; i < hit.Length; i++)
                {
                    if (i < hit.Length / 2) hitEarly += hit[i] * hit[i];
                    else hitLate += hit[i] * hit[i];
                    difference += Math.Abs(hit[i] - attack[i]);
                }
                check(hitEarly > hitLate * 4, "Audio: landed hit has a short contact transient and decaying body");
                check(difference / hit.Length > .08, "Audio: landed hit PCM differs from the weapon swing");

                GameAudio.Muted = true;
                check(!background.isPlaying && GameAudio.GetDiagnostics().PlayingEffects == 0,
                    "Audio: mute stops both background and all effects");
                GameAudio.Play(SoundCue.UI);
                check(GameAudio.GetDiagnostics().PlayingEffects == 0, "Audio: calls while muted cannot start an effect");
                GameAudio.Muted = false;
                GameAudio.Play(SoundCue.UI);
                check(background.isPlaying && GameAudio.GetDiagnostics().PlayingEffects == 1,
                    "Audio: unmute restores background and immediately accepts UI feedback");
                GameAudio.MasterVolume = .02f;
                check(Mathf.Abs(background.volume - .14f * .02f) < .00001f, "Audio: master gain reaches the live background source");
                foreach (AudioSource voice in voices)
                    if (voice.isPlaying) check(Mathf.Abs(voice.volume - .5f * .55f * .02f) < .00001f,
                        "Audio: master gain reaches the live UI source");
                log("AUDIO passed initialization, listener, PCM, native output, explicit cue playback, hit distinction, mute recovery and live volume checks.");
            }
            finally
            {
                GameAudio.Muted = true;
                GameAudio.MasterVolume = originalVolume;
                GameAudio.Muted = originalMuted;
            }
        }

        private static T Read<T>(GameAudio owner, string name)
        {
            return (T)typeof(GameAudio).GetField(name, PrivateInstance).GetValue(owner);
        }

        private static void ValidatePcm(AudioClip clip, string name, Action<bool, string> check)
        {
            check(clip != null && clip.samples > 0 && clip.channels == 1, "Audio: " + name + " owns a nonempty mono clip");
            if (clip == null) return;
            float[] samples = new float[clip.samples * clip.channels];
            check(clip.GetData(samples, 0), "Audio: " + name + " real PCM can be read back from Unity");
            float peak = 0;
            double energy = 0;
            bool finite = true;
            foreach (float sample in samples)
            {
                if (float.IsNaN(sample) || float.IsInfinity(sample)) finite = false;
                peak = Mathf.Max(peak, Mathf.Abs(sample));
                energy += sample * sample;
            }
            check(finite && peak > .1f && peak <= 1 && energy / samples.Length > .00001,
                "Audio: " + name + " PCM is finite, nonzero and unclipped");
        }
    }
}
