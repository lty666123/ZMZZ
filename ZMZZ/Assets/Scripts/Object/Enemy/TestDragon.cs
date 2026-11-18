using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDragon : MonoBehaviour
{
    public Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.J))
        {
            animator.SetTrigger("Atk1");
        }
        if(Input.GetKeyDown (KeyCode.K))
        {
            animator.SetTrigger("Atk2");
        }
    }
}
