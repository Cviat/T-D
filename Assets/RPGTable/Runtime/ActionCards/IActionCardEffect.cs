namespace RPGTable.Runtime.ActionCards
{
    public interface IActionCardEffect
    {
        string EffectId { get; }
        bool Execute(CampaignPlayerData caster, ActionCard card, UnityEngine.Vector2Int targetPosition, out string failReason);
    }
}
