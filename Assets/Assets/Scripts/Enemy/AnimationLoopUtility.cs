#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

/// <summary>
/// Utility menu item to set all animations in an Animator Controller to loop.
/// </summary>
public static class AnimationLoopUtility
{
    [MenuItem("Tools/Enemy Animations/Set All Animations in Selected Controller to Loop")]
    public static void SetSelectedControllerAnimationsToLoop()
    {
        // Get the selected object
        Object selected = Selection.activeObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select an Animator Controller in the Project window.", "OK");
            return;
        }
        
        AnimatorController controller = selected as AnimatorController;
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Invalid Selection", "Please select an Animator Controller (.controller file).", "OK");
            return;
        }
        
        SetAnimationsToLoop(controller);
    }
    
    [MenuItem("Tools/Enemy Animations/Set All Animations in Selected Controller to Loop", true)]
    public static bool ValidateSetSelectedControllerAnimationsToLoop()
    {
        return Selection.activeObject is AnimatorController;
    }
    
    [MenuItem("Tools/Enemy Animations/Set All Selected Animation Clips to Loop")]
    public static void SetSelectedClipsToLoop()
    {
        List<AnimationClip> clips = new List<AnimationClip>();
        
        foreach (Object obj in Selection.objects)
        {
            AnimationClip clip = obj as AnimationClip;
            if (clip != null)
            {
                clips.Add(clip);
            }
        }
        
        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("No Animation Clips", "Please select one or more Animation Clip files in the Project window.", "OK");
            return;
        }
        
        int loopedCount = 0;
        foreach (var clip in clips)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = WrapMode.Loop;
            EditorUtility.SetDirty(clip);
            loopedCount++;
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"Set {loopedCount} animation clip(s) to loop.");
    }
    
    [MenuItem("Tools/Enemy Animations/Set All Selected Animation Clips to Loop", true)]
    public static bool ValidateSetSelectedClipsToLoop()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is AnimationClip)
                return true;
        }
        return false;
    }
    
    private static void SetAnimationsToLoop(AnimatorController controller)
    {
        if (controller == null)
        {
            Debug.LogWarning("Animator Controller is null!");
            return;
        }
        
        int loopedCount = 0;
        
        // Get all animation clips from the controller
        foreach (var layer in controller.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.motion != null)
                {
                    AnimationClip clip = state.state.motion as AnimationClip;
                    if (clip != null)
                    {
                        // Set the clip to loop
                        var settings = AnimationUtility.GetAnimationClipSettings(clip);
                        settings.loopTime = true;
                        AnimationUtility.SetAnimationClipSettings(clip, settings);
                        clip.wrapMode = WrapMode.Loop;
                        
                        // Mark the asset as dirty so it saves
                        EditorUtility.SetDirty(clip);
                        loopedCount++;
                    }
                }
            }
        }
        
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Success", $"Set {loopedCount} animation(s) to loop in {controller.name}.", "OK");
        Debug.Log($"Set {loopedCount} animation(s) to loop in {controller.name}");
    }
}
#endif


