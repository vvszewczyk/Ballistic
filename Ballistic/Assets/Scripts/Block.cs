using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Transform rotatablePart;

    [Header("Triangle text")]
    [SerializeField] private Vector3 triangleTextOffset = new Vector3(-0.12f, -0.12f, 0f);

    private int hp;
    private GameManager gameManager;

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
        hp -= damage;

        if (hp <= 0)
        {
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
}