using System;

namespace RPGTable.Core
{
    [Serializable]
    public sealed class ActiveStatusEffect
    {
        public string effectName; // "Poison", "Stun", "Freeze", "Slow", "ExtraRoll", "Vampirism"
        public string affectedStat; // "HP", "MovementPoints", "Rolls", "Armor"
        public int value; // strength or magnitude of the effect
        public int durationTurns; // remaining duration in turns
        public bool appliedToSelf; // true if buff for self, false if debuff for target
    }
}
