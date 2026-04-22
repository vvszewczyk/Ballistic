using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public Ball ballPrefab;
    public Block blockPrefab;
    public Pickup pickupPrefab;
    public Block triangleBlockPrefab;
    [Range(0f, 1f)] public float triangleSpawnChance = 0.2f;

    [Header("Special block chances")]
    [Range(0f, 1f)] public float explosiveBlockChance = 0.08f;
    [Range(0f, 1f)] public float regeneratingBlockChance = 0.06f;
    [Range(0f, 1f)] public float armoredBlockChance = 0.10f;
    [SerializeField] private int guaranteedArmoredAfterBlocks = 12;

    [Header("Scene refs")]
    public Transform launcher;
    public LineRenderer aimLine;
    public Transform barrel;
    public Transform muzzle;
    public Collider2D topWallCollider;

    [Header("Trajectory preview")]
    public LayerMask trajectoryMask;
    public int maxPreviewBounces = 4;
    public float previewMaxDistance = 30f;
    public float previewStartOffset = 0.15f;
    public float previewHitOffset = 0.02f;

    [Header("UI")]
    public TMP_Text levelText;
    public TMP_Text ballCountText;
    public TMP_Text scoreText;
    public TMP_Text gameOverText;

    [Header("Board")]
    public int columns = 7;
    public float columnWidth = 1.2f;
    public float topRowY = 7f;
    public float rowStep = 1.2f;
    public float sideLimit = 4.2f;
    public float launcherY = -9f;

    [Header("Shot")]
    public int ballCount = 10;
    public float ballSpeed = 13f;
    public float delayBetweenBalls = 0.06f;
    public float minAimY = 0.15f;

    [Header("Pickups")]
    public float pickupSpawnChance = 0.7f;
    public int tempBallPlusAmount = 3;
    public int tempBallMinusAmount = 2;
    public float speedSpellMultiplier = 1.35f;
    public float scatterMaxAngle = 35f;

    [Header("Plus balance")]
    [SerializeField] private float earlyPlusChance = 1f;
    [SerializeField] private float midPlusChance = 0.85f;
    [SerializeField] private float latePlusChance = 0.55f;
    [SerializeField] private int guaranteedPlusAfterMisses = 2;

    [Header("Effect pickup balance")]
    [SerializeField] private float midEffectPickupChance = 0.10f;
    [SerializeField] private float lateEffectPickupChance = 0.20f;
    [SerializeField] private float bonusEffectPickupChance = 0.35f;

    [Header("Round trigger pickups")]
    public float bombRadius = 1.35f;
    public int bombDamage = 1;

    public int clusterHitCount = 5;
    public int clusterDamage = 1;

    public int sniperHitCount = 2;
    public int sniperDamage = 1;

    public int crossDamage = 1;

    [Header("Emergency scatter")]
    public float emergencyScatterLeadDistance = 0.8f;
    public float emergencyScatterEdgePadding = 0.4f;
    public float emergencyScatterBottomPadding = 0.8f;
    public float emergencyScatterTopPadding = 0.4f;

    private int roundsWithoutPlus = 0;

    private Camera cam;
    private bool isShooting;
    private bool isGameOver;
    private int activeBalls;
    private float firstReturnedX;
    private bool gotFirstReturn;

    private int turn = 1;
    private int score = 0;
    private int tempBallDeltaNextShot = 0;
    private int blocksSinceArmored = 0;

    private List<Block> blocks = new List<Block>();
    private List<Ball> liveBalls = new List<Ball>();
    private List<Pickup> pickups = new List<Pickup>();

    public float LauncherY => launcherY;

    private void Awake()
    {
        EnsureSpecialBlockChances();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSpecialBlockChances();
    }
