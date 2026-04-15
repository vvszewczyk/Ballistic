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
    [SerializeField] private SpriteRenderer spriteRenderer;

    private MeshRenderer labelRenderer;

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

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (label != null)
        {
            labelRenderer = label.GetComponent<MeshRenderer>();

            if (labelRenderer != null)
            {
                labelRenderer.sortingLayerName = "Default";
                labelRenderer.sortingOrder = 10;
            }

            label.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            label.transform.localRotation = Quaternion.identity;
            label.transform.localScale = Vector3.one;
        }
    }

    public void Setup(PickupType pickupType, GameManager manager)
    {
        type = pickupType;
        gameManager = manager;
        RefreshVisuals();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ball ball = other.GetComponent<Ball>();
        if (ball == null) return;

        if (!IsRoundPersistentTrigger() && consumed) return;

        if (IsRoundPersistentTrigger())
            activatedThisRound = true;
        else
            consumed = true;

        gameManager.CollectPickup(this, ball);
    }

    private void RefreshVisuals()
    {
        if (label != null)
        {
            label.text = GetSymbol(type);
            label.color = GetLabelColor(type);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = GetPickupColor(type);
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 0;
        }
    }

    private string GetSymbol(PickupType pickupType)
    {
        switch (pickupType)
        {
            case PickupType.AddBallPermanent: return "+";
            case PickupType.RowBlast:         return "—";
            case PickupType.ColumnBlast:      return "|";
            case PickupType.Bomb:             return "B";
            case PickupType.Cluster:          return "C";
            case PickupType.CrossBlast:       return "✚";
            case PickupType.Sniper:           return "N";
            case PickupType.BallPlusSpell:    return "B+";
            case PickupType.BallMinusSpell:   return "B-";
            case PickupType.SpeedSpell:       return "F";
            case PickupType.ScatterSpell:     return "S";
        }

        return "?";
    }

    private Color GetPickupColor(PickupType pickupType)
    {
        switch (pickupType)
        {
            case PickupType.AddBallPermanent: return new Color(0.25f, 0.85f, 0.35f);
            case PickupType.RowBlast:         return new Color(0.95f, 0.85f, 0.20f);
            case PickupType.ColumnBlast:      return new Color(0.20f, 0.90f, 0.95f);
            case PickupType.Bomb:             return new Color(0.90f, 0.25f, 0.25f);
            case PickupType.Cluster:          return new Color(0.70f, 0.30f, 0.90f);
            case PickupType.CrossBlast:       return new Color(1.00f, 0.55f, 0.20f);
            case PickupType.Sniper:           return new Color(1.00f, 0.35f, 0.70f);
            case PickupType.BallPlusSpell:    return new Color(0.45f, 1.00f, 0.55f);
            case PickupType.BallMinusSpell:   return new Color(0.85f, 0.45f, 0.15f);
            case PickupType.SpeedSpell:       return new Color(0.25f, 0.55f, 1.00f);
            case PickupType.ScatterSpell:     return new Color(0.60f, 0.90f, 1.00f);
        }

        return Color.white;
    }

    private Color GetLabelColor(PickupType pickupType)
    {
        return Color.white;
    }
}