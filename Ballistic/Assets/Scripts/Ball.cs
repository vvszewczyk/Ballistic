using UnityEngine;

public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;
    private GameManager gameManager;
    private bool returned;

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
        Block block = collision.collider.GetComponent<Block>();
        if (block != null)
        {
            block.Hit(1);
        }
    }
}