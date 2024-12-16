using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ultramove
{
    internal class DoorOpener : MonoBehaviour
    {
        int layerInteractive;
        int layerDoor;

        private void Start()
        {
            layerInteractive = LayerMask.NameToLayer("Interactive");
            layerDoor = LayerMask.NameToLayer("DoorLowPolyCollider");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.gameObject.layer == layerDoor)
            {
                if (collision.transform.parent.TryGetComponent<Door>(out Door door))
                {
                    if (door.DoorState == EDoorState.Locked ||
                        door.DoorState == EDoorState.Shut)
                        door.Interact(EFT.EInteractionType.Breach);
                }
            }
        }
    }
}
