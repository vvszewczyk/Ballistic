using TMPro;
using UnityEngine;

public enum PickupType
{
    AddBallPermanent,
    RowBlast,
    ColumnBlast,
    Bomb,
    Cluster,
    CrossBlast,
    Sniper,
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
    private bool activatedThisRound;

    public PickupType Type => type;

    public bool IsRoundPersistentTrigger()
    {
        return type == PickupType.RowBlast ||
               type == PickupType.ColumnBlast ||
               type == PickupType.Bomb ||
               type == PickupType.Cluster ||
               type == PickupType.CrossBlast ||
               type == PickupType.Sniper;
    }

    public bool ShouldExpireAtTurnEnd()
    {
        return IsRoundPersistentTrigger() && activatedThisRound;
    }

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
        Ball ball = other.GetComponent<Ball>();
        if (ball == null) return;

        if (!IsRoundPersistentTrigger() && consumed) return;

        if (IsRoundPersistentTrigger())
        {
            activatedThisRound = true;
        }
        else
        {
            consumed = true;
        }

        gameManager.CollectPickup(this, ball);
    }

    private void RefreshLabel()
    {
        if (label == null) return;

        switch (type)
        {
            case PickupType.AddBallPermanent: label.text = "+"; break;
            case PickupType.RowBlast:         label.text = "<->"; break;
            case PickupType.ColumnBlast:      label.text = "|"; break;
            case PickupType.Bomb:             label.text = "BMB"; break;
            case PickupType.Cluster:          label.text = "CLU"; break;
            case PickupType.CrossBlast:       label.text = "+"; break;
            case PickupType.Sniper:           label.text = "SNP"; break;
            case PickupType.BallPlusSpell:    label.text = "B+"; break;
            case PickupType.BallMinusSpell:   label.text = "B-"; break;
            case PickupType.SpeedSpell:       label.text = "SPD"; break;
            case PickupType.ScatterSpell:     label.text = "S"; break;
        }
    }
}