using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public string sceneToLoad;
    public Animator[] transitionImages;
    public Vector2 playerPositionInNewScene;
    private Transform playerTransform;

    public float timeToWait = 1f;

    private void Start()
    {
        // 检查是否有待处理的场景切换
        if (GameManager.hasPendingTransition)
        {
            GameManager.hasPendingTransition = false;

            // 新场景加载后，在过渡动画期间确保透明度为1，然后播放FadeOut
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }

            foreach (Animator transitionImage in transitionImages)
            {
                if (transitionImage != null)
                {
                    transitionImage.Play("FadeOut");
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)

    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            // 触发场景切换事件
            GameManager.TriggerSceneTransition();
            foreach (Animator transitionImage in transitionImages)
            {
                if (transitionImage != null)
                {
                    transitionImage.Play("FadeIn");
                }
            }
            StartCoroutine(LoadScene());
        }
    }

    IEnumerator LoadScene()
    {
        yield return new WaitForSeconds(timeToWait);
        if (playerTransform != null)
        {
            playerTransform.position = playerPositionInNewScene;
        }
        SceneManager.LoadScene(sceneToLoad);
    }
}
