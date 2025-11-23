using System.Linq;
using UnityEngine;

namespace Framework.Extensions
{
    public enum LayerPriorityOrder
    {
        Ascending,  // 오름차순 (0, 1, 2... 순서로 우선)
        Descending  // 내림차순 (31, 30, 29... 순서로 우선)
    }
    
    public static class PhysicsExtensions
    {
        public static bool RaycastLayerOrder(this Ray ray, out RaycastHit hitInfo, float maxDistance, LayerPriorityOrder order = LayerPriorityOrder.Ascending)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            
            if (hits.Length == 0)
            {
                hitInfo = default;
                return false;
            }

            IOrderedEnumerable<RaycastHit> sortedHits;

            if (order == LayerPriorityOrder.Ascending)
            {
                // 낮은 번호(0번)가 우선
                sortedHits = hits.OrderBy(h => h.collider.gameObject.layer)
                    .ThenBy(h => h.distance);
            }
            else
            {
                // 높은 번호(31번)가 우선
                sortedHits = hits.OrderByDescending(h => h.collider.gameObject.layer)
                    .ThenBy(h => h.distance);
            }

            // 3. 1등 선택
            hitInfo = sortedHits.First();
            return true;
        }
    }
}