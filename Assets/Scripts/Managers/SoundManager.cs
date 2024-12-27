using TMPro;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] TMP_Text toggleText;
    bool isPlaying = false;

    public void ToggleSound() {
        // TODO handle sound on/off logic
        toggleText.text = isPlaying ? "sound on" : "sound off";
        isPlaying = !isPlaying;
    }   
}
