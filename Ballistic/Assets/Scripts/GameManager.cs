using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public Ball ballPrefab;
    public Block blockPrefab;

    [Header("Scene refs")]
    public Transform launcher;
    public LineRenderer aimLine;

    [Header("Board")]
    public int columns = 7;
    public float columnWidth = 1.2f;
    public float topRowY = 7f;
    public float rowStep = 1.2f;
    public float sideLimit = 4.2f;
    public float launcherY = -7f;

    [Header("Shot")]
    public int ballCount = 10;
    public float ballSpeed = 13f;
    public float delayBetweenBalls = 0.06f;
    public float minAimY = 0.15f;

    private Camera cam;
    private bool isShooting;
    private int activeBalls;
    private float firstReturnedX;
    private bool gotFirstReturn;

    private int turn = 1;
    private List<Block> blocks = new List<Block>();

    public float LauncherY => launcherY;

    void Start()
    {
        cam = Camera.main;
        launcher.position = new Vector3(0f, launcherY, 0f);

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.enabled = true;
        }

        SpawnRow();
        SpawnRow();
    }

    void Update()
    {
        if (isShooting) return;

        Vector2 dir = GetAimDirection();
        UpdateAimLine(dir);

        if (Input.GetMouseButtonUp(0))
        {
            StartCoroutine(FireBalls(dir));
        }
    }

    Vector2 GetAimDirection()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorld - launcher.position;

        if (dir.y < minAimY)
            dir.y = minAimY;

        return dir.normalized;
    }

    IEnumerator FireBalls(Vector2 dir)
    {
        isShooting = true;
        activeBalls = ballCount;
        gotFirstReturn = false;

        for (int i = 0; i < ballCount; i++)
        {
            Ball ball = Instantiate(ballPrefab, launcher.position, Quaternion.identity);
            ball.Init(this);
            ball.Launch(dir, ballSpeed);

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

        Destroy(ball.gameObject);
        activeBalls--;

        if (activeBalls <= 0)
        {
            EndTurn();
        }
    }

    void EndTurn()
    {
        launcher.position = new Vector3(firstReturnedX, launcherY, 0f);

        MoveBlocksDown();
        turn++;
        SpawnRow();
        CheckGameOver();

        isShooting = false;
    }

    void MoveBlocksDown()
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

    void SpawnRow()
    {
        bool spawnedAny = false;

        for (int c = 0; c < columns; c++)
        {
            if (Random.value < 0.55f)
            {
                Vector3 pos = new Vector3(GetColumnX(c), topRowY, 0f);
                Block block = Instantiate(blockPrefab, pos, Quaternion.identity);
                block.Setup(turn, this);
                blocks.Add(block);
                spawnedAny = true;
            }
        }

        if (!spawnedAny)
        {
            int c = Random.Range(0, columns);
            Vector3 pos = new Vector3(GetColumnX(c), topRowY, 0f);
            Block block = Instantiate(blockPrefab, pos, Quaternion.identity);
            block.Setup(turn, this);
            blocks.Add(block);
        }
    }

    float GetColumnX(int columnIndex)
    {
        return -((columns - 1) * columnWidth * 0.5f) + (columnIndex * columnWidth);
    }

    public void NotifyBlockDestroyed(Block block)
    {
        blocks.Remove(block);
    }

    void CheckGameOver()
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

    void UpdateAimLine(Vector2 dir)
    {
        if (aimLine == null) return;

        aimLine.SetPosition(0, launcher.position);
        aimLine.SetPosition(1, launcher.position + (Vector3)(dir * 3f));
    }
}