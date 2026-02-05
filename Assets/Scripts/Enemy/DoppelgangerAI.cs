using UnityEngine;

public class DoppelgangerAI : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 4f;
    public float detectRange = 15f;
    public float attackRange = 1.0f;
    public float attackCooldown = 2.0f;

    [Header("Movement Physics")]
    public float maxSlopeAngle = 60f;
    [Range(0f, 100f)] public float maxAcceleration = 35f;
    [Range(0f, 100f)] public float maxAirAcceleration = 20f;
    [Range(0f, 5f)] public float downwardMovementMultiplier = 3f;
    [Range(0f, 5f)] public float upwardMovementMultiplier = 1.7f;
    public float jumpHeight = 3.0f;
    public float jumpCooldown = 0.2f; // New
    private float lastJumpTime = -1f;

    [Header("References")]
    public Transform target;
    public Animator animator;
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public GameObject attackHitbox; // Child NormalAttackCollider
    public Transform groundCheck;
    public LayerMask groundLayer;
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackClip;
    public AudioClip skillClip;

    // State
    private bool isDead = false;
    private bool isPaused = false; 
    private bool isAttacking = false;
    private float lastAttackTime = -99f;
    
    // Physics State
    private bool isGrounded;
    private float slopeAngle;
    private Vector2 slopeNormalPerp;
    private bool isOnSlope;
    private RaycastHit2D slopeHit;
    private float defaultGravityScale;
    private float moveInputX; // Controlled by AI Logic

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (rb) 
        {
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Added for safety
            defaultGravityScale = rb.gravityScale;
        }
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        // Fix Sticking: Apply Zero Friction
        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("SlipperyEnemy");
        slipperyMat.friction = 0f;
        slipperyMat.bounciness = 0f;
        
        Collider2D[] colls = GetComponents<Collider2D>();
        foreach(var c in colls) c.sharedMaterial = slipperyMat;
    }

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player) target = player.transform;
            else
            {
                GameObject p = GameObject.Find("Zenitsu");
                if (p) target = p.transform;
            }
        }

        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (attackHitbox && attackHitbox.activeSelf) attackHitbox.SetActive(false);
    }

    public void PauseAI(float duration)
    {
        StartCoroutine(PauseRoutine(duration));
    }

    System.Collections.IEnumerator PauseRoutine(float duration)
    {
        isPaused = true;
        if(rb) rb.linearVelocity = Vector2.zero; 
        moveInputX = 0;
        if(animator) animator.SetBool("isRunning", false);
        yield return new WaitForSeconds(duration);
        isPaused = false;
    }

    void AlignToGravity()
    {
        Vector2 g = Physics2D.gravity;
        if (g == Vector2.zero) return; 

        Vector2 targetUp = -g.normalized;
        float targetAngle = Mathf.Atan2(targetUp.y, targetUp.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    void Update()
    {
        if (isDead || isPaused) return;
        
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player) target = player.transform;
            return;
        }

        AlignToGravity();

        // --- AI LOGIC (Decision Making) ---
        float dist = Vector3.Distance(transform.position, target.position);
        
        // Attack Logic (Melee)
        if (dist <= attackRange)
        {
            moveInputX = 0; // Stop moving to attack
            if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                StartCoroutine(AttackRoutine(false)); // Normal Attack
            }
        }
        // Skill Logic (Ranged)
        else if (dist <= skillRange)
        {
             // Only stop if we are actually attacking or about to attack
             if (Time.time >= lastSkillTime + skillCooldown && !isAttacking)
             {
                 moveInputX = 0;
                 StartCoroutine(AttackRoutine(true)); // Skill Attack
             }
             // Otherwise continue chasing if not attacking (handled below)
        }
        // Chase Logic
        else if (dist <= detectRange && !isAttacking)
        {
            // Determine Direction
            Vector2 dirToTarget = (target.position - transform.position).normalized;
            Vector2 localRight = transform.right; 
            float dot = Vector2.Dot(dirToTarget, localRight);
            moveInputX = (dot > 0) ? 1f : -1f;

            // Visual Flip
            if (moveInputX > 0) transform.localScale = Vector3.one; 
            else transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            moveInputX = 0; // Idle
        }

        // Animation
        if (animator)
        {
            animator.SetBool("isRunning", Mathf.Abs(moveInputX) > 0.1f);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        if (isDead || isPaused) return;

        CheckGrounded();
        Move();
    }

    void CheckGrounded()
    {
        isGrounded = false;
        slopeAngle = 0f;
        slopeHit = new RaycastHit2D();

        if (groundCheck == null) return;

        // 1. Circle Check
        bool circleHit = Physics2D.OverlapCircle(groundCheck.position, 0.15f, groundLayer); // Reduced radius
        if (circleHit) isGrounded = true;

        // 2. Slope Raycast
        Vector2 gravityDown = -transform.up; 
        if (Physics2D.gravity != Vector2.zero) gravityDown = Physics2D.gravity.normalized;

        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, gravityDown, 0.6f, groundLayer);
        
        if (hit)
        {
            float angle = Vector2.Angle(hit.normal, -gravityDown);
            bool isStairs = hit.collider.CompareTag("Stairs");

            if (angle <= maxSlopeAngle || isStairs)
            {
                 float maxDist = isStairs ? 0.8f : 0.5f;
                 if (hit.distance < maxDist) 
                 {
                     isGrounded = true;
                     slopeAngle = angle;
                     slopeHit = hit;
                     slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
                 }
            }
        }
    }

    void Move()
    {
        isOnSlope = isGrounded && slopeAngle > 0;

        Vector2 playerRight = transform.right; 
        
        // 1. Calculate Current Local Velocity
        Vector2 currentVelocity = rb.linearVelocity;
        float currentSpeedX = Vector2.Dot(currentVelocity, playerRight);
        
        // 2. Calculate Target Speed & Acceleration
        float targetSpeedX = moveInputX * moveSpeed;
        float acceleration = isGrounded ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;
        
        float newSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeedX, maxSpeedChange);
        
        // STAIRS/WALL CHECK (Forward Probe like Player)
        if (isGrounded && Mathf.Abs(moveInputX) > 0.1f)
        {
            Vector2 forwardDir = (playerRight * Mathf.Sign(moveInputX));
            Vector2 origin = (Vector2)transform.position + (Vector2.up * 0.2f); 
            RaycastHit2D wallHit = Physics2D.Raycast(origin, forwardDir, 0.6f, groundLayer);
            if (wallHit && wallHit.collider.CompareTag("Stairs"))
            {
                isOnSlope = true;
                slopeNormalPerp = Vector2.Perpendicular(wallHit.normal).normalized;
                if (slopeNormalPerp.y < 0) slopeNormalPerp = -slopeNormalPerp; // Force Up
            }
        }

        // 3. Slope Movement
        if (isOnSlope)
        {
            // Snap to zero if stopping
            if (Mathf.Abs(targetSpeedX) < 0.01f && Mathf.Abs(newSpeedX) < 0.01f)
            {
                 rb.linearVelocity = Vector2.zero;
                 rb.gravityScale = 0f;
                 return;
            }
            
            rb.gravityScale = defaultGravityScale; 

            Vector2 direction = (playerRight * Mathf.Sign(newSpeedX));
            if (newSpeedX == 0) direction = playerRight;

            if (Vector2.Dot(direction, slopeNormalPerp) < 0)
            {
                slopeNormalPerp = -slopeNormalPerp;
            }
            
            float slopeFactor = 1f;
            float horizontalComponent = Mathf.Abs(Vector2.Dot(slopeNormalPerp, playerRight));
            
            if (horizontalComponent > 0.01f) 
            {
                slopeFactor = 1f / horizontalComponent;
            }
            slopeFactor = Mathf.Clamp(slopeFactor, 1f, 3f);

            Vector2 slopeVelocity = slopeNormalPerp * Mathf.Abs(newSpeedX) * slopeFactor;

            // SAFETY CHECK: Prevent sticking on vertical seams
            if (Mathf.Abs(targetSpeedX) > 0.1f && Mathf.Abs(slopeVelocity.x) < 0.01f)
            {
                // Fallback to normal movement
            }
            else
            {
                rb.linearVelocity = slopeVelocity;
                return; // Only return if successful slope move
            }
        }
        else
        {
            // 4. Flat/Air Movement
            rb.gravityScale = defaultGravityScale;
            
            // Variable Jump Gravity (If needed logic later, for now just multipliers)
            Vector2 gravityDir = Physics2D.gravity.normalized;
            if (gravityDir == Vector2.zero) gravityDir = Vector2.down;
            float verticalSpeed = Vector2.Dot(rb.linearVelocity, gravityDir);

            if (verticalSpeed < 0) // Moving Up (since gravityDir is down)
                rb.gravityScale = defaultGravityScale * upwardMovementMultiplier;
            else if (verticalSpeed > 0) // Falling Down
                rb.gravityScale = defaultGravityScale * downwardMovementMultiplier;

            // Apply Velocity
            Vector2 finalVelocity = rb.linearVelocity;
            
            // Remove old x component and add new x component
            // We use vector rejection/projection math or just reconstruct
            Vector2 verticalVelocity = finalVelocity - (playerRight * Vector2.Dot(finalVelocity, playerRight));
            rb.linearVelocity = verticalVelocity + (playerRight * newSpeedX);
        }

        // Jump Logic (Simple AI Jump)
        // Check if we need to jump to reach target (target is higher)
        Vector2 gDir = Physics2D.gravity.normalized;
        if(gDir == Vector2.zero) gDir = Vector2.down;
        
        float relativeHeight = Vector2.Dot(target.position - transform.position, -gDir);
        
        if (isGrounded && relativeHeight > 1.5f && Mathf.Abs(moveInputX) > 0.1f && Time.time >= lastJumpTime + jumpCooldown)
        {
             // Jump!
             lastJumpTime = Time.time;
             float gMag = Physics2D.gravity.magnitude * defaultGravityScale;
             float jumpVel = Mathf.Sqrt(2 * gMag * jumpHeight); // v = sqrt(2gh)
             
             // Add upward velocity (Opposite to gravity)
             rb.linearVelocity += (-gDir * jumpVel);
             isGrounded = false;
        }
    }

    [Header("Combat")]
    public float skillRange = 10f;
    public float skillCooldown = 5f;
    private float lastSkillTime = -99f;

    public GameObject swordProjectilePrefab;
    public Transform firePoint; 
    [Tooltip("Number of projectiles per attack")]
    public int projectilesPerAttack = 3;
    [Tooltip("Delay between projectiles")]
    public float projectileInterval = 0.2f;

    // ... existing code ...

    // Update Decision Logic
    // In Update():
    /*
        // Attack Logic (Melee)
        if (dist <= attackRange && Time.time >= lastAttackTime + attackCooldown && !isAttacking)
        {
            StartCoroutine(AttackRoutine(false)); // Normal Attack
        }
        // Skill Logic (Ranged)
        else if (dist <= skillRange && Time.time >= lastSkillTime + skillCooldown && !isAttacking)
        {
            StartCoroutine(AttackRoutine(true)); // Skill Attack
        }
    */

    System.Collections.IEnumerator AttackRoutine(bool isSkill)
    {
        isAttacking = true;
        
        rb.linearVelocity = Vector2.zero;
        if(animator) animator.SetBool("isRunning", false);

        // Face Target
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        float dot = Vector2.Dot(dirToTarget, transform.right);
        if (dot > 0) transform.localScale = Vector3.one;
        else transform.localScale = new Vector3(-1, 1, 1);

        if (isSkill)
        {
            // --- SKILL ATTACK (Projectiles) ---
            lastSkillTime = Time.time;
            
            // Fire Projectiles
            if (swordProjectilePrefab != null)
            {
                // Play Skill Sound ONCE at start
                if (audioSource && skillClip) audioSource.PlayOneShot(skillClip);

                for (int i = 0; i < projectilesPerAttack; i++)
                {
                    if (animator) 
                    {
                        animator.ResetTrigger("Attack"); // Ensure Attack doesn't play
                        animator.SetTrigger("SkillAttack"); // Trigger per projectile
                    }
                    yield return new WaitForSeconds(0.3f); // Windup per projectile

                    FireProjectile();
                    yield return new WaitForSeconds(projectileInterval);
                }
            }
            
            yield return new WaitForSeconds(0.5f); // Recovery
        }
        else
        {
            // --- NORMAL ATTACK (Melee) ---
            lastAttackTime = Time.time;
            if(animator) animator.SetTrigger("Attack");

            // Play Normal Attack Sound
            if (audioSource && attackClip) audioSource.PlayOneShot(attackClip);

            yield return new WaitForSeconds(0.1f); // Windup

            if(attackHitbox) 
            {
                DamageDealer dd = attackHitbox.GetComponent<DamageDealer>();
                if(dd) dd.BeginAttack();
                attackHitbox.SetActive(true);
            }

            yield return new WaitForSeconds(0.3f); // Active Duration

            if(attackHitbox) attackHitbox.SetActive(false);
        }

        isAttacking = false;
    }

    void FireProjectile()
    {
        Vector3 spawnPos = transform.position; // Changed to Vector3
        if (firePoint != null) spawnPos = firePoint.position;
        else spawnPos = transform.position + (transform.right * (transform.localScale.x > 0 ? 1 : -1) * 1.0f);

        GameObject proj = Instantiate(swordProjectilePrefab, spawnPos, Quaternion.identity);
        
        // 1. Determine Target Direction
        Vector3 dir;
        if (target != null)
        {
            dir = (target.position - spawnPos).normalized;
        }
        else
        {
            // If no target, fire in facing direction
            dir = (transform.localScale.x > 0) ? Vector3.right : Vector3.left;
        }

        // 2. Calculate Rotation (Z-Angle)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        proj.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 3. Handle Visual Orientation (Gravity Aware)
        // We want the projectile's 'Up' to align somewhat with the World's 'Up' (Gravity Up).
        // If Gravity is inverted, the projectile should look upside down relative to World (Upright relative to Boss).
        
        Vector2 gravityUp = Physics2D.gravity.magnitude > 0.1f ? -Physics2D.gravity.normalized : Vector2.up;
        Vector2 projectileUp = proj.transform.rotation * Vector3.up; 

        // Check alignment
        bool shouldFlipY = Vector2.Dot(projectileUp, gravityUp) < -0.1f; // Threshold to avoid flickering at 90 deg

        Vector3 defaultScale = proj.transform.localScale;
        defaultScale = new Vector3(Mathf.Abs(defaultScale.x), Mathf.Abs(defaultScale.y), defaultScale.z);

        if (shouldFlipY)
        {
             defaultScale.y = -defaultScale.y; 
        }
        
        proj.transform.localScale = defaultScale;
    }
    
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.15f);
        }
    }
}
