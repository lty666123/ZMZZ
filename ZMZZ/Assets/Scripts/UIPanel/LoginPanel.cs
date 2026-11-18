using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : BasePanel
{
    public InputField inputUserName;
    public InputField inputPassword;
    public Button btnLogin;
    public Button btnRegister;
    public LoginTest loginTest;
    

    public Button btnRegisterCertain;
    public InputField inputNickname;
   
    public override void Init()
    {

        btnRegisterCertain.gameObject.SetActive(false);
        inputNickname.gameObject.SetActive(false);
        btnLogin.gameObject.SetActive(true);
        btnRegister.interactable = true;

        loginTest = FindFirstObjectByType<LoginTest>();
    
        if(loginTest == null )
        {
            Debug.LogError("场景中没有找到 LoginTest");
            btnLogin.interactable = false;
            return;
        }
        loginTest.LoginSucceed += OnLoginSuccess;
        loginTest.RegisterSucceed += OnRegisterSucced;

        //登录按钮点击
        btnLogin.onClick.AddListener(() =>
        {
            string userName = inputUserName.text.Trim();
            string password = inputPassword.text;
            if(string.IsNullOrEmpty(userName)||string.IsNullOrEmpty(password))
            {
                Debug.Log("账号密码不能为空");
                return;
            }
            loginTest.OnLoginClicked(userName, password);
            
        });


        btnRegister.onClick.AddListener(() => 
        {
           
            btnRegisterCertain.gameObject.SetActive(true);
            inputNickname.gameObject.SetActive(true);
            btnLogin.gameObject.SetActive(false);

            inputNickname.text = "";
            inputPassword.text = "";
            inputUserName.text = "";

            btnRegister.interactable = false;
            btnRegisterCertain.onClick.AddListener(() => {
                
                string userName = inputUserName.text.Trim();
                string password = inputPassword.text;
                string nickname = inputNickname.text.Trim();
                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)||string.IsNullOrEmpty(nickname))
                {
                    Debug.Log("账号密码不能为空");
                    return;
                }
                loginTest.OnRegisterClicked(nickname, userName, password);
            });

        });


    }

    //登录成功要做的事
    private void OnLoginSuccess()
    {
        UIManger.Instance.HidePanel<LoginPanel>();
        UIManger.Instance.ShowPanel<BeginPanel>();
    }
    //注册成功要做的事
    private void OnRegisterSucced()
    {
        btnRegisterCertain.gameObject.SetActive(false);
        inputNickname.gameObject.SetActive(false);
        btnLogin.gameObject.SetActive(true);
        btnRegister.interactable = true;
    }
    private void OnDestroy()
    {
        loginTest.LoginSucceed -= OnLoginSuccess;
        loginTest.RegisterSucceed -= OnRegisterSucced;
    }
}

