using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class NodeEnterProximity : MonoBehaviour
{
    public string nodeName;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            KubeManager.Instance.currentNode = nodeName;
            SceneManager.LoadScene("Node");
        }
    }
}
