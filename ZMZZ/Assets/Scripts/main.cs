using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //开始时加载BeginPanel
        //UIManger.Instance.ShowPanel<BeginPanel>();

        UIManger.Instance.ShowPanel<LoginPanel>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
