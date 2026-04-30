using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public enum BlockType
{
    Normal,
    Explosive,
    Regenerating,
    Armored,
    Moving,
    Boss,
    Splitting
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
    [SerializeField] private Transform scalablePart;

    [Header("Triangle text")]
    [SerializeField] private Vector3 triangleTextOffset = new Vector3(-0.12f, -0.12f, 0f);

    [Header("Special block settings")]
    [SerializeField] private int explosiveDamage = 1;
    [SerializeField] private float explosiveRadius = 1.2f;
    [SerializeField] private int regenAmount = 1;
    [SerializeField] private int armoredHitsPerHp = 3;
    [SerializeField] private int maxSplitStages = 2;
    [SerializeField, Range(0.1f, 1f)] private float splitMinVisualScale = 0.5f;

    private int hp;
    private int initialHp;
    private int armoredHitsRemaining;
    private int splitStage;
    private GameManager gameManager;
    private BlockType blockType = BlockType.Normal;
    private int footprintWidth = 1;
    private int footprintHeight = 1;
    private float footprintColumnWidth = 1f;
    private float footprintRowStep = 1f;
    private bool isDying;

    private Color[] baseColors;
    private Vector3 baseLocalScale = Vector3.one;
    private Vector3 baseHpTextLocalScale = Vector3.one;
    private Vector3 baseScalablePartLocalScale = Vector3.one;
    private bool cachedBaseLayout;
    private bool cachedScalablePartLayout;

    private const string OutlineNameMarker = "outline";

    public BlockType Type => blockType;
    public int FootprintWidth => footprintWidth;
    public int FootprintHeight => footprintHeight;

    private void Awake()
    {
        if (visualRenderers == null || visualRenderers.Length == 0)
            visualRenderers = GetTintableRenderers();

        CacheBaseLayout();
        CacheBaseColors();
    }

    public void Setup(int value, GameManager manager)
    {
        Setup(value, manager, BlockType.Normal);
    }

    public void Setup(int value, GameManager manager, BlockType type)
    {
        hp = value;
        initialHp = Mathf.Max(1, value);
        gameManager = manager;
        blockType = type;
        isDying = false;
        armoredHitsRemaining = GetArmoredHitsPerHp();
        splitStage = blockType == BlockType.Splitting ? Mathf.Max(0, maxSplitStages) : 0;

        if (blockType == BlockType.Splitting)
        {
            EnsureScalablePart();
        }

        ApplyTypeVisuals();
        ApplySplitVisualScale();
        Refresh();
    }

    public void SetFootprint(int width, int height, float columnWidth, float rowStep)
    {
        CacheBaseLayout();

        footprintWidth = Mathf.Max(1, width);
        footprintHeight = Mathf.Max(1, height);
        footprintColumnWidth = Mathf.Max(0.01f, columnWidth);
        footprintRowStep = Mathf.Max(0.01f, rowStep);

        ApplyFootprintScale();
    }

    public float GetBottomY()
    {
        return transform.position.y - ((footprintHeight - 1) * footprintRowStep * 0.5f);
    }

    public bool OccupiesCell(float x, float y)
    {
        return OccupiesColumn(x) && OccupiesRow(y);
    }

    public bool OccupiesColumn(float x)
    {
        float halfWidth = (footprintWidth - 1) * footprintColumnWidth * 0.5f;
        return Mathf.Abs(transform.position.x - x) <= halfWidth + footprintColumnWidth * 0.35f;
    }

    public bool OccupiesRow(float y)
    {
        float halfHeight = (footprintHeight - 1) * footprintRowStep * 0.5f;
        return Mathf.Abs(transform.position.y - y) <= halfHeight + footprintRowStep * 0.35f;
    }

    public void SetShapeRotation(int quarterTurns)
    {
        float angle = quarterTurns * 90f;

        if (rotatablePart != null)
        {
            AttachOutlinePartsTo(rotatablePart);
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

        if (blockType == BlockType.Armored && source != DamageSource.Ball)
        {
            Refresh();
            return;
        }

        SpawnHitFx(hitPoint);
        StartCoroutine(FlashHit());

        bool playedHitSfx = false;

        if (blockType == BlockType.Armored)
        {
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
            if (TrySplitInsteadOfDestroy())
                return;

            DestroyBlock();
            return;
        }

        if (blockType == BlockType.Moving && source == DamageSource.Ball && appliedDamage > 0)
        {
            TryMoveAfterHit();
        }

        Refresh();
    }

    private int GetArmoredHitsPerHp()
    {
        return Mathf.Max(1, armoredHitsPerHp);
    }

    private bool TrySplitInsteadOfDestroy()
    {
        if (blockType != BlockType.Splitting || splitStage <= 0)
            return false;

        splitStage--;
        hp = GetSplitStageHp();

        ApplySplitVisualScale();
        Refresh();
        return true;
    }

    private int GetSplitStageHp()
    {
        if (splitStage <= 0)
            return 1;

        float stageRatio = splitStage / (float)Mathf.Max(1, maxSplitStages);
        return Mathf.Max(1, Mathf.RoundToInt(initialHp * stageRatio));
    }

    private void TryMoveAfterHit()
    {
        if (gameManager == null) return;

        int firstDir = Random.value < 0.5f ? -1 : 1;

        if (!gameManager.TryMoveBlockHorizontally(this, firstDir))
        {
            gameManager.TryMoveBlockHorizontally(this, -firstDir);
        }
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

            case BlockType.Moving:
                typeColor = new Color(0.20f, 0.45f, 0.90f);
                break;

            case BlockType.Boss:
                typeColor = new Color(0.45f, 0.12f, 0.65f);
                break;

            case BlockType.Splitting:
                typeColor = new Color(0.95f, 0.55f, 0.15f);
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

    private void CacheBaseLayout()
    {
        if (cachedBaseLayout)
            return;

        baseLocalScale = transform.localScale;

        if (hpText != null)
        {
            baseHpTextLocalScale = hpText.transform.localScale;
        }

        cachedBaseLayout = true;
    }

    private void ApplyFootprintScale()
    {
        float footprintScaleX = 1f + ((footprintWidth - 1) * footprintColumnWidth);
        float footprintScaleY = 1f + ((footprintHeight - 1) * footprintRowStep);

        transform.localScale = new Vector3(
            baseLocalScale.x * footprintScaleX,
            baseLocalScale.y * footprintScaleY,
            baseLocalScale.z);

        if (hpText != null)
        {
            hpText.transform.localScale = new Vector3(
                baseHpTextLocalScale.x / footprintScaleX,
                baseHpTextLocalScale.y / footprintScaleY,
                baseHpTextLocalScale.z);
        }
    }

    private void EnsureScalablePart()
    {
        if (scalablePart == null)
        {
            scalablePart = rotatablePart != null ? rotatablePart : CreateRuntimeScalablePart();
        }

        if (!cachedScalablePartLayout && scalablePart != null)
        {
            baseScalablePartLocalScale = scalablePart.localScale;
            cachedScalablePartLayout = true;
        }

        AttachOutlinePartsTo(scalablePart);
    }

    private Transform CreateRuntimeScalablePart()
    {
        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        BoxCollider2D rootCollider = GetComponent<BoxCollider2D>();

        if (rootRenderer == null && rootCollider == null)
            return null;

        GameObject part = new GameObject("ScalablePart");
        part.layer = gameObject.layer;
        part.tag = gameObject.tag;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        SpriteRenderer partRenderer = null;

        if (rootRenderer != null)
        {
            partRenderer = part.AddComponent<SpriteRenderer>();
            CopySpriteRenderer(rootRenderer, partRenderer);
            rootRenderer.enabled = false;
            visualRenderers = new[] { partRenderer };
        }

        if (rootCollider != null)
        {
            BoxCollider2D partCollider = part.AddComponent<BoxCollider2D>();
            CopyBoxCollider(rootCollider, partCollider);
            rootCollider.enabled = false;
        }

        return part.transform;
    }

    private SpriteRenderer[] GetTintableRenderers()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        int tintableCount = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!IsOutlineTransform(renderers[i].transform))
                tintableCount++;
        }

        SpriteRenderer[] tintableRenderers = new SpriteRenderer[tintableCount];
        int tintableIndex = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!IsOutlineTransform(renderers[i].transform))
            {
                tintableRenderers[tintableIndex] = renderers[i];
                tintableIndex++;
            }
        }

        return tintableRenderers;
    }

    private void AttachOutlinePartsTo(Transform target)
    {
        if (target == null)
            return;

        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < childTransforms.Length; i++)
        {
            Transform child = childTransforms[i];

            if (child == null || child == transform || child == target)
                continue;

            if (!IsOutlineTransform(child) || child.IsChildOf(target))
                continue;

            child.SetParent(target, true);
        }
    }

    private bool IsOutlineTransform(Transform target)
    {
        return target != null &&
               target.name.IndexOf(OutlineNameMarker, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void CopySpriteRenderer(SpriteRenderer source, SpriteRenderer target)
    {
        target.sprite = source.sprite;
        target.color = source.color;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
        target.drawMode = source.drawMode;
        target.size = source.size;
        target.maskInteraction = source.maskInteraction;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
        target.sharedMaterial = source.sharedMaterial;
    }

    private void CopyBoxCollider(BoxCollider2D source, BoxCollider2D target)
    {
        target.offset = source.offset;
        target.size = source.size;
        target.edgeRadius = source.edgeRadius;
        target.isTrigger = source.isTrigger;
        target.usedByEffector = source.usedByEffector;
        target.density = source.density;
        target.sharedMaterial = source.sharedMaterial;
    }

    private void ApplySplitVisualScale()
    {
        if (blockType != BlockType.Splitting)
            return;

        EnsureScalablePart();

        if (scalablePart == null)
            return;

        float maxStages = Mathf.Max(1, maxSplitStages);
        float stageRatio = splitStage / maxStages;
        float visualScale = Mathf.Lerp(splitMinVisualScale, 1f, stageRatio);
        scalablePart.localScale = baseScalablePartLocalScale * visualScale;
    }

    private void Refresh()
    {
        if (hpText == null) return;

        hpText.text = hp.ToString();
    }
}
