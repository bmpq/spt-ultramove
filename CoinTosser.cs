using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ultramove
{
    public class CoinTosser : MonoBehaviour
    {
        Camera cam;

        private GameObject prefabCoin;
        private Rigidbody playerrb;

        Material trailMat;

        private void Start()
        {
            cam = Camera.main;
            playerrb = GetComponent<Rigidbody>();
            trailMat = new Material(Shader.Find("Sprites/Default"));
        }

        public void SetPrefab(GameObject coin)
        {
            prefabCoin = coin;
        }

        public void Toss()
        {
            Vector3 newPos = cam.transform.position + (cam.transform.forward * 0.5f) + (cam.transform.up * -0.4f);

            GameObject newCoin = Instantiate(prefabCoin, newPos, transform.rotation);
            newCoin.GetComponent<TrailRenderer>().material = trailMat;

            newCoin.transform.localScale = Vector3.one * 4f;

            Rigidbody newRb = newCoin.GetComponent<Rigidbody>();

            Vector3 vel = (transform.forward * 2.5f) + (transform.up * 2f);
            vel *= 7f;
            newRb.velocity = playerrb.velocity + vel;
            newRb.maxAngularVelocity = 15;
            newRb.AddRelativeTorque(new Vector3(15, 0, 0), ForceMode.VelocityChange);
            newRb.angularDrag = 0;
        }
    }
}