#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateZenitsuAnimator : MonoBehaviour
{
    [MenuItem("Tools/Generate Zenitsu Animator")]
    public static void Generate()
    {
        // 1. Create Controller
        string path = "Assets/Sprites/Zenitsu/ZenitsuController.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // 2. Add Parameters
        controller.AddParameter("isRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isCharging", AnimatorControllerParameterType.Bool); // NEW
        controller.AddParameter("isDashing", AnimatorControllerParameterType.Bool); 
        controller.AddParameter("yVelocity", AnimatorControllerParameterType.Float);

        // 3. Add States (Layer 0)
        var rootStateMachine = controller.layers[0].stateMachine;

        var stateIdle = rootStateMachine.AddState("Idle");
        var stateRun = rootStateMachine.AddState("Run");
        var stateJump = rootStateMachine.AddState("Jump");
        var stateFall = rootStateMachine.AddState("Fall");
        var stateCharge = rootStateMachine.AddState("ThunderCharge"); // NEW Phase 1
        var stateDash = rootStateMachine.AddState("ThunderDash");     // NEW Phase 2

        // 4. Transitions

        // Any State -> Charge (Start)
        var anyToCharge = rootStateMachine.AddAnyStateTransition(stateCharge);
        anyToCharge.AddCondition(AnimatorConditionMode.If, 0, "isCharging");
        anyToCharge.duration = 0;
        anyToCharge.hasExitTime = false; // Fix: Instant transition

        // Charge -> Dash (Attack)
        var chargeToDash = stateCharge.AddTransition(stateDash);
        chargeToDash.AddCondition(AnimatorConditionMode.If, 0, "isDashing");
        chargeToDash.duration = 0;
        chargeToDash.hasExitTime = false; // Fix: Instant transition

        // Dash -> Idle (End)
        var dashToIdle = stateDash.AddTransition(stateIdle);
        dashToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isDashing");
        dashToIdle.duration = 0;
        dashToIdle.hasExitTime = false; // Fix: Instant transition

        // Emergency: If Charge is cancelled or bugs out -> Idle
        var chargeToIdle = stateCharge.AddTransition(stateIdle);
        chargeToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isCharging");
        chargeToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isDashing");
        chargeToIdle.duration = 0;

        // Idle <-> Run
        var toRun = stateIdle.AddTransition(stateRun);
        toRun.AddCondition(AnimatorConditionMode.If, 0, "isRunning");
        toRun.duration = 0; // Instant transition for pixel art

        var toIdle = stateRun.AddTransition(stateIdle);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isRunning");
        toIdle.duration = 0;

        // Any State -> Jump (When not grounded and moving up)
        var anyToJump = rootStateMachine.AddAnyStateTransition(stateJump);
        anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "isGrounded");
        anyToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "yVelocity");
        anyToJump.duration = 0;

        // Any State -> Fall (When not grounded and moving down)
        // Or simply Jump -> Fall
        var jumpToFall = stateJump.AddTransition(stateFall);
        jumpToFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "yVelocity");
        jumpToFall.duration = 0;

        // Fall -> Idle/Run (Landing)
        var fallToIdle = stateFall.AddTransition(stateIdle);
        fallToIdle.AddCondition(AnimatorConditionMode.If, 0, "isGrounded");
        fallToIdle.duration = 0;

        Debug.Log("Zenitsu Animator Created at: " + path);
        
        // Optional: Assign to selected object if it has Animator
        GameObject zenitsu = GameObject.Find("Zenitsu");
        if (zenitsu != null)
        {
            Animator anim = zenitsu.GetComponent<Animator>();
            if (anim) anim.runtimeAnimatorController = controller;
        }
    }
}
#endif
