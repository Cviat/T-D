using System.Collections;
using UnityEngine;

namespace RPGTable.Runtime
{
    /// <summary>
    /// Animates an attack "bump" towards a target position and returns to the original position.
    /// </summary>
    public sealed class TokenAttackAnimator : MonoBehaviour
    {
        private const float BumpSpeed = 15f;
        private const float BumpDistanceFraction = 0.5f;

        public void AnimateAttack(Vector3 targetWorldPos)
        {
            StartCoroutine(BumpRoutine(targetWorldPos));
        }

        private IEnumerator BumpRoutine(Vector3 targetPos)
        {
            Vector3 startPos = transform.position;
            Vector3 direction = (targetPos - startPos).normalized;
            
            // Move half the distance to the target
            float distance = Vector3.Distance(startPos, targetPos);
            Vector3 bumpPos = startPos + direction * (distance * BumpDistanceFraction);
            
            // Forward phase
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * BumpSpeed;
                transform.position = Vector3.Lerp(startPos, bumpPos, t);
                yield return null;
            }

            // Return phase
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * BumpSpeed;
                transform.position = Vector3.Lerp(bumpPos, startPos, t);
                yield return null;
            }

            transform.position = startPos;
            Destroy(this);
        }

        public void AnimateDamage(Vector3 attackerWorldPos)
        {
            StartCoroutine(DamageRoutine(attackerWorldPos));
        }

        private IEnumerator DamageRoutine(Vector3 attackerPos)
        {
            // Delay slightly so the damage happens when the attacker connects
            yield return new WaitForSeconds(1f / BumpSpeed);

            Vector3 startPos = transform.position;
            Vector3 pushDirection = (startPos - attackerPos).normalized;
            
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            Color[] originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
            }

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Shake and push
                float magnitude = (1f - progress) * 0.2f;
                Vector3 shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized * magnitude;
                transform.position = startPos + pushDirection * (magnitude * 0.5f) + shake;
                
                // Blink Red
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].color = Color.Lerp(Color.red, originalColors[i], progress);
                }

                yield return null;
            }

            transform.position = startPos;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].color = originalColors[i];
            }
            Destroy(this);
        }
    }
}
