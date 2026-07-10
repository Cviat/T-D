using System;

namespace RPGTable.Core
{
    [Serializable]
    public sealed class ActiveStatusEffect
    {
        public string effectName; // "Poison", "Stun", "Freeze", "Slow", "ExtraRoll", "Vampirism"
        public CombatAttributeStat affectedStat;
        public int value; // strength or magnitude of the effect
        public int durationTurns; // remaining duration in turns
    }
}
