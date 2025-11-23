using System;
using Framework.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Tests.Runtime
{
    public class PolygonOffsetRaycaster : MonoBehaviour
    {
        private void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.value);
    
            if (ray.RaycastLayerOrder(out var hit, 100f, LayerPriorityOrder.Descending))
            {
                Debug.Log($"{hit.collider.name} (Layer: {hit.collider.gameObject.layer})");
            }
   
            
        }

    }
}