using TMPro;
using UnityEngine;
public class Sign : MonoBehaviour
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
        if (Input.GetKeyDown(GameKeys.Interact) && signEnabled)
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
