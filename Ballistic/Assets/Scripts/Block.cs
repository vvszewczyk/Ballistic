using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Transform rotatablePart;
    [SerializeField] private SpriteRenderer[] flashRenderers;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.05f;

    [SerializeField] private GameObject hitFxPrefab;

    [Header("Triangle text")]
    [SerializeField] private Vector3 triangleTextOffset = new Vector3(-0.12f, -0.12f, 0f);

    private Color[] originalColors;
    private int hp;
    private GameManager gameManager;

    private void Awake()
    {
        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<SpriteRenderer>();

        originalColors = new Color[flashRenderers.Length];

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            originalColors[i] = flashRenderers[i].color;
        }
    }

    public void Setup(int value, GameManager manager)
    {
        hp = value;
        gameManager = manager;
        Refresh();
    }

    public void SetShapeRotation(int quarterTurns)
    {
        float angle = quarterTurns * 90f;

        if (rotatablePart != null)
        {
            rotatablePart.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (hpText != null && rotatablePart != null)
        {
            Vector3 rotatedOffset = Quaternion.Euler(0f, 0f, angle) * triangleTextOffset;
            hpText.transform.localPosition = rotatedOffset;
            hpText.transform.localRotation = Quaternion.identity;
        }
    }

    public void Hit(int damage)
    {
        StartCoroutine(FlashHit());
        int appliedDamage = Mathf.Min(damage, hp);
        hp -= appliedDamage;

        if (hitFxPrefab != null)
        {
            GameObject fx = Instantiate(hitFxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 0.5f);
        }

        if (appliedDamage > 0 && gameManager != null)
        {
            gameManager.AddScore(appliedDamage);
        }

        if (AudioManager.Instance != null && appliedDamage > 0)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.hitClip, 0.25f);
        }

        if (hp <= 0)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.destroyClip, 0.55f);
            }

            gameManager.NotifyBlockDestroyed(this);
            Destroy(gameObject);
            return;
        }

        Refresh();
    }

    void Refresh()
    {
        if (hpText != null)
        {
            hpText.text = hp.ToString();
        }
    }

    private System.Collections.IEnumerator FlashHit()
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
                flashRenderers[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
                flashRenderers[i].color = originalColors[i];
        }
    }
}
