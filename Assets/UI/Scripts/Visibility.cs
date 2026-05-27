using UnityEngine;
using UnityEngine.UI;

public class Visibility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _image;
    protected bool targetValue;

    void Start() 
    {
        _image = GetComponent<Image>();
    }

    void LateUpdate()
    {
        UpdateOpacity(targetValue);
    }

    public void UpdateOpacity(bool display)
    {
        Color currentColor = _image.color;
        float transitionSpeed = 20f;
        float opacity = 1f;

        float targetOpacity = display ? opacity : 0f;
        currentColor.a = Mathf.Lerp(currentColor.a, targetOpacity, Time.deltaTime * transitionSpeed);
        _image.color = currentColor;
    }
}
