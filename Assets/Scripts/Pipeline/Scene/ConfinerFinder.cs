using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pipeline.Scene
{
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

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(FindConfiner());
        }

        IEnumerator FindConfiner()
        {
            // 等一帧，确保场景物体初始化完成
            yield return null;

            GameObject obj = GameObject.FindGameObjectWithTag("Confiner");

            if (obj == null)
            {
#if UNITY_EDITOR

                Debug.LogWarning("Can't find Confiner");
#endif

                yield break;
            }

            Collider2D col = obj.GetComponent<Collider2D>();

            if (col == null)
            {
                Debug.LogWarning("No Confiner Collider2D");
                yield break;
            }

            confiner.m_BoundingShape2D = col;

            // ⭐ 必须刷新
            confiner.InvalidateCache();
        }
    }
}