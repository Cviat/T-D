using System.Collections;
using UnityEngine;

namespace RPGTable.Runtime
{
    [CreateAssetMenu(menuName = "RPG Table/Animations/Defense Shield", fileName = "DefenseShieldAnimation")]
    public sealed class DefenseShieldAnimation : CombatAnimationEffect
    {
        public Sprite shieldSprite;
        public float duration = 0.6f;
        public float maxScale = 1.2f;

        public override IEnumerator PlayRoutine(Transform attacker, Transform target, System.Action onImpact)
        {
            // Здесь target — это тот, кто защищается
            if (target == null || shieldSprite == null)
            {
                onImpact?.Invoke();
                yield break;
            }

            int maxSortingOrder = 50;
            var targetSRs = target.GetComponentsInChildren<SpriteRenderer>();
            foreach (var tsr in targetSRs)
            {
                if (tsr != null && tsr.sortingOrder > maxSortingOrder)
                {
                    maxSortingOrder = tsr.sortingOrder;
                }
            }

            GameObject shieldGo = new GameObject("DefenseShield", typeof(SpriteRenderer));
            shieldGo.transform.position = target.position + new Vector3(0, 0.2f, -1.0f);
            shieldGo.transform.localScale = Vector3.zero;
            shieldGo.layer = target.gameObject.layer;

            var sr = shieldGo.GetComponent<SpriteRenderer>();
            sr.sprite = shieldSprite;
            sr.sortingOrder = maxSortingOrder + 10;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                if (shieldGo != null)
                {
                    float s = Mathf.Sin(progress * Mathf.PI) * maxScale;
                    shieldGo.transform.localScale = new Vector3(s, s, 1f);

                    Color c = sr.color;
                    if (progress > 0.7f)
                    {
                        c.a = Mathf.Lerp(1f, 0f, (progress - 0.7f) / 0.3f);
                    }
                    sr.color = c;
                }
                yield return null;
            }

            onImpact?.Invoke();
            Destroy(shieldGo);
        }
    }
}
