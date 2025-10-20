using TMPro;
using UnityEngine;

public class StatusController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _statusText;

    void Awake()
    {
        ServiceLocator.Register<StatusController>(this);
    }

    public void ClearStatus()
    {
        SetStatus(string.Empty);
    }

    public void SetStatus(string status)
    {
        _statusText.text = status;
    }
}
