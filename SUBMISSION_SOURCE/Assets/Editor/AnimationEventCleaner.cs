// Assets/Editor/AnimationEventCleaner.cs
// Utility to find and remove Animation Events that have no function name.
// Run it once via: Tools → Fix Empty Animation Events

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationEventCleaner
{
    [MenuItem("Tools/Fix Empty Animation Events")]
    public static void RemoveEmptyAnimationEvents()
    {
        string[] allAnimGuids = AssetDatabase.FindAssets("t:AnimationClip");
        int totalFixed = 0;
        int clipsFixed = 0;

        foreach (string guid in allAnimGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Load ALL animation clips at this path (FBX can contain multiple)
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip == null) continue;

                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                if (events == null || events.Length == 0) continue;

                List<AnimationEvent> cleanEvents = new List<AnimationEvent>();
                int removedFromClip = 0;

                foreach (AnimationEvent evt in events)
                {
                    if (string.IsNullOrEmpty(evt.functionName))
                    {
                        removedFromClip++;
                        totalFixed++;
                    }
                    else
                    {
                        cleanEvents.Add(evt);
                    }
                }

                if (removedFromClip > 0)
                {
                    AnimationUtility.SetAnimationEvents(clip, cleanEvents.ToArray());
                    EditorUtility.SetDirty(clip);
                    clipsFixed++;
                    Debug.Log($"[AnimationEventCleaner] Removed {removedFromClip} empty event(s) from: {path} / {clip.name}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (totalFixed == 0)
            Debug.Log("[AnimationEventCleaner] No empty Animation Events found — all clips are clean.");
        else
            Debug.Log($"[AnimationEventCleaner] Done! Removed {totalFixed} empty event(s) across {clipsFixed} clip(s).");
    }
}
