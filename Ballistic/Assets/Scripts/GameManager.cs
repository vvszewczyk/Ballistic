using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public Ball ballPrefab;
    public Block blockPrefab;
    public Pickup pickupPrefab;

    [Header("Scene refs")]
    public Transform launcher;
    public LineRenderer aimLine;

    [Header("UI")]
    public TMP_Text levelText;

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
    public float pickupSpawnChance = 0.45f;
    public int tempBallPlusAmount = 3;
    public int tempBallMinusAmount = 2;
    public float speedSpellMultiplier = 1.35f;
    public float scatterMaxAngle = 35f;

    private Camera cam;
    private bool isShooting;
    private int activeBalls;
    private float firstReturnedX;
    private bool gotFirstReturn;

    private int turn = 1;
    private int tempBallDeltaNextShot = 0;

    private List<Block> blocks = new List<Block>();
    private List<Ball> liveBalls = new List<Ball>();
    private List<Pickup> pickups = new List<Pickup>();

    public float LauncherY => launcherY;

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
            aimLine.positionCount = 2;
            aimLine.enabled = true;
        }

        UpdateLevelText();

        SpawnInitialRows(2);
    }

    private void Update()
    {
        if (isShooting) return;

        Vector2 dir = GetAimDirection();
        UpdateAimLine(dir);

        if (Input.GetMouseButtonUp(0))
        {
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
        isShooting = true;
        gotFirstReturn = false;

        int currentShotBallCount = Mathf.Max(1, ballCount + tempBallDeltaNextShot);
        tempBallDeltaNextShot = 0;

        activeBalls = currentShotBallCount;

        for (int i = 0; i < currentShotBallCount; i++)
        {
            Ball ball = Instantiate(ballPrefab, launcher.position, Quaternion.identity);
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

        MoveBlocksDown();
        MovePickupsDown();

        turn++;
        UpdateLevelText();

        SpawnRow();
        CheckGameOver();

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

    private void SpawnRow()
    {
        bool spawnedAnyBlock = false;
        List<int> emptyColumns = new List<int>();

        for (int c = 0; c < columns; c++)
        {
            if (Random.value < 0.55f)
            {
                Vector3 pos = new Vector3(GetColumnX(c), topRowY, 0f);
                Block block = Instantiate(blockPrefab, pos, Quaternion.identity);
                block.Setup(turn, this);
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
            Vector3 pos = new Vector3(GetColumnX(forcedColumn), topRowY, 0f);

            Block block = Instantiate(blockPrefab, pos, Quaternion.identity);
            block.Setup(turn, this);
            blocks.Add(block);

            emptyColumns.Remove(forcedColumn);
        }

        if (pickupPrefab != null && emptyColumns.Count > 0 && Random.value < pickupSpawnChance)
        {
            int pickupColumn = emptyColumns[Random.Range(0, emptyColumns.Count)];
            Vector3 pickupPos = new Vector3(GetColumnX(pickupColumn), topRowY, 0f);

            Pickup pickup = Instantiate(pickupPrefab, pickupPos, Quaternion.identity);
            pickup.Setup(GetRandomPickupType(), this);
            pickups.Add(pickup);
        }
    }

    private void SpawnPickupAt(int columnIndex)
    {
        Vector3 pos = new Vector3(GetColumnX(columnIndex), topRowY, 0f);
        Pickup pickup = Instantiate(pickupPrefab, pos, Quaternion.identity);
        pickup.Setup(GetRandomPickupType(), this);
        pickups.Add(pickup);
    }

    private PickupType GetRandomPickupType()
    {
        int roll = Random.Range(0, 100);

        if (roll < 40) return PickupType.AddBallPermanent; // 40%
        if (roll < 50) return PickupType.RowBlast;         // 10%
        if (roll < 60) return PickupType.ColumnBlast;      // 10%
        if (roll < 70) return PickupType.BallPlusSpell;    // 10%
        if (roll < 78) return PickupType.BallMinusSpell;   // 8%
        if (roll < 89) return PickupType.SpeedSpell;       // 11%

        return PickupType.ScatterSpell;                    // 11%
    }

    public void CollectPickup(Pickup pickup, Ball triggeringBall)
    {
        switch (pickup.Type)
        {
            case PickupType.AddBallPermanent:
                ballCount += 1;
                break;

            case PickupType.RowBlast:
                DamageRowAt(pickup.transform.position.y, 1);
                break;

            case PickupType.ColumnBlast:
                DamageColumnAt(pickup.transform.position.x, 1);
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

        pickups.Remove(pickup);
        Destroy(pickup.gameObject);
    }

    private void DamageRowAt(float y, int damage)
    {
        List<Block> snapshot = new List<Block>(blocks);

        foreach (Block block in snapshot)
        {
            if (block == null) continue;

            if (Mathf.Abs(block.transform.position.y - y) <= rowStep * 0.35f)
            {
                block.Hit(damage);
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
                block.Hit(damage);
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
        foreach (Block block in blocks)
        {
            if (block != null && block.transform.position.y <= launcherY + 0.6f)
            {
                Debug.Log("GAME OVER");

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPaused = true;
#endif
                isShooting = true;
                break;
            }
        }
    }

    private void UpdateAimLine(Vector2 dir)
    {
        if (aimLine == null) return;

        aimLine.SetPosition(0, launcher.position);
        aimLine.SetPosition(1, launcher.position + (Vector3)(dir * 3f));
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + turn;
        }
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
}