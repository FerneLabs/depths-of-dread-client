using UnityEngine;

public class CoinScript : MonoBehaviour
{
    [SerializeField] float minScale = 0.8f;
    [SerializeField] float maxScale = 1f;
    [SerializeField] float scaleSpeed = 1f;
    float scaleTime;

    // Update is called once per frame
    void Update()
    {
        scaleTime += Time.deltaTime * scaleSpeed;
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(scaleTime) + 1f) / 2f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
