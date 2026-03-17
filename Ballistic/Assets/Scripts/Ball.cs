using UnityEngine;

public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;
    private GameManager gameManager;
    private bool returned;

    void Awake()
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

    void Update()
    {
        if (returned || gameManager == null) return;

        if (transform.position.y < gameManager.LauncherY - 0.5f && rb.linearVelocity.y <= 0f)
        {
            returned = true;
            gameManager.OnBallReturned(this);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Block block = collision.collider.GetComponent<Block>();
        if (block != null)
        {
            block.Hit(1);
        }
    }
}