using System.Collections;
using UnityEngine;

namespace RPGTable.Runtime
{
    [CreateAssetMenu(menuName = "RPG Table/Animations/Magic Splash", fileName = "MagicSplashAnimation")]
    public sealed class MagicSplashAnimation : CombatAnimationEffect
    {
        public GameObject effectPrefab; // Временный префаб эффекта
        public float duration = 1.0f;
        public Vector3 spawnOffset = new Vector3(0, 0, -2.0f);

        public override IEnumerator PlayRoutine(Transform attacker, Transform target, System.Action onImpact)
        {
            if (target == null)
            {
                onImpact?.Invoke();
                yield break;
            }

            GameObject spawnedEffect = null;
            if (effectPrefab != null)
            {
                spawnedEffect = Instantiate(effectPrefab, target.position + spawnOffset, Quaternion.identity);
                SetLayerRecursively(spawnedEffect, target.gameObject.layer);
                
                // Находим максимальный sortingOrder цели
                int maxSortingOrder = 50;
                var targetSRs = target.GetComponentsInChildren<SpriteRenderer>();
                foreach (var tsr in targetSRs)
                {
                    if (tsr != null && tsr.sortingOrder > maxSortingOrder)
                    {
                        maxSortingOrder = tsr.sortingOrder;
                    }
                }
                
                // Применяем sortingOrder ко всем Renderer (включая SpriteRenderer, ParticleSystemRenderer и др.)
                var spawnedRenderers = spawnedEffect.GetComponentsInChildren<Renderer>();
                foreach (var r in spawnedRenderers)
                {
                    if (r != null)
                    {
                        r.sortingOrder = maxSortingOrder + 30;
                    }
                }
            }

            // Магия наносит урон/звук практически сразу
            onImpact?.Invoke();

            if (spawnedEffect != null)
            {
                yield return new WaitForSeconds(duration);
                Destroy(spawnedEffect);
            }
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, newLayer);
                }
            }
        }
    }
}
