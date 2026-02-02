using UnityEngine;

public class DoppelgangerAI : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 4f;
    public float detectRange = 15f;
    public float attackRange = 1.0f;
    public float attackCooldown = 2.0f;

    [Header("References")]
    public Transform target;
    public Animator animator;
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public GameObject attackHitbox; // Child NormalAttackCollider

    private bool isDead = false;
    private bool isPaused = false; 
    private bool isAttacking = false;
    private float lastAttackTime = -99f;

    void Awake()
    {
        // Init components early
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        // Set Color in Awake so EnemyHealth.Start() catches this modified color as 'original'
        if (spriteRenderer) spriteRenderer.color = new Color(0.3f, 0.3f, 0.5f, 1f); 

        // Fix Sticking: Apply Zero Friction
        PhysicsMaterial2D slipperyMat = new PhysicsMaterial2D("SlipperyEnemy");
        slipperyMat.friction = 0f;
        slipperyMat.bounciness = 0f;
        
        Collider2D[] colls = GetComponents<Collider2D>();
        foreach(var c in colls) c.sharedMaterial = slipperyMat;
    }

    void Start()
    {
        // ... (existing Start) ...
        // Auto-find player if not set
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player) target = player.transform;
            else
            {
                // Fallback name
                GameObject p = GameObject.Find("Zenitsu");
                if (p) target = p.transform;
            }
        }

        // 1. Prevent falling over (Lying down bug) - But we will MANUALLY rotate to gravity
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void PauseAI(float duration)
    {
        StartCoroutine(PauseRoutine(duration));
    }

    System.Collections.IEnumerator PauseRoutine(float duration)
    {
        isPaused = true;
        
        // Optional: Reset velocity here too, or let GravityManager do it
        if(rb) rb.linearVelocity = Vector2.zero; 
        if(animator) animator.SetBool("isRunning", false);

        yield return new WaitForSeconds(duration);
        isPaused = false;
    }

    // Old Update method removed


    void AlignToGravity()
    {
        Vector2 g = Physics2D.gravity;
        if (g == Vector2.zero) return; 

        Vector2 targetUp = -g.normalized;
        float targetAngle = Mathf.Atan2(targetUp.y, targetUp.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    [Header("Movement Settings")]
    public float jumpHeight = 3.0f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;

    void ChasePlayer()
    {
        // Direction to target
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        
        // Convert to Local Space based on our "Up" (which is aligned to gravity)
        Vector2 localRight = transform.right; 
        float dot = Vector2.Dot(dirToTarget, localRight);

        // Calculate move direction (-1 or 1)
        float moveDir = (dot > 0) ? 1f : -1f;

        // Apply Velocity Logic corrected for Gravity Drift
        Vector2 gravityDir = Physics2D.gravity.normalized;
        if (gravityDir == Vector2.zero) gravityDir = Vector2.down; 
        
        Vector2 groundTangent = new Vector2(-gravityDir.y, gravityDir.x); 
        Vector2 currentVel = rb.linearVelocity;
        float verticalSpeed = Vector2.Dot(currentVel, gravityDir); 

        Vector2 desiredDir = (localRight * moveDir).normalized;
        
        if (Vector2.Dot(desiredDir, groundTangent) < 0) groundTangent = -groundTangent;

        Vector2 horizontalVel = groundTangent * moveSpeed;
        
        // Jump Logic (If target is higher & we are grounded)
        // Check height difference relative to Gravity Up
        float heightDiff = Vector2.Dot(target.position - transform.position, -gravityDir);
        
        if (heightDiff > 1.5f && isGrounded)
        {
            // Calculate Jump Velocity: v = sqrt(2gh)
            float gMag = Physics2D.gravity.magnitude * rb.gravityScale;
            float jumpVel = Mathf.Sqrt(2 * gMag * jumpHeight);
            
            // Apply Jump (Replace vertical speed)
            verticalSpeed = -jumpVel; // -gravityDir is "Up", so negative gravityDir is Up velocity? 
            // Wait, gravityDir is DOWN. So Up velocity is OPPOSITE to gravityDir.
            // verticalSpeed is PROJECTION on gravityDir.
            // If we look at line 169: rb.velocity = horizontal + (gravityDir * verticalSpeed)
            // if verticalSpeed is positive, we move DOWN.
            // We want to move UP, so verticalSpeed should be NEGATIVE of JumpVel.
            verticalSpeed = -jumpVel; 
            
            isGrounded = false; // Prevent double jump immediately
            if(animator) animator.SetBool("isGrounded", false);
        }

        // 5. Combine
        rb.linearVelocity = horizontalVel + (gravityDir * verticalSpeed);

        // Animate
        if(animator) 
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isGrounded", isGrounded);
        }

        // Face Target (Visual Flip)
        if (moveDir > 0) transform.localScale = Vector3.one; 
        else transform.localScale = new Vector3(-1, 1, 1);
    }
    
    void Update()
    {
        if (isDead) return;
        if (isPaused) return; 
        
        if (target == null)
        {
             GameObject player = GameObject.FindWithTag("Player");
            if (player) target = player.transform;
            return;
        }

        AlignToGravity();
        CheckGrounded(); // Update ground state

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                // Wait/Cooldown state
                if(animator) animator.SetBool("isRunning", false);
                rb.linearVelocity = Vector2.zero; // Stop moving
            }
        }
        else if (dist <= detectRange && !isAttacking)
        {
            ChasePlayer();
        }
        else
        {
            if(animator) animator.SetBool("isRunning", false);
        }
    }

    System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Stop Movement
        rb.linearVelocity = Vector2.zero;
        if(animator) animator.SetBool("isRunning", false);

        // Face Target before attack (Account for rotation/gravity using Dot Product)
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        float dot = Vector2.Dot(dirToTarget, transform.right);
        
        // If dot > 0, target is to our "Right" (Local). If dot < 0, to our "Left".
        // Since Scale.x = 1 is facing Right, and -1 is Left:
        if (dot > 0) transform.localScale = Vector3.one;
        else transform.localScale = new Vector3(-1, 1, 1);

        // Visuals
        if(animator) animator.SetTrigger("Attack");

        // Wait for windup (Optional delay, using 0.1s similar to Player)
        yield return new WaitForSeconds(0.1f);

        // Hitbox ON
        if(attackHitbox) attackHitbox.SetActive(true);

        yield return new WaitForSeconds(0.3f); // Attack duration

        // Hitbox OFF
        if(attackHitbox) attackHitbox.SetActive(false);

        if(attackHitbox) attackHitbox.SetActive(false);

        isAttacking = false;
    }

    void CheckGrounded()
    {
        if (groundCheck != null)
        {
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
}
