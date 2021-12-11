using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragHelpPanel : MonoBehaviour
{
    [SerializeField] private GameObject developerConsolePanel;

    private void Update()
    {
        transform.GetChild(0).gameObject.SetActive(developerConsolePanel.activeSelf);
    }
}
