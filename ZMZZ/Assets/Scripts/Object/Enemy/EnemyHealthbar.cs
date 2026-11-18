using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthbar : MonoBehaviour
{
    public Enemy enemy;
    public Image fill;

    public Vector3 offset = new Vector3(0f, 1.2f, 0f);
    public CanvasGroup group;
    public bool hideWhenFull;


    private void Awake()
    {
        if (enemy != null)
        {
            enemy = GetComponentInParent<Enemy>();
        }
        group = GetComponent<CanvasGroup>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (enemy == null || fill == null) return;
        fill.transform.position = transform.position + offset;
        fill.transform.rotation = transform.rotation;
        (fill.transform as RectTransform).sizeDelta = new Vector2((float)enemy.hp / enemy.maxHp*10, 1);

        if (group != null && hideWhenFull)
        { 
            group.alpha = enemy.hp >= enemy.maxHp ? 0f : 1f;
        }
    }
}
