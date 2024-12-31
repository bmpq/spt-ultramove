using Comfort.Common;
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

        private float walkCooldown = 0.33f;
        private float nextStepTime;

        private static readonly System.Random random = new System.Random();

        AudioClip[] allClips;

        BetterSource sourceSlide;

        public PlayerAudio(AssetBundle bundle)
        {
            player = Singleton<GameWorld>.Instance.MainPlayer.Transform.Original;
            cam = Camera.main.transform;

            allClips = bundle.LoadAllAssets<AudioClip>();

            sourceSlide = MonoBehaviourSingleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Environment, true);
            sourceSlide.Loop = true;
            sourceSlide.Play(allClips.FirstOrDefault(c => c.name.StartsWith("wallcling")), null, 1f, 1f, false, false);

            footstepClips = allClips
                .Where(clip => clip.name.StartsWith("footstep_heavy"))
                .OrderBy(clip => ExtractIndex(clip.name))
                .ToArray();

            shootClips = allClips
                .Where(clip => clip.name.StartsWith("Shoot1c"))
                .OrderBy(clip => ExtractIndex(clip.name))
                .ToArray();
        }

        private int ExtractIndex(string clipName)
        {
            string numericPart = new string(clipName.SkipWhile(c => !char.IsDigit(c)).ToArray());
            return int.TryParse(numericPart, out int result) ? result : int.MaxValue; // Return max if no index found
        }

        public void PlayWalk()
        {
            if (Time.time < nextStepTime) return;

            AudioClip clip = footstepClips[random.Next(footstepClips.Length)];
            PlayInTarkov(clip, player.position, 1f);

            nextStepTime = Time.time + walkCooldown;
        }

        public void PlayShoot()
        {
            AudioClip clip = shootClips[random.Next(shootClips.Length)];
            PlayInTarkov(clip, cam.position + cam.forward);
        }

        public void Play(string clip, float volume = 1f, float pitch = 1f)
        {
            Vector3 pos = player.position;

            foreach (var item in allClips)
            {
                if (item.name.StartsWith(clip))
                {
                    PlayInTarkov(item, pos, volume);
                    return;
                }
            }

            Plugin.Log.LogError($"Could not find audio clip {clip}!");
        }

        public void Sliding(bool sliding, Vector3 playerPos, Vector3 slideDir)
        {
            Vector3 point = playerPos + slideDir + new Vector3(0, 0.1f, 0);

            sourceSlide.Position = point;
            sourceSlide.SetBaseVolume(sliding ? 1f : 0f);
        }

        void PlayInTarkov(AudioClip clip, Vector3 pos, float volume = 1f)
        {
            Singleton<BetterAudio>.Instance.PlayAtPoint(pos, clip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 1, volume, EOcclusionTest.None, null, false);
        }
    }
}
