using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 8f;
    public float jumpHeight = 5f; // Desired height in Units (v = sqrt(2gh))
    public float jumpCooldown = 0.2f; // Prevent double-trigger/super jump
    private float lastJumpTime = -1f;
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
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackClip;
    public AudioClip dashClip;
    
    public GameObject normalAttackCollider;
    public int normalAttackDamage = 25;
    public int thunderclapDamage = 50; // New: Higher damage for Skill
    public float attackDelay = 0.05f; // Faster hitbox 
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
    private bool isDead = false; // Track death state locally

    // Respawn Data
    private Vector3 initialPosition;

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
        initialPosition = transform.position; // Cache start pos

        if(rb) 
        {
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep; 
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            defaultGravityScale = rb.gravityScale; 
        }
        animator = GetComponent<Animator>(); 
        
        // 1. Fix Rotation
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 2. Fix Sticking
        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("Slippery");
        slipperyMat.friction = 0f;
        slipperyMat.bounciness = 0f;
        
        Collider2D[] colls = GetComponents<Collider2D>();
        foreach(var c in colls) c.sharedMaterial = slipperyMat;

        // 3. Auto-assign GroundCheck
        if (groundCheck == null)
        {
            Transform foundCheck = transform.Find("GroundCheck");
            if (foundCheck != null) groundCheck = foundCheck;
        }

        // 4. Auto-assign GroundLayer
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Default", "Ground", "Wall"); 
            if(groundLayer.value == 0) groundLayer = -1; 
        }

        // 5. Ensure components are off
        if(attackCollider) attackCollider.SetActive(false);
        if(normalAttackCollider) normalAttackCollider.SetActive(false);

        // 6. Cache Layers
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    public void Respawn()
    {
        isDead = false;
        controlsPaused = false;
        isDashing = false;
        isAttacking = false;
        
        // Reset Position
        transform.position = initialPosition;
        rb.linearVelocity = Vector2.zero;
        SnapToGravity(); 

        // Reset Animation
        if(animator)
        {
            animator.Rebind(); 
            animator.Update(0f); 
        }
        
        // Reset Colliders
        if(attackCollider) attackCollider.SetActive(false);
        if(normalAttackCollider) normalAttackCollider.SetActive(false);
        
        // Reset Effects
        if(dashParticle) dashParticle.Stop();
        isCharging = false;
        canDash = true;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        
        // Stop Movement
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        
        // Cancel Charge/Dash
        isCharging = false;
        isDashing = false;
        if(dashParticle) dashParticle.Stop();
        if(attackCollider) attackCollider.SetActive(false);
        if(normalAttackCollider) normalAttackCollider.SetActive(false);

        if(animator) animator.SetTrigger("Die");
    }

    public void PauseControls(float duration)
    {
        StartCoroutine(PauseControlsRoutine(duration));
    }

    IEnumerator PauseControlsRoutine(float duration)
    {
        controlsPaused = true;
        moveInput = Vector2.zero; 
        rb.linearVelocity = Vector2.zero; 
        
        yield return new WaitForSeconds(duration);
        
        if(!isDead) controlsPaused = false;
    }

    // Sixfold State
    private int sixfoldStacks = 0;
    private float sixfoldExpireTimer = 0f;
    private float cKeyHoldTime = 0f;
    private bool cKeyWasPressed = false;
    private bool cKeyReadyFeedbackPlayed = false;

    void Update()
    {
        // 0. Dead Check
        if (isDead) return;

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
                if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
                {
                    Jump();
                    lastJumpTime = Time.time;
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
                    if (cKeyHoldTime >= 1.0f && !cKeyReadyFeedbackPlayed)
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
                        if (cKeyHoldTime >= 1.0f)
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

            // 2. Thunderclap Input (Left Mouse Click) w/ BUFFERING
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                lastAttackInputTime = Time.time; // Buffer the input
            }

            // Execute Buffered Attack
            if (Time.time - lastAttackInputTime < inputBufferWindow && canDash)
            {
               Camera cam = Camera.main;
               if (cam == null) cam = FindFirstObjectByType<Camera>(); 

               if (cam != null)
               {
                   lastAttackInputTime = -99f; // Consume buffer

                   Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                   mouseWorldPos.z = transform.position.z; 
                   Vector2 dashDir = (mouseWorldPos - transform.position).normalized;

                   StartCoroutine(ThunderclapAndFlash(dashDir));
               }
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

        // 5. Cooldown Management
        if (!canDash && !isCharging && !isDashing) // Only recover when not doing the move
        {
            currentCooldownTimer += Time.deltaTime;
            // Removed spammy log, but if needed: 
            // Debug.Log($"Cooldown: {currentCooldownTimer}/{dashCooldown}"); 
            
            if (currentCooldownTimer >= dashCooldown)
            {
                canDash = true;
                currentCooldownTimer = 0f;
            }
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

    void Start()
    {
        // ... (existing Start code if any, though currently Awake is used mostly)
        // Force Snap Rotation to Gravity on Start (No Slerp)
        SnapToGravity();
    }

    void AlignToGravity()
    {
        // 1. Determine "Up" based on Gravity
        Vector2 g = Physics2D.gravity;
        if (g == Vector2.zero) return; 

        Vector2 targetUp = -g.normalized;

        // 2. Calculate Target Angle (Degrees)
        float targetAngle = Mathf.Atan2(targetUp.y, targetUp.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // 3. Smooth Rotate
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    public void SnapToGravity()
    {
        Vector2 g = Physics2D.gravity;
        if (g == Vector2.zero) return; 

        Vector2 targetUp = -g.normalized;
        float targetAngle = Mathf.Atan2(targetUp.y, targetUp.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        CheckGrounded();
        if (!isDashing && !isCharging && !isAttacking)
        {
            Move();
        }
    }

    [Header("Movement Settings")]
    public float maxSlopeAngle = 60f;
    [Range(0f, 100f)] public float maxAcceleration = 35f; // New: Acceleration
    [Range(0f, 100f)] public float maxAirAcceleration = 20f; // New: Air Control
    [Range(0f, 5f)] public float downwardMovementMultiplier = 3f; // New: Heavy fall
    [Range(0f, 5f)] public float upwardMovementMultiplier = 1.7f; // New: Low jump gravity
    
    private Vector2 slopeNormalPerp;
    private bool isOnSlope;
    private float slopeLockoutTimer = 0f;
    
    // Input Buffer
    private float lastAttackInputTime = -99f;
    private const float inputBufferWindow = 0.2f;

    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;
        slopeAngle = 0f;
        slopeHit = new RaycastHit2D();

        // 1. Standard Circle Check (Flat ground)
        if (groundCheck != null)
        {
            // Reduced radius from 0.4f to 0.15f to prevent detecting walls as ground
            bool circleHit = Physics2D.OverlapCircle(groundCheck.position, 0.15f, groundLayer);
            if (circleHit) 
            {
                isGrounded = true;
            }
        }

        // 2. Slope Raycast Check (If strictly needed or to override Air state on slopes)
        // Always cast ray to get slope info
        Vector2 gravityDown = -transform.up; 
        if (Physics2D.gravity != Vector2.zero) gravityDown = Physics2D.gravity.normalized;

        Debug.DrawRay(groundCheck.position, gravityDown * 0.6f, Color.blue); // Visualize Ray
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, gravityDown, 0.6f, groundLayer);
        
        if (hit)
        {
            float angle = Vector2.Angle(hit.normal, -gravityDown);
            
            // Check for Stairs Tag (Allow explicit stair climbing regardless of angle)
            bool isStairs = hit.collider.CompareTag("Stairs");

            if (angle <= maxSlopeAngle || isStairs)
            {
                 // If we are close enough to the hit point, consider grounded even if Circle missed
                 // For stairs, we might want to be more lenient with distance
                 float maxDist = isStairs ? 0.8f : 0.5f;
                 
                 if (hit.distance < maxDist) 
                 {
                     // KEY FIX: Only snap/ground via Raycast if NOT in Jump Lockout
                     // This prevents "Double Jumping" and "Velocity Killing" on start of jump
                     if (slopeLockoutTimer <= 0)
                     {
                         isGrounded = true;
                     }

                     // Keep tracking slope data anyway for smooth transitions
                     slopeAngle = angle;
                     slopeHit = hit;
                     slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
                 }
            }
        }
    }
    
    // Cached slope data
    private float slopeAngle;
    private RaycastHit2D slopeHit;

    void Move()
    {
        // Decrease Lockout
        if (slopeLockoutTimer > 0) 
        {
            slopeLockoutTimer -= Time.deltaTime;
            isOnSlope = false;
        }
        else
        {
             // Determine if we are effectively on a slope
             isOnSlope = isGrounded && slopeAngle > 0;
        }

        Vector2 playerRight = transform.right; 
        Vector2 playerUp = transform.up;       

        // 1. Calculate Current Local Velocity
        Vector2 currentVelocity = rb.linearVelocity;
        float currentSpeedX = Vector2.Dot(currentVelocity, playerRight);
        
        // 2. Calculate Target Speed & Acceleration
        float targetSpeedX = moveInput.x * moveSpeed;
        float newSpeedX;
        
        // MOMENTUM PRESERVATION (Air Attack)
        if (isAttacking && !isGrounded)
        {
            newSpeedX = currentSpeedX; // Preserve momentum
        }
        else
        {
            float acceleration = isGrounded ? maxAcceleration : maxAirAcceleration;
            float maxSpeedChange = acceleration * Time.deltaTime;
        
            newSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeedX, maxSpeedChange);
        }

        // STAIRS/WALL CHECK (Forward Probe)
        // If we are grounded and moving, check for stairs in front
        if (isGrounded && Mathf.Abs(moveInput.x) > 0.1f)
        {
            Vector2 forwardDir = (playerRight * Mathf.Sign(moveInput.x));
            // Cast from slightly above feet to hit the step face
            Vector2 origin = (Vector2)transform.position + (Vector2.up * 0.2f); 
            
            // Debug
            Debug.DrawRay(origin, forwardDir * 0.6f, Color.yellow);
            
            RaycastHit2D wallHit = Physics2D.Raycast(origin, forwardDir, 0.6f, groundLayer);
            if (wallHit && wallHit.collider.CompareTag("Stairs"))
            {
                // We hit a stair step face! Treat as steep slope.
                isOnSlope = true;
                slopeNormalPerp = Vector2.Perpendicular(wallHit.normal).normalized;
                
                // IMPORTANT: If facing wall (normal opposes dir), Perpendicular might be Down-ish or Up-ish.
                // Wall Normal is (-1, 0). Perp is (0, -1) or (0, 1).
                // We want UP.
                // Let's force alignment upward for stairs
                if (slopeNormalPerp.y < 0) slopeNormalPerp = -slopeNormalPerp;
            }
        }
        
        // 3. Slope Movement
        if (isOnSlope && slopeLockoutTimer <= 0)
        {
             // If we are effectively "stopped" (very low speed input and actual speed), snap to 0
             // KEY FIX: Only stick if slope is steep enough (> 5 deg). Don't stick on flat tiles (0~tiny)
             if (slopeAngle > 5f && Mathf.Abs(targetSpeedX) < 0.01f && Mathf.Abs(newSpeedX) < 0.01f)
             {
                 rb.linearVelocity = Vector2.zero;
                 rb.gravityScale = 0f; // Disable gravity to stick
                 return;
             }
             
             rb.gravityScale = defaultGravityScale; // Ensure gravity is on when moving

             // Flip tangent to match desired direction (based on newSpeedX sign)
             // Use newSpeedX for direction to preserve momentum direction
             Vector2 direction = (playerRight * Mathf.Sign(newSpeedX));
             if (newSpeedX == 0) direction = playerRight; // Default

             if (Vector2.Dot(direction, slopeNormalPerp) < 0)
             {
                 slopeNormalPerp = -slopeNormalPerp;
             }
             
             // Compensate for slope angle to maintain horizontal speed
             float slopeFactor = 1f;
             // slopeNormalPerp.x is the cosine of the angle with the horizontal (if using local right)
             // Check dot product with playerRight to get the horizontal component magnitude
             float horizontalComponent = Mathf.Abs(Vector2.Dot(slopeNormalPerp, playerRight));
             
             if (horizontalComponent > 0.01f) // Threshold slightly lowered for safety
             {
                 slopeFactor = 1f / horizontalComponent;
             }
             
             // Clamp to prevent excessive speed on very steep slopes/vertical stairs
             // 3f allows for slopes up to ~70 degrees to maintain full horizontal speed
             slopeFactor = Mathf.Clamp(slopeFactor, 1f, 3f);

             // Apply Velocity aligned with Slope
             Vector2 slopeVelocity = slopeNormalPerp * Mathf.Abs(newSpeedX) * slopeFactor;

             // SAFETY CHECK: If we are trying to move, but slope math creates a vertical-only vector (Stuck on wall/seam)
             // Fallback to normal movement to push through or slide off.
             if (Mathf.Abs(targetSpeedX) > 0.1f && Mathf.Abs(slopeVelocity.x) < 0.01f)
             {
                 // Debug.Log("Slope Stuck Detected! Fallback to standard Physics.");
                 // Fall through to Section 4 (Normal/Air Movement)
             }
             else
             {
                 rb.linearVelocity = slopeVelocity;
                 // Debug
                 Debug.DrawRay(transform.position, slopeVelocity, Color.green);
                 return;
             }
        }

        // 4. Normal / Air Movement
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Ensure loose constraints
        
        float velY = Vector2.Dot(currentVelocity, playerUp); 
        
        // PREVENT SLOPE LAUNCH:
        // If we are grounded (flat ground or transition) and not jumping, 
        // kill any residual upward momentum from slopes/stairs.
        if (isGrounded && slopeLockoutTimer <= 0 && velY > 0)
        {
            velY = 0;
        }

        // Variable Gravity Logic (Local Space)
        if(velY > 0.1f)
        {
            rb.gravityScale = defaultGravityScale * upwardMovementMultiplier;
        }
        else if(velY < -0.1f)
        {
            rb.gravityScale = defaultGravityScale * downwardMovementMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }

        Vector2 newVelocity = (playerRight * newSpeedX) + (playerUp * velY);

        rb.linearVelocity = newVelocity;
    }

    private float defaultGravityScale = 3f; // Cache this in Awake

    void Jump()
    {
        // Unlock constraints before applying force
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        Vector2 playerUp = transform.up;
        Vector2 playerRight = transform.right;
        
        Vector2 currentVel = rb.linearVelocity;
        
        float velX = Vector2.Dot(currentVel, playerRight);
        
        float gravity = Physics2D.gravity.magnitude * rb.gravityScale;
        float jumpVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
        
        rb.linearVelocity = (playerRight * velX) + (playerUp * jumpVelocity);
        
        transform.position += (Vector3)playerUp * 0.05f;
        isGrounded = false;
        slopeLockoutTimer = 0.2f; // Disable slope snapping for 0.2s
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

        if (audioSource && dashClip) audioSource.PlayOneShot(dashClip);

        float elapsedTime = 0f;
        
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // PREDICTIVE COLLISION CHECK
            // Cast a ray forward to see if we are about to hit a wall in this frame
            float checkDist = (dashSpeed * Time.deltaTime) + 0.5f; 
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDir, checkDist, groundLayer);

            if (hit && !hit.collider.isTrigger)
            {
                // We are about to hit a wall! 
                // Stop immediately to prevent tunneling/clipping.
                rb.linearVelocity = Vector2.zero;
                
                // Optional: Snap to just in front of the wall
                transform.position = hit.point - (dashDir * 0.4f); 
                break; // Exit Dash Loop
            }

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

        // Manual Cooldown Logic - DELEGATE TO UPDATE
        // Remove conflicting while-loop logic.
        // Update() counts UP. Coroutine should just exit and let Update take over.
        
        if (sixfoldStacks > 0)
        {
            // Instant Reset for Sixfold
            canDash = true;
            currentCooldownTimer = 0f; // UI Ready
        }
        else
        {
            // Normal Mode: Maintain cooldown state
            // Ensure timer starts at 0 so Update can count it UP to dashCooldown
            currentCooldownTimer = 0f; 
            // canDash remains false here. It will become true in Update() when timer >= limit.
        }
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
        
        isAttacking = true;
        
        // Stop Movement ONLY if Grounded (DISABLED: Requested Sliding/Inertia on Ground too)
        // if (isGrounded)
        // {
        //     rb.linearVelocity = Vector2.zero;
        //     moveInput = Vector2.zero;
        // }

        // Visuals
        if (animator) animator.SetTrigger("Attack");

        // Play Sound
        if (audioSource && attackClip) audioSource.PlayOneShot(attackClip);

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