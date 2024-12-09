using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField] private GameObject[] uiElements;

    private void Awake() 
    {
        if (instance == null) 
        {
            instance = this;
        }
    }

    public void SetActive(string[] tags, bool enable)
    {
        foreach (var tag in tags)
        {
            foreach (var element in uiElements)
            {
                if (element.CompareTag(tag)) element.SetActive(enable);
            }
        }
    }

    public void SetText(string tag, string text)
    {
        foreach (var element in uiElements)
        {
            if (element.CompareTag(tag)) element.GetComponent<TextMeshProUGUI>().text = text;
        }
    }

}
