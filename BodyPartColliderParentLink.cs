using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal class BodyPartColliderParentLink : MonoBehaviour
    {
        Transform virtualParent;

        Vector3 virtualLocalPos;

        void Start()
        {
            virtualParent = transform.parent;
            virtualLocalPos = transform.localPosition;
            transform.SetParent(null, true);
            gameObject.layer = 16;
        }

        void Update()
        {
            transform.position = virtualParent.TransformPoint(virtualLocalPos);
        }
    }
}
