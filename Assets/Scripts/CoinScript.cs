using UnityEngine;

public class CoinScript : MonoBehaviour
{
    [SerializeField] float minScale = 0.8f;
    [SerializeField] float maxScale = 1f;
    [SerializeField] float minRotation = 0f;
    [SerializeField] float maxRotation = 360f;
    [SerializeField] float rotationSpeed = 1f;
    [SerializeField] float scaleSpeed = 1f;
    float scaleTime;
    float rotationTime;

    // Update is called once per frame
    void Update()
    {
        // Increment time based on animation speed
        scaleTime += Time.deltaTime * scaleSpeed;


        // Calculate scale using a sine wave for eased movement
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(scaleTime) + 1f) / 2f);


        // Apply scale to the coin
        transform.localScale = new Vector3(scale, scale, 1f);

    }
}
