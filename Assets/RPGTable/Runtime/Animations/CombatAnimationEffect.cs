using System.Collections;
using UnityEngine;

namespace RPGTable.Runtime
{
    public abstract class CombatAnimationEffect : ScriptableObject
    {
        // Метод, который будет переопределен в конкретных анимациях.
        // onImpact вызывается в момент нанесения урона/эффекта (например, в момент удара или попадания снаряда).
        public abstract IEnumerator PlayRoutine(Transform attacker, Transform target, System.Action onImpact);
    }
}
