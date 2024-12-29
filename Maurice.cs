using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal class Maurice : MonoBehaviour
    {
        Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();

            rb.useGravity = false;
        }

        void Update()
        {
            Vector3 targetDirection = EFTTargetInterface.GetPlayerPosition() - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 100f * Time.deltaTime);
        }

        void FixedUpdate()
        {
            float distToPlayer = Vector3.Distance(EFTTargetInterface.GetPlayerPosition(), transform.position);
            Vector3 targetVel = Vector3.zero;
            if (distToPlayer > 15f)
                targetVel = (EFTTargetInterface.GetPlayerPosition() - transform.position).normalized;

            if (DistanceToFloor() < 5f)
                targetVel.y = Mathf.Max(targetVel.y, 0);

            Debug.Log(DistanceToFloor());

            rb.velocity = targetVel;
        }

        float DistanceToFloor()
        {
            int layer1 = 1 << 18; // LowPolyCollider
            int layer2 = 1 << 11; // Terrain
            int layerMask = layer1 | layer2;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f, layerMask))
            {
                return Vector3.Distance(transform.position, hit.point);
            }

            return 100f;
        }
    }
}
