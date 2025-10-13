// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2025 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="DirectionalAnimationSet4"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/DirectionalAnimationSet4Editor
    [CustomEditor(typeof(DirectionalAnimationSet4), true)]
    public class DirectionalAnimationSet4Editor : DirectionalAnimationSetEditor { }

    /// <summary>[Editor-Only] A custom Inspector for <see cref="DirectionalAnimationSet8"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/DirectionalAnimationSet8Editor
    [CustomEditor(typeof(DirectionalAnimationSet8), true)]
    public class DirectionalAnimationSet8Editor : DirectionalAnimationSetEditor { }

    /// <summary>[Editor-Only]
    /// A custom Inspector for 
    /// <see cref="DirectionalAnimationSet4"/> and <see cref="DirectionalAnimationSet8"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/DirectionalAnimationSetEditor
    [CanEditMultipleObjects]
    public class DirectionalAnimationSetEditor : ScriptableObjectEditor
    {
        /************************************************************************************************************************/

        [MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet2) + "/Find Animations")]
        [MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet4) + "/Find Animations")]
        [MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet8) + "/Find Animations")]
        private static void FindSimilarAnimations(MenuCommand command)
        {
            var set = (DirectionalSet<AnimationClip>)command.context;
            var setName = set.name;

            var directory = AssetDatabase.GetAssetPath(set);
            directory = Path.GetDirectoryName(directory);

            var guids = AssetDatabase.FindAssets(
                $"{set.name} t:{nameof(AnimationClip)}",
                new string[] { directory });

            using (new ModifySerializedField(set, "Find Animations"))
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip == null)
                        continue;

                    var clipName = clip.name;
                    if (clipName.StartsWith(setName))
                        clipName = clipName[setName.Length..];

                    set.SetByName(clipName, clip);
                }
            }
        }

        /************************************************************************************************************************/

        [MenuItem(
            itemName: Strings.CreateMenuPrefix + "Directional Animation Set/From Selection",
            priority = Strings.AssetMenuOrder + 7)]
        private static void CreateDirectionalAnimationSet()
        {
            GatherSelectedAnimationClips(out var clips, out var namesLowercase);

            if (clips.Count == 0)
                throw new InvalidOperationException("No animation clips are selected");
            else if (clips.Count == 1)
                throw new InvalidOperationException("Only 1 animation clip is selected");

            var prefix = GetCommonPrefix(namesLowercase);

            var count = clips.Count;
            DirectionalSet<AnimationClip> set
                = count <= 2
                ? CreateInstance<DirectionalAnimationSet2>()
                : count <= 4
                ? CreateInstance<DirectionalAnimationSet4>()
                : CreateInstance<DirectionalAnimationSet8>();

            set.AllowChanges();
            for (int i = 0; i < clips.Count; i++)
            {
                var name = namesLowercase[i][prefix.Length..];
                set.SetByName(name, clips[i]);
            }

            // The prefix is lowercase so get the original case from the first clip.
            var firstClip = clips[0];
            var setName = firstClip.name;
            var nameLength = prefix.Length;
            while (nameLength > 0)
            {
                var character = setName[nameLength - 1];
                if (char.IsLetterOrDigit(character))
                    break;

                nameLength--;
            }

            if (nameLength <= 0)
                nameLength = prefix.Length;

            setName = setName[..nameLength];

            var path = AssetDatabase.GetAssetPath(firstClip);
            path = $"{Path.GetDirectoryName(path)}/{setName}.asset";
            AssetDatabase.CreateAsset(set, path);

            Selection.objects = new Object[] { set };
        }

        /************************************************************************************************************************/

        public static void GatherSelectedAnimationClips(
            out List<AnimationClip> clips,
            out List<string> namesLowercase)
        {
            clips = new();
            namesLowercase = new();

            var selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                var clip = selection[i] as AnimationClip;
                if (clip == null)
                    continue;

                clips.Add(clip);
                namesLowercase.Add(clip.name.ToLower());
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a string containing the section from the start of each of the `strings`
        /// which is exactly the same.
        /// </summary>
        public static string GetCommonPrefix(IList<string> strings)
        {
            if (strings == null ||
                strings.Count == 0)
                return "";

            // Start with the first string as the candidate prefix.
            var prefix = strings[0];

            for (int i = 1; i < strings.Count; i++)
            {
                var current = strings[i];
                var length = Math.Min(prefix.Length, current.Length);

                // Find the common prefix length between the prefix and current string.
                int j;
                for (j = 0; j < length; j++)
                    if (prefix[j] != current[j])
                        break;

                // Shorten the prefix to the common part.
                prefix = prefix[..j];

                // Early exit if there's no common prefix left.
                if (prefix.Length == 0)
                    break;
            }

            return prefix;
        }

        /************************************************************************************************************************/

        [MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet2) + "/Toggle Looping")]
        [MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet4) + "/Toggle Looping")]
        [MenuItem("CONTEXT/" + nameof(DirectionalAnimationSet8) + "/Toggle Looping")]
        private static void ToggleLooping(MenuCommand command)
        {
            var set = (DirectionalSet<AnimationClip>)command.context;

            var count = set.DirectionCount;
            for (int i = 0; i < count; i++)
            {
                var clip = set.Get(i);
                if (clip == null)
                    continue;

                var isLooping = !clip.isLooping;
                for (i = 0; i < count; i++)
                {
                    clip = set.Get(i);
                    if (clip == null)
                        continue;

                    AnimancerEditorUtilities.SetLooping(clip, isLooping);
                }

                break;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