#endif

    void Start()
    {
        cam = Camera.main;
        launcher.position = new Vector3(0f, launcherY, 0f);

        if (aimLine == null && launcher != null)
        {
            aimLine = launcher.GetComponent<LineRenderer>();
        }

        if (aimLine != null)
        {
            aimLine.positionCount = 0;
            aimLine.enabled = true;
            aimLine.useWorldSpace = true;
            aimLine.startWidth = 0.05f;
            aimLine.endWidth = 0.05f;
        }

        UpdateLevelText();
        UpdateBallCountText();
        UpdateScoreText();

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }

        SpawnInitialRows(1);
    }

    private void Update()
    {
        if (isShooting || isGameOver)
        {
            if (aimLine != null)
                aimLine.enabled = false;

            return;
        }

        if (aimLine != null)
            aimLine.enabled = true;

        Vector2 dir = GetAimDirection();
        UpdateBarrelRotation(dir);
        UpdateAimLine(dir);

        if (Input.GetMouseButtonUp(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            StartCoroutine(FireBalls(dir));
        }
    }

    private Vector2 GetAimDirection()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorld - launcher.position;

        if (dir.y < minAimY)
            dir.y = minAimY;

        return dir.normalized;
    }

    private IEnumerator FireBalls(Vector2 dir)
    {
        if (isGameOver)
            yield break;

        isShooting = true;
        gotFirstReturn = false;

        int currentShotBallCount = Mathf.Max(1, ballCount + tempBallDeltaNextShot);
        tempBallDeltaNextShot = 0;

        activeBalls = currentShotBallCount;

        for (int i = 0; i < currentShotBallCount; i++)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.shootClip, 0.5f);
            }

            Vector3 spawnPos = GetFireOrigin() + (Vector3)(dir.normalized * 0.1f);
            Ball ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
            ball.Init(this);
            ball.Launch(dir, ballSpeed);
            liveBalls.Add(ball);

            yield return new WaitForSeconds(delayBetweenBalls);
        }
    }

    public void OnBallReturned(Ball ball)
    {
        if (!gotFirstReturn)
        {
            gotFirstReturn = true;
            firstReturnedX = Mathf.Clamp(ball.transform.position.x, -sideLimit, sideLimit);
        }

        liveBalls.Remove(ball);
        Destroy(ball.gameObject);
        activeBalls--;

        if (activeBalls <= 0)
        {
            EndTurn();
        }
    }

    private void EndTurn()
    {
        launcher.position = new Vector3(firstReturnedX, launcherY, 0f);

        RemoveExpiredPickups();

        MoveBlocksDown();
        MovePickupsDown();
        AdvanceBlocksTurn();

        turn++;
        UpdateLevelText();

        SpawnRow();
        CheckGameOver();

        if (!isGameOver)
            isShooting = false;
    }

    private void MoveBlocksDown()
    {
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            if (blocks[i] == null)
            {
                blocks.RemoveAt(i);
                continue;
            }

            blocks[i].transform.position += Vector3.down * rowStep;
        }
    }

    private void MovePickupsDown()
    {
        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            if (pickups[i] == null)
            {
                pickups.RemoveAt(i);
                continue;
            }

            pickups[i].transform.position += Vector3.down * rowStep;

            if (pickups[i].transform.position.y < launcherY - 0.5f)
            {
                Destroy(pickups[i].gameObject);
                pickups.RemoveAt(i);
            }
        }
    }

    private void AdvanceBlocksTurn()
    {
        List<Block> snapshot = new List<Block>(blocks);

        foreach (Block block in snapshot)
        {
            if (block != null)
            {
                block.OnTurnEnded();
            }
        }
    }

    private void SpawnRow()
    {
        bool spawnedAnyBlock = false;
        List<int> emptyColumns = new List<int>();

        for (int c = 0; c < columns; c++)
        {
            if (Random.value < 0.40f)
            {
                Block block = SpawnBlockAt(c);
                blocks.Add(block);
                spawnedAnyBlock = true;
            }
            else
            {
                emptyColumns.Add(c);
            }
        }

        if (!spawnedAnyBlock)
        {
            int forcedColumn = Random.Range(0, columns);
            Block block = SpawnBlockAt(forcedColumn);
            blocks.Add(block);

            emptyColumns.Remove(forcedColumn);
        }

        bool spawnedPlus = false;

        if (pickupPrefab != null && emptyColumns.Count > 0)
        {
            bool shouldSpawnPlus = ShouldSpawnPlus();
            bool shouldSpawnEffect = ShouldSpawnEffectPickup();
            bool shouldSpawnAnyPickup = shouldSpawnPlus || shouldSpawnEffect || ShouldSpawnAnyPickup();
            bool shouldSpawnBonusEffect = shouldSpawnPlus && !shouldSpawnEffect && ShouldSpawnBonusEffectPickup();

            if (shouldSpawnAnyPickup)
            {
                PickupType primaryPickupType = GetPrimaryPickupType(shouldSpawnPlus, shouldSpawnEffect, out spawnedPlus);

                SpawnPickupInRandomEmptyColumn(emptyColumns, primaryPickupType);

                if (spawnedPlus && emptyColumns.Count > 0 && (shouldSpawnEffect || shouldSpawnBonusEffect))
                {
                    SpawnPickupInRandomEmptyColumn(emptyColumns, GetRandomEffectPickupType());
                }
            }
        }

        if (spawnedPlus)
            roundsWithoutPlus = 0;
        else
            roundsWithoutPlus++;
    }

    public void CollectPickup(Pickup pickup, Ball triggeringBall)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.pickupClip, 0.45f);
        }

        bool destroyAfterUse = true;

        switch (pickup.Type)
        {
            case PickupType.AddBallPermanent:
                ballCount += 1;
                UpdateBallCountText();
                break;

            case PickupType.RowBlast:
                DamageRowAt(pickup.transform.position.y, 1);
                destroyAfterUse = false;
                break;

            case PickupType.ColumnBlast:
                DamageColumnAt(pickup.transform.position.x, 1);
                destroyAfterUse = false;
                break;

            case PickupType.Bomb:
                DamageBlocksInRadius(pickup.transform.position, bombRadius, bombDamage);
                destroyAfterUse = false;
                break;

            case PickupType.Cluster:
                DamageRandomBlocks(clusterHitCount, clusterDamage);
                destroyAfterUse = false;
                break;

            case PickupType.CrossBlast:
                DamageCrossAt(pickup.transform.position, crossDamage);
                destroyAfterUse = false;
                break;

            case PickupType.Sniper:
                DamageNearestBlocks(pickup.transform.position, sniperHitCount, sniperDamage);
                destroyAfterUse = false;
                break;

            case PickupType.BallPlusSpell:
                tempBallDeltaNextShot += tempBallPlusAmount;
                break;

            case PickupType.BallMinusSpell:
                tempBallDeltaNextShot -= tempBallMinusAmount;
                break;

            case PickupType.SpeedSpell:
                SpeedUpActiveBalls(speedSpellMultiplier);
                break;

            case PickupType.ScatterSpell:
                if (triggeringBall != null)
                    triggeringBall.Scatter(scatterMaxAngle);
                break;
        }

        if (destroyAfterUse)
        {
            pickups.Remove(pickup);
            Destroy(pickup.gameObject);
        }
    }

    private void DamageRowAt(float y, int damage)
    {
        List<Block> snapshot = new List<Block>(blocks);

        foreach (Block block in snapshot)
        {
            if (block == null) continue;

            if (Mathf.Abs(block.transform.position.y - y) <= rowStep * 0.35f)
            {
                block.Hit(damage, DamageSource.Row, block.transform.position);
            }
        }
    }

    private void DamageColumnAt(float x, int damage)
    {
        List<Block> snapshot = new List<Block>(blocks);

        foreach (Block block in snapshot)
        {
            if (block == null) continue;

            if (Mathf.Abs(block.transform.position.x - x) <= columnWidth * 0.35f)
            {
                block.Hit(damage, DamageSource.Column, block.transform.position);
            }
        }
    }

    private void SpeedUpActiveBalls(float multiplier)
    {
        foreach (Ball ball in liveBalls)
        {
            if (ball != null)
            {
                ball.MultiplySpeed(multiplier);
            }
        }
    }

    private float GetColumnX(int columnIndex)
    {
        return -((columns - 1) * columnWidth * 0.5f) + (columnIndex * columnWidth);
    }

    public void NotifyBlockDestroyed(Block block)
    {
        blocks.Remove(block);
    }

    private void CheckGameOver()
    {
        if (isGameOver) return;

        foreach (Block block in blocks)
        {
            if (block != null && block.transform.position.y <= launcherY + 0.6f)
            {
                StartGameOver();
                break;
            }
        }
    }

    private void StartGameOver()
    {
        isGameOver = true;
        isShooting = true;

        if (aimLine != null)
            aimLine.enabled = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverClip, 0.7f);
        }

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
        }

        Debug.Log("GAME OVER");
    }

    private void UpdateAimLine(Vector2 dir)
    {
        if (aimLine == null) return;

        List<Vector3> points = new List<Vector3>();

        Vector2 origin = (Vector2)GetFireOrigin();
        Vector2 direction = dir.normalized;

        origin += direction * previewStartOffset;
        points.Add(GetFireOrigin());

        for (int bounce = 0; bounce < maxPreviewBounces; bounce++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, previewMaxDistance, trajectoryMask);

            if (hit.collider == null)
            {
                points.Add(origin + direction * previewMaxDistance);
                break;
            }

            points.Add(hit.point);

            origin = hit.point + hit.normal * previewHitOffset;
            direction = Vector2.Reflect(direction, hit.normal).normalized;
        }

        aimLine.positionCount = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            aimLine.SetPosition(i, points[i]);
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + turn;
        }
    }

    private void UpdateBallCountText()
    {
        if (ballCountText != null)
        {
            ballCountText.text = "Balls: " + ballCount;
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    void SpawnInitialRows(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                MoveBlocksDown();
                MovePickupsDown();
            }

            SpawnRow();
        }
    }

    private int GetTargetBallCount()
    {
        return 1 + Mathf.FloorToInt(turn / 2f);
    }

    private bool ShouldSpawnPlus()
    {
        if (turn <= 3)
            return Random.value < earlyPlusChance;

        if (roundsWithoutPlus >= guaranteedPlusAfterMisses)
            return true;

        int targetBalls = GetTargetBallCount();

        if (ballCount < targetBalls)
            return Random.value < 0.9f;

        if (turn <= 8)
            return Random.value < midPlusChance;

        return Random.value < latePlusChance;
    }

    private bool ShouldSpawnEffectPickup()
    {
        if (turn <= 3)
            return false;

        if (turn <= 6)
            return Random.value < midEffectPickupChance;

        return Random.value < lateEffectPickupChance;
    }

    private bool ShouldSpawnBonusEffectPickup()
    {
        if (turn <= 3)
            return false;

        return Random.value < bonusEffectPickupChance;
    }

    private bool ShouldSpawnAnyPickup()
    {
        return Random.value < pickupSpawnChance;
    }

    private void EnsureSpecialBlockChances()
    {
        if (explosiveBlockChance <= 0f && regeneratingBlockChance <= 0f && armoredBlockChance <= 0f)
        {
            explosiveBlockChance = 0.08f;
            regeneratingBlockChance = 0.06f;
            armoredBlockChance = 0.10f;
            return;
        }

        if (armoredBlockChance <= 0f)
            armoredBlockChance = 0.10f;
    }

    private BlockType GetRandomBlockType()
    {
        if (armoredBlockChance > 0f &&
            guaranteedArmoredAfterBlocks > 0 &&
            blocksSinceArmored >= guaranteedArmoredAfterBlocks)
        {
            blocksSinceArmored = 0;
            return BlockType.Armored;
        }

        float roll = Random.value;
        float threshold = 0f;
        BlockType blockType = BlockType.Normal;

        threshold += explosiveBlockChance;
        if (roll < threshold)
            blockType = BlockType.Explosive;
        else
        {
            threshold += regeneratingBlockChance;
            if (roll < threshold)
                blockType = BlockType.Regenerating;
            else
            {
                threshold += armoredBlockChance;
                if (roll < threshold)
                    blockType = BlockType.Armored;
            }
        }

        if (blockType == BlockType.Armored)
            blocksSinceArmored = 0;
        else
            blocksSinceArmored++;

        return blockType;
    }

    private void SpawnSpecificPickupAt(int columnIndex, PickupType type)
    {
        Vector3 pos = new Vector3(GetColumnX(columnIndex), topRowY, 0f);
        Pickup pickup = Instantiate(pickupPrefab, pos, Quaternion.identity);
        pickup.Setup(type, this);
        pickups.Add(pickup);
    }

    private void SpawnPickupInRandomEmptyColumn(List<int> emptyColumns, PickupType type)
    {
        int pickupColumn = emptyColumns[Random.Range(0, emptyColumns.Count)];
        SpawnSpecificPickupAt(pickupColumn, type);
        emptyColumns.Remove(pickupColumn);
    }

    private PickupType GetPrimaryPickupType(bool shouldSpawnPlus, bool shouldSpawnEffect, out bool spawnedPlus)
    {
        if (shouldSpawnPlus)
        {
            spawnedPlus = true;
            return PickupType.AddBallPermanent;
        }

        if (shouldSpawnEffect)
        {
            spawnedPlus = false;
            return GetRandomEffectPickupType();
        }

        // Honor a successful pickup roll even if the sub-rolls missed.
        spawnedPlus = true;
        return PickupType.AddBallPermanent;
    }

    private PickupType GetRandomEffectPickupType()
    {
        int roll = Random.Range(0, 100);

        if (roll < 18) return PickupType.RowBlast;
        if (roll < 36) return PickupType.ColumnBlast;
        if (roll < 52) return PickupType.Bomb;
        if (roll < 66) return PickupType.Cluster;
        if (roll < 78) return PickupType.CrossBlast;
        if (roll < 88) return PickupType.Sniper;
        if (roll < 95) return PickupType.SpeedSpell;

        return PickupType.ScatterSpell;
    }

    private void RemoveExpiredPickups()
    {
        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            if (pickups[i] == null)
            {
                pickups.RemoveAt(i);
                continue;
            }

            if (pickups[i].ShouldExpireAtTurnEnd())
            {
                Destroy(pickups[i].gameObject);
                pickups.RemoveAt(i);
            }
        }
    }

    private Block SpawnBlockAt(int columnIndex)
    {
        Vector3 pos = new Vector3(GetColumnX(columnIndex), topRowY, 0f);

        bool spawnTriangle = triangleBlockPrefab != null && Random.value < triangleSpawnChance;
        Block prefabToSpawn = spawnTriangle ? triangleBlockPrefab : blockPrefab;

        Block block = Instantiate(prefabToSpawn, pos, Quaternion.identity);

        BlockType blockType = GetRandomBlockType();
        block.Setup(turn, this, blockType);

        if (spawnTriangle)
        {
            int rotationIndex = Random.Range(0, 4);
            block.SetShapeRotation(rotationIndex);
        }

        return block;
    }

    public void DamageBlocksInRadius(Vector3 center, float radius, int damage, Block sourceToIgnore = null)
    {
        List<Block> snapshot = new List<Block>(blocks);

        foreach (Block block in snapshot)
        {
            if (block == null) continue;
            if (block == sourceToIgnore) continue;

            float dist = Vector2.Distance(block.transform.position, center);
            if (dist <= radius)
            {
                block.Hit(damage, DamageSource.Explosion, block.transform.position);
            }
        }
    }

    private void DamageRandomBlocks(int count, int damage)
    {
        List<Block> candidates = new List<Block>();

        foreach (Block block in blocks)
        {
            if (block != null)
                candidates.Add(block);
        }

        int damagedBlocks = 0;

        while (damagedBlocks < count && candidates.Count > 0)
        {
            candidates.RemoveAll(block => block == null);
            if (candidates.Count == 0)
                break;

            int idx = Random.Range(0, candidates.Count);
            Block target = candidates[idx];
            candidates.RemoveAt(idx);

            if (target == null)
                continue;

            target.Hit(damage, DamageSource.Cluster, target.transform.position);
            damagedBlocks++;
        }
    }

    private void DamageNearestBlocks(Vector3 center, int count, int damage)
    {
        List<Block> candidates = new List<Block>();

        foreach (Block block in blocks)
        {
            if (block != null)
                candidates.Add(block);
        }

        candidates.Sort((a, b) =>
        {
            float da = Vector2.Distance(a.transform.position, center);
            float db = Vector2.Distance(b.transform.position, center);
            return da.CompareTo(db);
        });

        int damagedBlocks = 0;

        foreach (Block block in candidates)
        {
            if (damagedBlocks >= count)
                break;

            if (block == null)
                continue;

            block.Hit(damage, DamageSource.Sniper, block.transform.position);
            damagedBlocks++;
        }
    }

    private void DamageCrossAt(Vector3 center, int damage)
    {
        DamageRowAt(center.y, damage);
        DamageColumnAt(center.x, damage);
    }

    public bool TrySpawnEmergencyScatter(Vector3 ballPosition, Vector2 velocity)
    {
        if (pickupPrefab == null) return false;
        if (velocity.sqrMagnitude < 0.001f) return false;

        float horizontalDir = Mathf.Sign(velocity.x);
        if (Mathf.Abs(horizontalDir) < 0.01f) return false;

        Vector3 spawnPos = ballPosition;
        spawnPos.x += horizontalDir * emergencyScatterLeadDistance;
        spawnPos.y = ballPosition.y;

        float minX = -sideLimit + emergencyScatterEdgePadding;
        float maxX = sideLimit - emergencyScatterEdgePadding;
        float minY = launcherY + emergencyScatterBottomPadding;

        spawnPos.x = Mathf.Clamp(spawnPos.x, minX, maxX);

        if (topWallCollider != null)
        {
            float topLimitY = topWallCollider.bounds.min.y - 0.15f;
            spawnPos.y = Mathf.Clamp(spawnPos.y, minY, topLimitY);
        }
        else
        {
            spawnPos.y = Mathf.Max(spawnPos.y, minY);
        }

        spawnPos.z = 0f;

        if (HasNearbyScatterPickup(spawnPos))
            return false;

        Pickup pickup = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);
        pickup.Setup(PickupType.ScatterSpell, this);
        pickups.Add(pickup);

        return true;
    }

    private bool HasNearbyScatterPickup(Vector3 position)
    {
        float maxHorizontalDistance = Mathf.Max(0.35f, emergencyScatterLeadDistance * 0.75f);
        float maxVerticalDistance = Mathf.Max(0.25f, rowStep * 0.5f);

        foreach (Pickup pickup in pickups)
        {
            if (pickup == null || pickup.Type != PickupType.ScatterSpell)
                continue;

            Vector3 pickupPosition = pickup.transform.position;

            if (Mathf.Abs(pickupPosition.x - position.x) <= maxHorizontalDistance &&
                Mathf.Abs(pickupPosition.y - position.y) <= maxVerticalDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateBarrelRotation(Vector2 dir)
    {
        if (barrel == null) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 180f;
        barrel.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector3 GetFireOrigin()
    {
        if (muzzle != null)
            return muzzle.position;

        return launcher.position;
    }
}
