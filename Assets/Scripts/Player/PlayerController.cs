using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 8f;
    public float jumpHeight = 2.5f; // Desired height in Units (v = sqrt(2gh))
    public LayerMask groundLayer;
    public Transform groundCheck;

    [Header("Zenitsu Mode (Thunderclap)")]
    public float dashSpeed = 40f; 
    public float chargeDuration = 0.15f; 
    public float dashDuration = 0.15f; 
    public float dashCooldown = 0.3f; 
    public ParticleSystem dashParticle; // New: Particle System reference
    public GameObject attackCollider; 

    [Header("Combat")]
    public GameObject normalAttackCollider;
    public int normalAttackDamage = 25;
    public int thunderclapDamage = 50; // New: Higher damage for Skill
    public float attackDelay = 0.1f; 
    public float attackDuration = 0.3f;
    private bool isAttacking = false; 

    // Layer Caching for Penetration
    private int playerLayer;
    private int enemyLayer; 

    private Rigidbody2D rb;
    private Animator animator; 
    [SerializeField] private bool isGrounded; // Debuggable
    private Vector2 moveInput; 
    
    // State
    private bool isDashing = false;
    private bool isCharging = false; 
    private bool canDash = true;
    private bool controlsPaused = false; 

    // UI Helpers
    public float GetDashCooldownRatio() 
    {
        // If mid-action (Charging or Dashing), show Full Cooldown
        if (isCharging || isDashing) return 1f;
        
        // If ready
        if (canDash) return 0f;
        
        // If in cool-down phase
        return currentCooldownTimer / dashCooldown;
    }
    private float currentCooldownTimer = 0f; 



    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // Auto-get parameter
        
        // 1. Fix Rotation (Prevent Toppling)
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 2. Fix Sticking (Prevent Wall/Enemy Stick)
        // Create Frictionless Material in memory
        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("Slippery");
        slipperyMat.friction = 0f;
        slipperyMat.bounciness = 0f;
        
        // Apply to all Colliders on this object
        Collider2D[] colls = GetComponents<Collider2D>();
        foreach(var c in colls) c.sharedMaterial = slipperyMat;

        // Auto-assign GroundCheck if missing
        if (groundCheck == null)
        {
            Transform foundCheck = transform.Find("GroundCheck");
            if (foundCheck != null)
            {
                groundCheck = foundCheck;
            }
            else
            {
                // GroundCheck not found - Silent
            }
        }

        // Auto-assign GroundLayer if Nothing (0)
        if (groundLayer.value == 0)
        {
            // Default to Default, Ground, Wall
            // Ensure Enemy is NOT ground if you don't want to jump off their head.
            groundLayer = LayerMask.GetMask("Default", "Ground", "Wall"); 
            if(groundLayer.value == 0) groundLayer = -1; // Everything
        }

        // Ensure components are off by default
        if(attackCollider) attackCollider.SetActive(false);
        if(normalAttackCollider) normalAttackCollider.SetActive(false);

        // Cache Layers
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    public void PauseControls(float duration)
    {
        StartCoroutine(PauseControlsRoutine(duration));
    }

    IEnumerator PauseControlsRoutine(float duration)
    {
        controlsPaused = true;
        moveInput = Vector2.zero; // Reset internal input
        rb.linearVelocity = Vector2.zero; // Optional: Stop sliding too
        
        yield return new WaitForSeconds(duration);
        
        controlsPaused = false;
    }

    // Sixfold State
    private int sixfoldStacks = 0;
    private float sixfoldExpireTimer = 0f;
    private float cKeyHoldTime = 0f;
    private bool cKeyWasPressed = false;
    private bool cKeyReadyFeedbackPlayed = false;

    void Update()
    {
        // 1. Move Input & Actions (Blocked if Paused/Dashing/Charging/Attacking)
        if (!controlsPaused && !isDashing && !isCharging && !isAttacking)
        {
            float x = 0;
            
            if (Keyboard.current != null)
            {
                // ... (Move Input) ...
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1;

                // Jump Input
                if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
                {
                    Jump();
                }

                // --- SIXFOLD CHARGE LOGIC (Key: C) ---
                if (Keyboard.current.cKey.isPressed)
                {
                    if (!cKeyWasPressed) // First frame press
                    {
                        cKeyWasPressed = true;
                        cKeyHoldTime = 0f;
                        cKeyReadyFeedbackPlayed = false;
                    }
                    cKeyHoldTime += Time.deltaTime;
                    
                    // Trigger "Charge Complete" visual once
                    if (cKeyHoldTime >= 2.0f && !cKeyReadyFeedbackPlayed)
                    {
                        cKeyReadyFeedbackPlayed = true;
                        StartCoroutine(PlayChargeCompleteEffect());
                    }
                    
                    // Optional: Feedback for charging
                    if(animator) animator.SetBool("isChargingSixfold", true);
                }
                else
                {
                    // Key Released
                    if (cKeyWasPressed)
                    {
                        if (cKeyHoldTime >= 2.0f)
                        {
                            // Activate Sixfold Mode
                            sixfoldStacks = 6;
                            sixfoldExpireTimer = 1.0f; // 1s window to start
                        }
                        cKeyWasPressed = false;
                        cKeyHoldTime = 0f;
                        if(animator) animator.SetBool("isChargingSixfold", false);
                    }
                }
            }

            // --- SIXFOLD EXPIRATION LOGIC ---
            if (sixfoldStacks > 0)
            {
                sixfoldExpireTimer -= Time.deltaTime;
                if (sixfoldExpireTimer <= 0)
                {
                    sixfoldStacks = 0; // Expired
                }
            }
            
            // Vertical input is ignored for walking in this setup
            moveInput = new Vector2(x, 0);

            // 2. Thunderclap Input (Left Mouse Click)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && canDash)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mouseWorldPos.z = transform.position.z; // Flatten Z
                Vector2 dashDir = (mouseWorldPos - transform.position).normalized;

                StartCoroutine(ThunderclapAndFlash(dashDir));
            }

            // 3. Normal Attack (Ctrl Key)
            if (Keyboard.current.ctrlKey.wasPressedThisFrame)
            {
                StartCoroutine(PerformNormalAttack());
            }

            // 4. Flip Sprite (Visuals)
            if (moveInput.x > 0) transform.localScale = new Vector3(1, 1, 1);
            else if (moveInput.x < 0) transform.localScale = new Vector3(-1, 1, 1);
        }

        // 4. Update Animation (ALWAYS RUNS)
        if (animator != null)
        {
            // ... (keep existing animation logic) ...
            // Always set ability states
            animator.SetBool("isCharging", isCharging); 
            animator.SetBool("isDashing", isDashing); 

            // Standard Movement Animation
            if (!isCharging && !isDashing)
            {
                animator.SetBool("isRunning", Mathf.Abs(moveInput.x) > 0.1f);
                animator.SetBool("isGrounded", isGrounded);
                
                float localYVel = Vector2.Dot(rb.linearVelocity, transform.up);
                animator.SetFloat("yVelocity", localYVel);
                
                // NEW: Auto-Align to Gravity
                AlignToGravity();
            }
            else
            {
                // Special Move Override
                animator.SetBool("isRunning", false);
                animator.SetBool("isGrounded", true); 
                animator.SetFloat("yVelocity", 0f);   
            }
        }
    }

    void AlignToGravity()
    {
        // 1. Determine "Up" based on Gravity
        Vector2 g = Physics2D.gravity;
        if (g == Vector2.zero) return; 

        Vector2 targetUp = -g.normalized;

        // 2. Calculate Target Angle (Degrees)
        // Standard Atan2 returns angle from Right (1,0). 
        // Our sprite "Up" is (0,1). So we subtract 90 degrees.
        // ex: Target Up is (0,1). Atan2(1,0) = 90. 90-90 = 0.
        // ex: Target Up is (1,0). Atan2(0,1) = 0. 0-90 = -90.
        float targetAngle = Mathf.Atan2(targetUp.y, targetUp.x) * Mathf.Rad2Deg - 90f;

        // 3. Create Rotation
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // 4. Smooth Rotate
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    void FixedUpdate()
    {
        CheckGrounded();
        if (!isDashing && !isCharging && !isAttacking)
        {
            Move();
        }
    }

    void CheckGrounded()
    {
        if (groundCheck != null)
        {
            // Increased radius (0.2 -> 0.4) to ensure it hits ground even if pivot is slightly high
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.4f, groundLayer);
        }
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.4f);
        }
    }



    void Move()
    {
        Vector2 playerRight = transform.right; 
        Vector2 playerUp = transform.up;       

        Vector2 currentVelocity = rb.linearVelocity;

        float velY = Vector2.Dot(currentVelocity, playerUp); 

        Vector2 newVelocity = (playerRight * moveInput.x * moveSpeed) + (playerUp * velY);

        rb.linearVelocity = newVelocity;
    }

    void Jump()
    {
        Vector2 playerUp = transform.up;
        Vector2 playerRight = transform.right;
        
        Vector2 currentVel = rb.linearVelocity;
        
        float velX = Vector2.Dot(currentVel, playerRight);
        
        float gravity = Physics2D.gravity.magnitude * rb.gravityScale;
        float jumpVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
        
        rb.linearVelocity = (playerRight * velX) + (playerUp * jumpVelocity);
        
        transform.position += (Vector3)playerUp * 0.05f;
        isGrounded = false;
    }

    IEnumerator ThunderclapAndFlash(Vector2 dashDir)
    {
        // Phase 1: Charge (Preparation)
        isCharging = true;
        canDash = false;
        
        // Zero out velocity to freeze in place
        rb.linearVelocity = Vector2.zero;

        // ROTATE & FLIP Logic
        float angle = Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg;
        bool shouldFlip = false;
        Vector2 g = Physics2D.gravity.normalized;
        
        // Rules based on User Feedback:
        if (g.y > 0.5f) // Gravity UP
        {
            shouldFlip = (Mathf.Abs(angle) <= 90);
        }
        else if (Mathf.Abs(g.x) > 0.5f) // Gravity Horizontal
        {
            if (g.x < 0) // Gravity Left
            {
                shouldFlip = (angle >= 0);
            }
            else // Gravity Right
            {
                shouldFlip = (angle < 0);
            }
        }
        else // Gravity Down (Standard)
        {
            shouldFlip = (Mathf.Abs(angle) > 90);
        }

        // Determine active durations based on Sixfold Mode
        float activeChargeDuration = (sixfoldStacks > 0) ? 0f : chargeDuration;
        float activeCooldown = (sixfoldStacks > 0) ? 0f : dashCooldown;

        // Apply Transformation
        if (shouldFlip)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            transform.rotation = Quaternion.Euler(0, 0, angle - 180f);
        }
        else
        {
            transform.localScale = Vector3.one; 
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        // Active Wait (Skip if Sixfold)
        if (activeChargeDuration > 0)
        {
            float chargeTimer = 0f;
            while (chargeTimer < activeChargeDuration)
            {
                chargeTimer += Time.deltaTime;
                rb.linearVelocity = Vector2.zero; 
                yield return null;
            } 
        }

        isCharging = false;

        // Phase 2: Dash (Movement)
        isDashing = true; 

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; 
        
        // Penetration: Ignore Enemy Collision (Body specific)
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        int originalHitboxLayer = 0; // Cache

        if (attackCollider) 
        {
            // CRITICAL FIX: Temporary move Hitbox to "Default" layer.
            // Because IgnoreLayerCollision(Player, Enemy) prevents ALL events between those layers.
            // By moving Hitbox to "Default", it can still trigger events on "Enemy".
            originalHitboxLayer = attackCollider.layer;
            attackCollider.layer = 0; // Default Layer

            attackCollider.SetActive(true);
            DamageDealer dd = attackCollider.GetComponent<DamageDealer>();
            if(dd)
            {
                dd.damageAmount = thunderclapDamage; // 50
                dd.targetTag = "Enemy";
            }
        }
        
        // Start Particle System
        if (dashParticle != null)
        {
            dashParticle.Play();
        }

        float elapsedTime = 0f;
        
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            rb.linearVelocity = dashDir * dashSpeed; 
            yield return null;
        }

        // 3. Stop (Rest of cleanup)
        rb.linearVelocity = Vector2.zero; 
        rb.gravityScale = originalGravity;
        
        // Restore Collision
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        if (attackCollider) 
        {
            attackCollider.SetActive(false); 
            attackCollider.layer = originalHitboxLayer; // Restore
        }
        
        // Stop Particle System
        if (dashParticle != null)
        {
            dashParticle.Stop();
        }

        // Snap Rotation
        float targetZ = 0f;
        if (g.x > 0.5f) targetZ = 90f;      
        else if (g.x < -0.5f) targetZ = -90f;
        else if (g.y > 0.5f) targetZ = 180f; 
        
        transform.rotation = Quaternion.Euler(0, 0, targetZ);

        isDashing = false;

        if (sixfoldStacks > 0)
        {
            sixfoldStacks--;
            sixfoldExpireTimer = 1.0f; // Reset expire timer (extend chain window)
        }

        // Manual Cooldown for UI
        currentCooldownTimer = activeCooldown;
        while (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        
        canDash = true;
    }

    private IEnumerator PlayChargeCompleteEffect()
    {
        // Flash Sprite Yellow
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            Color original = sr.color;
            // Blink 3 times
            for(int i=0; i<3; i++)
            {
                sr.color = Color.yellow;
                yield return new WaitForSeconds(0.05f);
                sr.color = Color.white; // Flash White
                yield return new WaitForSeconds(0.05f);
            }
            sr.color = original;
        }
        
        // Optional: Play Particle Burst if assigned
        if (dashParticle) dashParticle.Play();
    }

    private IEnumerator PerformNormalAttack()
    {
        isAttacking = true;
        
        // Stop Movement
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;

        // Visuals
        if (animator) animator.SetTrigger("Attack");

        // Wait for animation impact frame
        yield return new WaitForSeconds(attackDelay);

        // Activate Hitbox
        if (normalAttackCollider)
        {
            normalAttackCollider.SetActive(true);
            DamageDealer dealer = normalAttackCollider.GetComponent<DamageDealer>();
            if (dealer) 
            {
                dealer.damageAmount = normalAttackDamage;
                dealer.targetTag = "Enemy"; // Ensure we hit Enemies
            }
        }

        yield return new WaitForSeconds(attackDuration);

        // Cleanup
        if (normalAttackCollider) normalAttackCollider.SetActive(false);
        isAttacking = false;
    }
}