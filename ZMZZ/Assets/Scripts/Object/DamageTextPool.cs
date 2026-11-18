using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance {  get; private set; }

    public DamageText textPrefab;
    private Queue<DamageText> pool = new Queue<DamageText>();

    private void Awake()
    {
        Instance = this;
    }

    public void ShowDamage(int damage,Color color,Vector3 position)
    {
        DamageText text;
        if(pool.Count > 0)
        {
            text = pool.Dequeue();
        }
        else
        {
            text = Instantiate(textPrefab, transform);
        }
        text.Play(damage, color, position, Recycle);
    }

    private void Recycle(DamageText text)
    {
        pool.Enqueue(text);
    }

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}
