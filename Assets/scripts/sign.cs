using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class sign : MonoBehaviour
{
    public GameObject dialogBox;
    public TextMeshProUGUI dialogBoxText;
    public string signText;
    public bool signEnabled = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && signEnabled)
        {
            if (dialogBox != null && dialogBoxText != null)
            {
                if (dialogBox.activeSelf)
                {
                    dialogBox.SetActive(false);
                }
                else
                {
                    dialogBoxText.text = signText;
                    dialogBox.SetActive(true);
                }
            }
        }
       
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        signEnabled = true;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        signEnabled = false;
        if (dialogBox != null)
            dialogBox.SetActive(false);
    }
}
