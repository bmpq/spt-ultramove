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

        private AudioClip[] footstepClips;

        private float walkCooldown = 0.36f;
        private float nextStepTime;

        private static readonly System.Random random = new System.Random();

        public PlayerAudio(AssetBundle bundle)
        {
            player = Singleton<GameWorld>.Instance.MainPlayer.Transform.Original;

            AudioClip[] allClips = bundle.LoadAllAssets<AudioClip>();

            footstepClips = allClips
                .Where(clip => clip.name.StartsWith("footstep_heavy"))
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

            Singleton<BetterAudio>.Instance.PlayAtPoint(player.position, clip, 0, BetterAudio.AudioSourceGroupType.Character, 5, 1f, EOcclusionTest.None, null, false);

            nextStepTime = Time.time + walkCooldown;
        }
    }
}
