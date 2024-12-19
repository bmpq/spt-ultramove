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

        List<Coin> coinPool;

        private void Start()
        {
            coinPool = new List<Coin>();
            cam = Camera.main;
            playerrb = GetComponent<Rigidbody>();
            trailMat = new Material(Shader.Find("Sprites/Default"));
        }

        public void SetPrefab(GameObject coin, Texture mainTex)
        {
            prefabCoin = coin;

            MeshRenderer meshRend = prefabCoin.GetComponent<MeshRenderer>();
            meshRend.sharedMaterial = new Material(Shader.Find("p0/Reflective/Bumped Emissive Specular SMap"));
            meshRend.sharedMaterial.mainTexture = mainTex;
            meshRend.sharedMaterial.SetTexture("_EmissionMap", mainTex);
        }

        public void Toss()
        {
            Vector3 newPos = cam.transform.position + (cam.transform.forward * 0.8f) + (cam.transform.up * -0.7f);

            Coin coin = GetCoin();

            Rigidbody newRb = coin.GetComponent<Rigidbody>();

            coin.transform.position = newPos;
            coin.transform.rotation = cam.transform.rotation;
            newRb.position = newPos;
            newRb.rotation = cam.transform.rotation;

            Vector3 vel = (cam.transform.forward * 2.5f) + (cam.transform.up * 2f);
            vel *= 7f;
            newRb.velocity = playerrb.velocity + vel;
            newRb.maxAngularVelocity = 15;
            newRb.AddRelativeTorque(new Vector3(15, 0, 0), ForceMode.VelocityChange);
            newRb.angularDrag = 0;

            coin.Activate();
        }

        Coin GetCoin()
        {
            foreach (var item in coinPool)
            {
                if (!item.active)
                    return item;
            }

            GameObject newCoin = Instantiate(prefabCoin);
            newCoin.GetComponent<TrailRenderer>().material = trailMat;
            newCoin.transform.localScale = Vector3.one * 4f;

            coinPool.Add(newCoin.GetOrAddComponent<Coin>());

            return coinPool[coinPool.Count - 1];
        }
    }
}