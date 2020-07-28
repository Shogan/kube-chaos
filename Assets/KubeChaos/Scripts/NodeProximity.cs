using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeProximity : MonoBehaviour
{
    public Node nodeRef;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            Debug.Log(collision.gameObject.name);
            nodeRef.OpenDoor();
        }
        
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            Debug.Log(collision.gameObject.name);
            nodeRef.CloseDoor();
        }
        
    }
}
