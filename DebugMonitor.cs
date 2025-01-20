using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tui;
using UnityEngine;

namespace ultramove
{
    internal class DebugMonitor : MonoBehaviour
    {
        Camera cam;

        void Start()
        {
            cam = Camera.main;
        }

        void OnGUI()
        {
            if (TryGetComponent<BoxCollider>(out BoxCollider box))
            {
                DebugRender.DrawBoxCollider(cam, box, Color.green);
            }
        }
    }
}
