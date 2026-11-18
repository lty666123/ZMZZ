using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiddenLevelSpwan : MonoBehaviour
{
    public Transform pos;
    private bool isPlayerInRange;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isPlayerInRange&&Input.GetKeyUp(KeyCode.W))
        {
            PlayerObject player  = FindObjectOfType<PlayerObject>();
            player.transform.position = pos.position;
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerObject>()!=null)
        {
            isPlayerInRange = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerObject>()!=null)
        {
            isPlayerInRange = false;
        }
    }
}
