using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public enum BlockType
{
    Normal,
    Explosive,
    Regenerating,
    Armored
}

public enum DamageSource
{
    Ball,
    Explosion,
    Row,
    Column,
    Cluster,
    Cross,
    Sniper
}

public class Block : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Transform rotatablePart;
    [SerializeField] private GameObject hitFxPrefab;

    [Header("Visuals")]
    [FormerlySerializedAs("flashRenderers")]
    [SerializeField] private SpriteRenderer[] visualRenderers;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.05f;

    [Header("Text")]
    [SerializeField, Range(0.4f, 1f)] private float armoredTextScale = 0.65f;
    [SerializeField, Range(0.25f, 1f)] private float armoredTriangleTextScale = 0.48f;

    [Header("Triangle text")]
    [SerializeField] private Vector3 triangleTextOffset = new Vector3(-0.12f, -0.12f, 0f);

    [Header("Special block settings")]
    [SerializeField] private int explosiveDamage = 1;
    [SerializeField] private float explosiveRadius = 1.2f;
    [SerializeField] private int regenAmount = 1;
    [SerializeField] private int armoredHitsPerHp = 3;

    private int hp;
    private int armoredHitsRemaining;
    private GameManager gameManager;
    private BlockType blockType = BlockType.Normal;
    private bool isDying;

    private Color[] baseColors;
    private float baseHpFontSize;

    public BlockType Type => blockType;

    private void Awake()
    {
        if (visualRenderers == null || visualRenderers.Length == 0)
            visualRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        CacheBaseColors();
        CacheBaseTextSize();
    }

    public void Setup(int value, GameManager manager)
    {
        Setup(value, manager, BlockType.Normal);
    }

    public void Setup(int value, GameManager manager, BlockType type)
    {
        hp = value;
        gameManager = manager;
        blockType = type;
        isDying = false;
        armoredHitsRemaining = GetArmoredHitsPerHp();

        ApplyTypeVisuals();
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

    public void OnTurnEnded()
    {
        if (isDying) return;

        if (blockType == BlockType.Regenerating)
        {
            hp += regenAmount;
            Refresh();
        }
    }

    public void Hit(int damage)
    {
        Hit(damage, DamageSource.Ball, transform.position);
    }

    public void Hit(int damage, DamageSource source)
    {
        Hit(damage, source, transform.position);
    }

    public void Hit(int damage, Vector2 hitPoint)
    {
        Hit(damage, DamageSource.Ball, hitPoint);
    }

    public void Hit(int damage, DamageSource source, Vector2 hitPoint)
    {
        if (isDying) return;

        SpawnHitFx(hitPoint);
        StartCoroutine(FlashHit());

        bool playedHitSfx = false;

        if (blockType == BlockType.Armored)
        {
            if (source != DamageSource.Ball)
            {
                Refresh();
                return;
            }

            armoredHitsRemaining--;
            PlayHitSfx();
            playedHitSfx = true;

            if (armoredHitsRemaining > 0)
            {
                Refresh();
                return;
            }

            armoredHitsRemaining = GetArmoredHitsPerHp();
            damage = 1;
        }

        int appliedDamage = Mathf.Min(damage, hp);
        hp -= appliedDamage;

        if (appliedDamage > 0 && gameManager != null)
        {
            gameManager.AddScore(appliedDamage);
        }

        if (appliedDamage > 0 && !playedHitSfx)
        {
            PlayHitSfx();
        }

        if (hp <= 0)
        {
            DestroyBlock();
            return;
        }

        Refresh();
    }

    private int GetArmoredHitsPerHp()
    {
        return Mathf.Max(1, armoredHitsPerHp);
    }

    private void DestroyBlock()
    {
        if (isDying) return;
        isDying = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.destroyClip, 0.55f);
        }

        if (blockType == BlockType.Explosive && gameManager != null)
        {
            gameManager.DamageBlocksInRadius(transform.position, explosiveRadius, explosiveDamage, this);
        }

        if (gameManager != null)
        {
            gameManager.NotifyBlockDestroyed(this);
        }

        Destroy(gameObject);
    }

    private void SpawnHitFx(Vector2 hitPoint)
    {
        if (hitFxPrefab != null)
        {
            GameObject fx = Instantiate(hitFxPrefab, hitPoint, Quaternion.identity);
            Destroy(fx, 0.5f);
        }
    }

    private void PlayHitSfx()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.hitClip, 0.25f);
        }
    }

    private IEnumerator FlashHit()
    {
        if (visualRenderers == null || visualRenderers.Length == 0)
            yield break;

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] != null)
                visualRenderers[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] != null && baseColors != null && i < baseColors.Length)
                visualRenderers[i].color = baseColors[i];
        }
    }

    private void ApplyTypeVisuals()
    {
        if (visualRenderers == null)
            return;

        Color typeColor = Color.white;

        switch (blockType)
        {
            case BlockType.Normal:
                typeColor = new Color(0.0f, 0.0f, 0.0f);
                break;

            case BlockType.Explosive:
                typeColor = new Color(0.75f, 0.20f, 0.20f);
                break;

            case BlockType.Regenerating:
                typeColor = new Color(0.20f, 0.60f, 0.25f);
                break;

            case BlockType.Armored:
                typeColor = new Color(0.12f, 0.18f, 0.36f);
                break;
        }

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] != null)
                visualRenderers[i].color = typeColor;
        }

        CacheBaseColors();
    }

    private void CacheBaseColors()
    {
        if (visualRenderers == null)
            return;

        baseColors = new Color[visualRenderers.Length];

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] != null)
                baseColors[i] = visualRenderers[i].color;
        }
    }

    private void CacheBaseTextSize()
    {
        if (hpText != null)
        {
            baseHpFontSize = hpText.fontSize;
        }
    }

    private void ApplyHpTextSize(bool isArmored)
    {
        if (hpText == null || baseHpFontSize <= 0f)
            return;

        float textScale = rotatablePart != null ? armoredTriangleTextScale : armoredTextScale;
        hpText.fontSize = isArmored ? baseHpFontSize * textScale : baseHpFontSize;
    }

    private void Refresh()
    {
        if (hpText == null) return;

        ApplyHpTextSize(blockType == BlockType.Armored);

        switch (blockType)
        {
            case BlockType.Explosive:
                hpText.text = hp + "B";
                break;

            case BlockType.Regenerating:
                hpText.text = hp + "+";
                break;

            case BlockType.Armored:
                hpText.text = hp + "A" + armoredHitsRemaining;
                break;

            default:
                hpText.text = hp.ToString();
                break;
        }
    }
}
