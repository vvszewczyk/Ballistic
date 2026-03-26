using UnityEngine;

public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;
    private GameManager gameManager;
    private bool returned;

    [SerializeField] private float noBlockHitScatterDelay = 1.2f;
    [SerializeField] private float horizontalVelocityThreshold = 0.35f;

    private float timeSinceLastBlockHit = 0f;
    private bool emergencyScatterSpawned = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(GameManager manager)
    {
        gameManager = manager;
    }

    public void Launch(Vector2 dir, float speed)
    {
        rb.linearVelocity = dir.normalized * speed;
    }

    public void MultiplySpeed(float multiplier)
    {
        rb.linearVelocity *= multiplier;
    }

    public void Scatter(float maxAngleDeg)
    {
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.001f) return;

        float angle = Random.Range(-maxAngleDeg, maxAngleDeg);
        Vector2 newDir = Quaternion.Euler(0f, 0f, angle) * v.normalized;

        rb.linearVelocity = newDir.normalized * v.magnitude;
    }

    private void Update()
    {
        if (returned || gameManager == null) return;

        timeSinceLastBlockHit += Time.deltaTime;

        Vector2 velocity = rb.linearVelocity;
        bool isAlmostHorizontal = Mathf.Abs(velocity.y) < horizontalVelocityThreshold;

        if (!emergencyScatterSpawned &&
            timeSinceLastBlockHit >= noBlockHitScatterDelay &&
            isAlmostHorizontal)
        {
            bool spawned = gameManager.TrySpawnEmergencyScatter(transform.position, velocity);

            if (spawned)
            {
                emergencyScatterSpawned = true;
            }
        }

        if (transform.position.y < gameManager.LauncherY - 0.5f && rb.linearVelocity.y <= 0f)
        {
            ReturnToManager();
        }
    }

    public void ReturnToManager()
    {
        if (returned) return;
        returned = true;
        gameManager.OnBallReturned(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Block block = collision.collider.GetComponentInParent<Block>();
        if (block != null)
        {
            block.Hit(1);

            timeSinceLastBlockHit = 0f;
            emergencyScatterSpawned = false;
        }
    }
}