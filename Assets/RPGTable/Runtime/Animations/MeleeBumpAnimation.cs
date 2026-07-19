using System.Collections;
using UnityEngine;

namespace RPGTable.Runtime
{
    [CreateAssetMenu(menuName = "RPG Table/Animations/Melee Bump", fileName = "MeleeBumpAnimation")]
    public sealed class MeleeBumpAnimation : CombatAnimationEffect
    {
        public float speed = 15f;
        public float distanceFraction = 0.5f;

        public override IEnumerator PlayRoutine(Transform attacker, Transform target, System.Action onImpact)
        {
            if (attacker == null || target == null)
            {
                onImpact?.Invoke();
                yield break;
            }

            Vector3 startPos = attacker.position;
            Vector3 targetPos = target.position;
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            Vector3 bumpPos = startPos + direction * (distance * distanceFraction);

            // Движение вперед
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                attacker.position = Vector3.Lerp(startPos, bumpPos, t);
                yield return null;
            }

            // Вызываем воздействие (урон/звук попадания) на пике рывка
            onImpact?.Invoke();

            // Движение назад
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                attacker.position = Vector3.Lerp(bumpPos, startPos, t);
                yield return null;
            }

            attacker.position = startPos;
        }
    }
}
