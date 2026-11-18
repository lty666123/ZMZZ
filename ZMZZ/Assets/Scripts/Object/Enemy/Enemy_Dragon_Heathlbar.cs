using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy_Dragon_Heathlbar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 30f, 0);

    public Enemy_Dragon enemy_Dragon;

    public Image blackHp;
    public Image redHp;

    public float width;

    private void Awake()
    {
        enemy_Dragon = GetComponentInParent<Enemy_Dragon>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void LateUpdate()
    {
        if (enemy_Dragon == null) return;
        redHp.transform.position = this.transform.position + offset;
        blackHp.transform.position = this.transform.position + offset;
        redHp.transform.rotation = transform.rotation;
        blackHp.transform.rotation = transform.rotation;

        (redHp.transform as RectTransform).sizeDelta = new Vector2((float)enemy_Dragon.nowHp / enemy_Dragon.maxHp * width,1);
        //redHp.fillAmount = (float)enemy_Dragon.nowHp / enemy_Dragon.maxHp;
    }
}
