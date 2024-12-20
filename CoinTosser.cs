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

        private GameObject prefabGlint;

        Material trailMat;

        List<Coin> coinPool;
        List<GameObject> glintPool;

        private void Start()
        {
            coinPool = new List<Coin>();
            glintPool = new List<GameObject>();
            cam = Camera.main;
            playerrb = GetComponent<Rigidbody>();
            trailMat = new Material(Shader.Find("Sprites/Default"));
        }

        public void SetPrefab(GameObject coin, Texture mainTex, GameObject glint)
        {
            prefabCoin = coin;

            MeshRenderer meshRend = prefabCoin.GetComponent<MeshRenderer>();
            meshRend.sharedMaterial = new Material(Shader.Find("p0/Reflective/Bumped Emissive Specular SMap"));
            meshRend.sharedMaterial.mainTexture = mainTex;
            meshRend.sharedMaterial.SetTexture("_EmissionMap", mainTex);

            prefabGlint = glint;
            prefabGlint.GetComponentInChildren<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            prefabGlint.GetComponentInChildren<MeshRenderer>().sharedMaterial.color = new Color(1, 0.9f, 0.7f, 1);
        }

        private void Update()
        {
            // Add additional glint objects to the pool if needed
            while (glintPool.Count < Coin.activeCoins.Count)
            {
                GameObject newGlint = Instantiate(prefabGlint);
                glintPool.Add(newGlint);
            }

            int activeGlintIndex = 0;

            foreach (Coin coin in Coin.activeCoins)
            {
                if (coin.IsOnApex())
                {
                    GameObject glint = glintPool[activeGlintIndex];
                    glint.transform.position = coin.transform.position;
                    glint.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
                    glint.transform.localScale = Vector3.one * 0.5f;
                    activeGlintIndex++;
                }
            }

            // Hide unused glint objects in the pool
            for (int i = activeGlintIndex; i < glintPool.Count; i++)
            {
                glintPool[i].transform.position = new Vector3(0, -900, 0);
            }
        }

        GameObject GetGlint()
        {
            foreach (GameObject glint in glintPool)
            {
                if (!glint.activeSelf)
                    return glint;
            }

            glintPool.Add(Instantiate(prefabGlint));
            return glintPool[glintPool.Count - 1];
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
            newRb.maxAngularVelocity = 25;
            newRb.AddRelativeTorque(new Vector3(25, 0, 0), ForceMode.VelocityChange);
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
            newCoin.transform.localScale = Vector3.one * 3f;

            coinPool.Add(newCoin.GetOrAddComponent<Coin>());

            return coinPool[coinPool.Count - 1];
        }
    }
}