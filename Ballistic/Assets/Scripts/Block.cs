using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private TMP_Text hpText;

    private int hp;
    private GameManager gameManager;

    public void Setup(int value, GameManager manager)
    {
        hp = value;
        gameManager = manager;
        Refresh();
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