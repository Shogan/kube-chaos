using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour {

    // Quick and dirty singleton instance
    public static ObjectPoolManager instance;

    public GameObject beamPrefab;

    public int numBeamInstancesToPool;

    public static List<GameObject> beamPool;

    [SerializeField]
    private string pooledObjectFolderName = "InitialPooledObjects";

    private GameObject pooledObjectFolder;

    // Use this for initialization
    private void Start()
    {
        instance = this;

        if (!string.IsNullOrEmpty(pooledObjectFolderName))
        {
            pooledObjectFolder = GameObject.Find(pooledObjectFolderName);
            if (pooledObjectFolder == null)
                pooledObjectFolder = new GameObject(pooledObjectFolderName);
        }


        beamPool = new List<GameObject>();

        for (var i = 1; i <= numBeamInstancesToPool; i++)
        {
            var beamBullet = (GameObject)Instantiate(beamPrefab);

            SetParentTransform(beamBullet);

            beamBullet.SetActive(false);
            beamPool.Add(beamBullet);
        }
    }

    private void SetParentTransform(GameObject gameObjectRef)
    {
        if (pooledObjectFolder != null)
        {
            gameObjectRef.transform.parent = pooledObjectFolder.transform;
        }
    }

    public GameObject GetUsableBeam2Bullet()
    {
        var obj = (from item in beamPool
                   where item.activeSelf == false
                   select item).FirstOrDefault();

        if (obj != null)
        {
            return obj;
        }

        Debug.Log("<color=orange>WARNING: Ran out of reusable bullet objects. Now instantiating a new one</color>");
        var beam2Bullet = (GameObject)Instantiate(instance.beamPrefab);

        SetParentTransform(beam2Bullet);

        beam2Bullet.SetActive(false);
        beamPool.Add(beam2Bullet);

        return beam2Bullet;
    }
}
