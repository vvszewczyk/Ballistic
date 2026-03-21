using TMPro;
using UnityEngine;

public enum PickupType
{
    AddBallPermanent,
    RowBlast,
    ColumnBlast,
    BallPlusSpell,
    BallMinusSpell,
    SpeedSpell,
    ScatterSpell
}

public class Pickup : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private PickupType type;
    private GameManager gameManager;
    private bool consumed;

    public PickupType Type => type;

    private void Awake()
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);
    }

    public void Setup(PickupType pickupType, GameManager manager)
    {
        type = pickupType;
        gameManager = manager;
        RefreshLabel();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        Ball ball = other.GetComponent<Ball>();
        if (ball == null) return;

        consumed = true;
        gameManager.CollectPickup(this, ball);
    }

    private void RefreshLabel()
    {
        if (label == null) return;

        switch (type)
        {
            case PickupType.AddBallPermanent:
                label.text = "+";
                break;
            case PickupType.RowBlast:
                label.text = "<->";
                break;
            case PickupType.ColumnBlast:
                label.text = "|";
                break;
            case PickupType.BallPlusSpell:
                label.text = "B+";
                break;
            case PickupType.BallMinusSpell:
                label.text = "B-";
                break;
            case PickupType.SpeedSpell:
                label.text = "SPD";
                break;
            case PickupType.ScatterSpell:
                label.text = "S";
                break;
        }
    }
}