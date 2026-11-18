using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{

    public float moveSpeed = 1f;
    public float duration = 0.8f;

    public float timer;


    public TMP_Text textDamage;



    private Action<DamageText> OnFinished;

    public Color startColor;

    public void Play(int damage,Color color,Vector3 position,Action<DamageText> recycleAction)
    {
        timer = 0;
        transform.position = position;

        startColor = color;
        startColor.a = 1f;

        textDamage.text = damage.ToString();
        textDamage.color = startColor;
        
        OnFinished = recycleAction;
        gameObject.SetActive(true);

    }

    // Start is called before the first frame update
    private void Awake()
    {
        textDamage = GetComponent<TMP_Text>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        float progress = duration > 0 ? Mathf.Clamp01(timer / duration) : 1f;
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        Color color = startColor;
        color.a = 1f - progress;
        textDamage.color = color;
        
        if(progress>=1f)
        {
            Action<DamageText> callback = OnFinished;
            OnFinished = null;

            gameObject.SetActive(false);
            callback?.Invoke(this);
        }
    }
}
