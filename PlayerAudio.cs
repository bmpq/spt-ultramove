﻿using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal class PlayerAudio
    {
        public static PlayerAudio Instance;
        private Transform player;
        private Transform cam;

        private AudioClip[] footstepClips;
        private AudioClip[] shootClips;
        private AudioClip[] shootClipsRail;
        private AudioClip[] shootClipsShotgun;

        private float walkCooldown = 0.33f;
        private float nextStepTime;

        private static readonly System.Random random = new System.Random();

        AudioClip[] allClips;

        BetterSource sourceSlide;

        BetterSource sourceWalk;
        BetterSource sourceWaterSkip;

        Dictionary<string, BetterSource> loops = new Dictionary<string, BetterSource>();

        public PlayerAudio(AssetBundle bundle)
        {
            player = Singleton<GameWorld>.Instance.MainPlayer.Transform.Original;
            cam = Camera.main.transform;

            allClips = bundle.LoadAllAssets<AudioClip>();

            footstepClips = allClips
                .Where(clip => clip.name.StartsWith("footstep_heavy"))
                .OrderBy(clip => ExtractIndex(clip.name))
                .ToArray();

            shootClips = allClips
                .Where(clip => clip.name.StartsWith("Shoot1c"))
                .OrderBy(clip => ExtractIndex(clip.name))
                .ToArray();

            shootClipsRail = allClips
                .Where(clip => clip.name.StartsWith("RailcannonFire"))
                .OrderBy(clip => ExtractIndex(clip.name))
                .ToArray();

            shootClipsShotgun = allClips
                .Where(clip => clip.name.StartsWith("Steampunk Weapons - Shotgun 2 - Shot"))
                .OrderBy(clip => ExtractIndex(clip.name))
                .ToArray();
        }

        public AudioClip GetClip(string clip)
        {
            return allClips.FirstOrDefault(c => c.name.StartsWith(clip));
        }

        public BetterSource GetSource()
        {
            return Singleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Character, true);
        }

        private int ExtractIndex(string clipName)
        {
            string numericPart = new string(clipName.SkipWhile(c => !char.IsDigit(c)).ToArray());
            return int.TryParse(numericPart, out int result) ? result : int.MaxValue; // Return max if no index found
        }

        public void PlayWalk()
        {
            if (Time.time < nextStepTime) return;

            if (sourceWalk == null)
            {
                sourceWalk = Singleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Collisions, true);
                sourceWalk.SetBaseVolume(0.5f);
                sourceWalk.Loop = false;
            }
            sourceWalk.Position = player.position;

            AudioClip clip = footstepClips[random.Next(footstepClips.Length)];
            sourceWalk.SetPitch(UnityEngine.Random.Range(0.8f, 1.2f));
            sourceWalk.Play(clip, null, 1f, 0.5f, false, true);

            nextStepTime = Time.time + walkCooldown;
        }

        public void PlayShoot()
        {
            AudioClip clip = shootClips[random.Next(shootClips.Length)];
            PlayInTarkov(clip, cam.position + cam.forward * 0.2f);
        }

        public void PlayShootRail()
        {
            AudioClip clip = shootClipsRail[random.Next(shootClipsRail.Length)];
            PlayInTarkov(clip, cam.position + cam.forward * 0.2f);
        }

        public void PlayShootShotgun()
        {
            AudioClip clip = shootClipsShotgun[random.Next(shootClipsShotgun.Length)];
            PlayInTarkov(clip, cam.position + cam.forward * 0.2f);
        }

        public void PlayAtPoint(string clip, Vector3 pos, float volume = 1f, float pitch = 1f)
        {
            var audioClip = allClips.FirstOrDefault(c => c.name.StartsWith(clip));
            if (audioClip != null)
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, audioClip, CameraClass.Instance.Distance(pos), BetterAudio.AudioSourceGroupType.Environment, 60);
            }
            else
                Plugin.Log.LogError($"Could not find audio clip {clip}!");
        }

        public void Play(string clip, float volume = 1f)
        {
            Vector3 pos = cam.transform.position;

            var audioClip = allClips.FirstOrDefault(c => c.name.StartsWith(clip));
            if (audioClip != null)
                PlayInTarkov(audioClip, pos, volume);
            else
                Plugin.Log.LogError($"Could not find audio clip {clip}!");
        }

        public void Loop(string clip, bool active, float volume = 1f)
        {
            Loop(clip, active, cam.transform.position);
        }

        public void Loop(string clip, bool active, Vector3 pos, float volume = 1f)
        {
            if (active)
            {
                BetterSource source;
                if (!loops.ContainsKey(clip))
                {
                    source = Singleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Collisions, true);
                    source.Loop = true;
                    source.Play(allClips.FirstOrDefault(c => c.name.StartsWith(clip)), null, 1f, 1f, false, false);
                    source.SetBaseVolume(volume);
                    loops[clip] = source;
                }
                else
                    source = loops[clip];

                source.Position = pos;
            }
            else
            {
                if (loops.ContainsKey(clip))
                {
                    loops[clip].Release();
                    loops.Remove(clip);
                }
            }
        }

        public void PlayWaterSkip(Vector3 pos)
        {
            if (sourceWaterSkip == null)
            {
                sourceWaterSkip = Singleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Collisions, true);
                sourceWaterSkip.Loop = false;
            }

            sourceWaterSkip.Position = pos;
            sourceWaterSkip.SetPitch(UnityEngine.Random.Range(0.9f, 1.1f));
            sourceWaterSkip.Play(allClips.FirstOrDefault(c => c.name.StartsWith("WaterSmallSplash")), null, 1f, 1f, false, false);
        }

        void PlayInTarkov(AudioClip clip, Vector3 pos, float volume = 1f)
        {
            Singleton<BetterAudio>.Instance.PlayAtPoint(pos, clip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 50, volume, EOcclusionTest.None, null, false);
        }
    }
}
