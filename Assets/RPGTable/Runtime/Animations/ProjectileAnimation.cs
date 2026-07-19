using System.Collections;
using UnityEngine;

namespace RPGTable.Runtime
{
    [CreateAssetMenu(menuName = "RPG Table/Animations/Projectile", fileName = "ProjectileAnimation")]
    public sealed class ProjectileAnimation : CombatAnimationEffect
    {
        public Sprite projectileSprite;
        public Sprite stuckSprite; // Спрайт стрелы после попадания
        public float speed = 12f;
        public float durationAfterHit = 0.4f;
        public float scale = 0.5f;

        public override IEnumerator PlayRoutine(Transform attacker, Transform target, System.Action onImpact)
        {
            if (attacker == null || target == null || projectileSprite == null)
            {
                onImpact?.Invoke();
                yield break;
            }

            int maxSortingOrder = 50;
            var attackerSRs = attacker.GetComponentsInChildren<SpriteRenderer>();
            foreach (var asr in attackerSRs)
            {
                if (asr != null && asr.sortingOrder > maxSortingOrder)
                {
                    maxSortingOrder = asr.sortingOrder;
                }
            }
            var targetSRs = target.GetComponentsInChildren<SpriteRenderer>();
            foreach (var tsr in targetSRs)
            {
                if (tsr != null && tsr.sortingOrder > maxSortingOrder)
                {
                    maxSortingOrder = tsr.sortingOrder;
                }
            }

            // Создаем временный объект для летящей стрелы
            GameObject projGo = new GameObject("CombatProjectile", typeof(SpriteRenderer));
            projGo.transform.position = attacker.position + new Vector3(0, 0, -1.5f);
            projGo.transform.localScale = Vector3.one * scale;
            projGo.layer = attacker.gameObject.layer;

            var sr = projGo.GetComponent<SpriteRenderer>();
            sr.sprite = projectileSprite;
            sr.sortingOrder = maxSortingOrder + 20; // Рисуем поверх фишек

            Vector3 startPos = attacker.position + new Vector3(0, 0, -1.5f);
            Vector3 targetPos = target.position + new Vector3(0, 0, -1.5f);

            // Разворачиваем снаряд в сторону цели
            Vector3 diff = targetPos - startPos;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            projGo.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Полет
            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / speed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                if (projGo != null)
                {
                    projGo.transform.position = Vector3.Lerp(startPos, targetPos, progress);
                }
                yield return null;
            }

            if (projGo != null)
            {
                projGo.transform.position = targetPos;
                if (stuckSprite != null)
                {
                    sr.sprite = stuckSprite;
                }
            }

            // Наносим урон/звук в момент попадания снаряда
            onImpact?.Invoke();

            // Стрела торчит в цели какое-то время, а потом плавно исчезает
            float fadeElapsed = 0f;
            while (fadeElapsed < durationAfterHit)
            {
                fadeElapsed += Time.deltaTime;
                if (projGo != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, fadeElapsed / durationAfterHit);
                    sr.color = c;
                }
                yield return null;
            }

            Destroy(projGo);
        }
    }
}
