using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupZenitsuAnimator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Setup Zenitsu Animator")]
    static void Setup()
    {
        // 1. Find Player and Animator
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }

        Animator animator = player.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Animator or Controller missing on Player!");
            return;
        }

        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            // It might be an OverrideController, try to get base
            AnimatorOverrideController sections = animator.runtimeAnimatorController as AnimatorOverrideController;
            if (sections != null) controller = sections.runtimeAnimatorController as AnimatorController;
        }
        
        if (controller == null)
        {
            Debug.LogError("Could not find the raw AnimatorController asset.");
            return;
        }

        // 2. Add Parameter
        AddParameter(controller, "isChargingSixfold", AnimatorControllerParameterType.Bool);
        Debug.Log("Added 'isChargingSixfold' Parameter.");

        // 3. Add State
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Check if state exists
        AnimatorState chargeState = FindState(rootStateMachine, "SixfoldCharge");
        if (chargeState == null)
        {
            chargeState = rootStateMachine.AddState("SixfoldCharge");
            chargeState.motion = null; // User will assign their animation here
            Debug.Log("Created 'SixfoldCharge' State.");
        }

        // 4. Add Transitions (Any State -> Charge)
        // Check if transition exists to avoid duplicates
        bool hasEntryTransition = false;
        foreach(var t in rootStateMachine.anyStateTransitions)
        {
            if (t.destinationState == chargeState) hasEntryTransition = true;
        }

        if (!hasEntryTransition)
        {
            AnimatorStateTransition trans = rootStateMachine.AddAnyStateTransition(chargeState);
            trans.AddCondition(AnimatorConditionMode.If, 0, "isChargingSixfold");
            trans.duration = 0.1f;
            trans.hasExitTime = false;
        }

        // Charge -> Exit (Back to Locomotion)
        // Simple way: Transition to "Idle" or "Exit". Let's transition to Exit so the graph decides next state.
        // But transitioning to Exit from AnyState destination can execute infinitely if conditions persist.
        // Safer: Transition back to Idle.
        
        AnimatorState idleState = FindState(rootStateMachine, "Idle");
        if (idleState != null)
        {
             // Check outgoing
             bool hasExit = false;
             foreach(var t in chargeState.transitions)
             {
                 if (t.destinationState == idleState) hasExit = true;
             }
             
             if (!hasExit)
             {
                 AnimatorStateTransition exitTrans = chargeState.AddTransition(idleState);
                 exitTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "isChargingSixfold");
                 exitTrans.duration = 0.1f;
                 exitTrans.hasExitTime = false;
             }
        }
        else
        {
            Debug.LogWarning("Could not find 'Idle' state to return to. Please connect 'SixfoldCharge' exit manually.");
        }

        Debug.Log("Animator Setup Complete! Please assign your Animation Clip to the 'SixfoldCharge' state.");
    }

    static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var child in sm.states)
        {
            if (child.state.name == name) return child.state;
        }
        return null;
    }
#endif
}
