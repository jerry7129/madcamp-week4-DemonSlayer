using UnityEngine;
using System.Collections;

public class GravityBoundEnemyAI : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Direction of gravity for THIS enemy (e.g., Down, Up, Left, Right)")]
    public Vector2 localGravityDirection = Vector2.down;
    public float gravityMagnitude = 30f; 

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float detectRange = 10f;
    public float attackRange = 1.0f;
    public float attackCooldown = 2.0f;

    [Header("Physics Tweaks")]
    public float maxSlopeAngle = 60f;
    public float maxAcceleration = 35f;
    public float maxAirAcceleration = 20f;
    public float downwardMovementMultiplier = 3f;
    public float upwardMovementMultiplier = 1.7f;
    public float jumpHeight = 3.0f;

    [Header("References")]
    public Transform target;
    public Animator animator;
    public GameObject attackHitbox;
    public Transform groundCheck;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    // State
    private bool isGrounded;
    private bool isOnSlope;
    private float moveInputX; 
    private bool isAttacking;
    private bool isDead;
    private float lastAttackTime = -99f;
    
    // Physics Calc
    private Vector2 slopeNormalPerp;
    private float slopeAngle;
    private RaycastHit2D slopeHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb)
        {
            rb.gravityScale = 0f; // Disable global gravity
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("SlipperyGravityEnemy");
        slipperyMat.friction = 0f;
        slipperyMat.bounciness = 0f;
        foreach(var c in GetComponents<Collider2D>()) c.sharedMaterial = slipperyMat;
        
        if (attackHitbox) attackHitbox.SetActive(false);
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if(p) target = p.transform;
            else
            {
                GameObject zen = GameObject.Find("Zenitsu");
                if(zen) target = zen.transform;
            }
        }
    }

    void Update()
    {
        if (isDead) return;
        if (target == null) return;

        AlignToGravity();

        // Calculate Relative Distances (Local Space)
        Vector2 diff = target.position - transform.position;
        float dist = diff.magnitude;
        
        // Project difference onto My Right (Horizontal) and My Up (Vertical)
        float horzDist = Mathf.Abs(Vector2.Dot(diff, transform.right));
        float vertDist = Mathf.Abs(Vector2.Dot(diff, transform.up));

        // Attack Condition: Close Horizontally AND reasonably close Vertically
        // Allow larger vertical gap (e.g., 2.5f) to hit players below/above
        if (horzDist <= attackRange && vertDist <= 2.5f)
        {
            moveInputX = 0;
            if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else if (dist <= detectRange && !isAttacking)
        {
            Vector2 dirToTarget = (target.position - transform.position).normalized;
            // "Right" is -90 deg from Gravity Down (Perpendicular)
            // But we can just use Transform.right since we adhere to AlignToGravity
            float dot = Vector2.Dot(dirToTarget, transform.right);
            moveInputX = (dot > 0) ? 1f : -1f;
            
            if (moveInputX > 0) transform.localScale = Vector3.one;
            else transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            moveInputX = 0;
        }

        if(animator)
        {
            animator.SetBool("isRunning", Mathf.Abs(moveInputX) > 0.1f);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        CheckGrounded();
        Move();
        
        // Apply Custom Gravity Logic MANUALLY provided NOT on slope (or stuck prevention)
        // Move() handles velocity setting directly for slopes.
        // If Move() did NOT return early (flat/air), we need to handle gravity.
    }

    void AlignToGravity()
    {
        Vector2 targetUp = -localGravityDirection.normalized;
        float targetAngle = Mathf.Atan2(targetUp.y, targetUp.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }

    void CheckGrounded()
    {
        isGrounded = false;
        slopeAngle = 0f;
        slopeNormalPerp = Vector2.zero;

        if (groundCheck == null) return;

        if (Physics2D.OverlapCircle(groundCheck.position, 0.4f, groundLayer)) isGrounded = true;

        Vector2 rayDir = localGravityDirection.normalized;
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, rayDir, 0.6f, groundLayer);

        if (hit)
        {
            float angle = Vector2.Angle(hit.normal, -rayDir);
            bool isStairs = hit.collider.CompareTag("Stairs");

            if (angle <= maxSlopeAngle || isStairs)
            {
                float maxDist = isStairs ? 0.8f : 0.6f;
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
        // Jump Logic (Moved to Top)
        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            // Height relative to MY up
            float relativeH = Vector2.Dot(target.position - transform.position, transform.up);

            // Condition: Grounded, Close enough, Target is higher, Moving
            if (isGrounded && dist <= detectRange && relativeH > 1.5f && Mathf.Abs(moveInputX) > 0)
            {
                 // 5% chance per frame to jump
                 if (Random.value < 0.05f) 
                 {
                     Jump();
                     return; // Override move
                 }
            }
        }

        isOnSlope = isGrounded && slopeAngle > 0;
        Vector2 playerRight = transform.right; 
        
        Vector2 currentVel = rb.linearVelocity;
        float currentSpeedX = Vector2.Dot(currentVel, playerRight);
        
        float targetSpeedX = moveInputX * moveSpeed;
        float accel = isGrounded ? maxAcceleration : maxAirAcceleration;
        float newSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeedX, accel * Time.deltaTime);

        // STAIRS/WALL CHECK (Ported from DoppelgangerAI)
        if (isGrounded && Mathf.Abs(moveInputX) > 0.1f)
        {
            Vector2 forwardDir = (playerRight * Mathf.Sign(moveInputX));
            Vector2 origin = (Vector2)transform.position + ((Vector2)transform.up * 0.2f); 
            RaycastHit2D wallHit = Physics2D.Raycast(origin, forwardDir, 0.6f, groundLayer);
            if (wallHit && wallHit.collider.CompareTag("Stairs"))
            {
                isOnSlope = true;
                slopeNormalPerp = Vector2.Perpendicular(wallHit.normal).normalized;
                // Verify Perp Direction relative to UP
                if (Vector2.Dot(slopeNormalPerp, transform.up) < 0) slopeNormalPerp = -slopeNormalPerp; 
            }
        }

        // bool appliedSlopeMove = false; // Unused

        if (isOnSlope)
        {
             if (Mathf.Abs(targetSpeedX) < 0.01f && Mathf.Abs(newSpeedX) < 0.01f)
             {
                 rb.linearVelocity = Vector2.zero;
                 return; // Stop & Stick
             }

             Vector2 direction = playerRight * Mathf.Sign(newSpeedX);
             if (newSpeedX == 0) direction = playerRight;

             if (Vector2.Dot(direction, slopeNormalPerp) < 0) slopeNormalPerp = -slopeNormalPerp;
             
             float slopeF = 1f;
             float hComp = Mathf.Abs(Vector2.Dot(slopeNormalPerp, playerRight));
             if (hComp > 0.01f) slopeF = 1f / hComp;
             slopeF = Mathf.Clamp(slopeF, 1f, 3f);

             Vector2 slopeVel = slopeNormalPerp * Mathf.Abs(newSpeedX) * slopeF;
             
             // Safety Check
             if (Mathf.Abs(targetSpeedX) > 0.1f && Mathf.Abs(slopeVel.x) < 0.01f)
             {
                 // Fallback to normal
             }
             else
             {
                 rb.linearVelocity = slopeVel;
                 // appliedSlopeMove = true; // Unused
                 return; // Successfully moved on slope, SKIP Gravity
             }
        }

        // If we are here, we are either NOT on slope, OR slope logic failed (fallback)
        // 4. Flat/Air Movement
        
        // Simulating Variable Gravity (DoppelgangerAI uses RB gravity scale, we use Force)
        Vector2 gDir = localGravityDirection.normalized;
        float vSpeed = Vector2.Dot(rb.linearVelocity, gDir); // Positive = Falling down

        float gravityMult = 1f;
        if (vSpeed < 0) gravityMult = upwardMovementMultiplier; // Moving UP (against gravity)
        else if (vSpeed > 0) gravityMult = downwardMovementMultiplier; // Falling DOWN

        // Apply Force for Gravity
        rb.AddForce(gDir * gravityMagnitude * gravityMult * rb.mass);

        // Apply Horizontal Velocity without killing Vertical Velocity
        Vector2 finalVel = rb.linearVelocity;
        
        // Remove old 'Right' component
        Vector2 verticalVel = finalVel - (playerRight * Vector2.Dot(finalVel, playerRight));
        rb.linearVelocity = verticalVel + (playerRight * newSpeedX);

        // Jump Logic moved to top

    }

    void Jump()
    {
        Vector2 jumpDir = -localGravityDirection.normalized;
        float jumpVel = Mathf.Sqrt(2f * gravityMagnitude * jumpHeight);
        
        Vector2 currentVel = rb.linearVelocity;
        // Project velocity onto jump plane (remove vertical velocity) to cleanly jump
        Vector2 lateralVel = currentVel - (jumpDir * Vector2.Dot(currentVel, jumpDir)); 
        
        rb.linearVelocity = lateralVel + (jumpDir * jumpVel);
        isGrounded = false;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Halt Movement but keep Gravity
        Vector2 gDir = localGravityDirection.normalized;
        float vSpeed = Vector2.Dot(rb.linearVelocity, gDir);
        rb.linearVelocity = gDir * vSpeed; 
        
        if (animator)
        {
            animator.SetBool("isRunning", false);
            animator.SetTrigger("Attack");
        }
        
        yield return new WaitForSeconds(0.1f);
        if (attackHitbox)
        {
            attackHitbox.SetActive(true);
            DamageDealer dd = attackHitbox.GetComponent<DamageDealer>();
            if(dd) dd.BeginAttack();
        }
        yield return new WaitForSeconds(0.4f);
        if (attackHitbox) attackHitbox.SetActive(false);
        isAttacking = false;
    }
}
