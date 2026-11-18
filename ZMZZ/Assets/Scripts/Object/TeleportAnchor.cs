using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportAnchor : MonoBehaviour
{
    public string id;
    public string displayName;
    public string sceneName;

    public SpriteRenderer sr;

    public bool isActivated;
    public bool isPlayerInRange;

    public Transform anchorTrs;

    private void Awake()
    {
        if(sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }
    }
    private void Start()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = this.gameObject.name;
        }
        if(string.IsNullOrEmpty(sceneName))
        {
            sceneName = this.gameObject.scene.name;
        }
        anchorTrs = GetComponent<Transform>();
        isActivated = GameDataMgr.Instance.worldData.IsAnchorActive(id);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerObject>() != null)
        {
            isPlayerInRange = true;
        }
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerObject>() != null) { 
            isPlayerInRange = false;   
        }
    }


    private void Update()
    {
        if (!isPlayerInRange) return;
        if (!isActivated && Input.GetKeyDown(KeyCode.F))
        {
            bool changed = GameDataMgr.Instance.worldData.ActivateAnchor(id, displayName, sceneName , anchorTrs.position.x,anchorTrs.position.y );
            if (changed)
            {
                isActivated = true;
                GameDataMgr.Instance.SaveWorldData();


                Debug.Log("传送点激活");
            }
        }


    }


}


