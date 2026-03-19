using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfinerFinder : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Cinemachine.CinemachineConfiner2D confiner = GetComponent<Cinemachine.CinemachineConfiner2D>();
        if (confiner != null)
        {
            confiner.m_BoundingShape2D = GameObject.FindGameObjectWithTag("Confiner").GetComponent<PolygonCollider2D>();
        }
    }
}
