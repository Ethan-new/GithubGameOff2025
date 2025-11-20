#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor utility to ensure animations in the Enemy Animator Controller are set to loop.
/// </summary>
[CustomEditor(typeof(EnemyAnimator))]
public class EnemyAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EnemyAnimator enemyAnimator = (EnemyAnimator)target;
        Animator animator = enemyAnimator.GetComponent<Animator>();
        
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("To ensure animations loop:\n1. Select your Animation Clips in the Project window\n2. In the Inspector, check 'Loop Time' in the Animation settings\n3. Or use the button below to auto-configure animations in the controller", MessageType.Info);
            
            if (GUILayout.Button("Set All Animations in Controller to Loop"))
            {
                SetAnimationsToLoop(animator.runtimeAnimatorController as AnimatorController);
            }
        }
    }
    
    private void SetAnimationsToLoop(AnimatorController controller)
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
                        // Set the clip to loop using wrap mode
                        var settings = AnimationUtility.GetAnimationClipSettings(clip);
                        settings.loopTime = true;
                        AnimationUtility.SetAnimationClipSettings(clip, settings);
                        
                        // Also set wrap mode for runtime
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
        
        Debug.Log($"Set {loopedCount} animation(s) to loop in {controller.name}");
    }
}
#endif

