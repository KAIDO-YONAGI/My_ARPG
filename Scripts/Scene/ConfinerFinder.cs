using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;
using System.Collections;

public class ConfinerFinder : MonoBehaviour
{
    private CinemachineConfiner2D confiner;

    void Awake()
    {
        confiner = GetComponent<CinemachineConfiner2D>();
    }

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
        StartCoroutine(BindConfiner());
    }

    IEnumerator BindConfiner()
    {
        // 等一帧，确保场景物体初始化完成
        yield return null;

        GameObject obj = GameObject.FindGameObjectWithTag("Confiner");

        if (obj == null)
        {
            Debug.LogWarning("没找到 Confiner！");
            yield break;
        }

        Collider2D col = obj.GetComponent<Collider2D>();

        if (col == null)
        {
            Debug.LogWarning("Confiner 没有 Collider2D！");
            yield break;
        }

        confiner.m_BoundingShape2D = col;

        // ⭐ 必须刷新
        confiner.InvalidateCache();
    }
}