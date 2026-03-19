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

    public float timeToWait = 1f;

    private void Start()
    {
        // 检查是否有待处理的场景切换
        if (GameManager.transitionData.hasPendingTransition)
        {
            // 先保存玩家位置
            Vector2 playerPos = GameManager.transitionData.playerPosition;

            // 清空数据
            GameManager.transitionData = default(SceneTransitionData);

            // 设置玩家位置
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = playerPos;
            }

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
            // 保存目标位置到 GameManager
            GameManager.transitionData.playerPosition = playerPositionInNewScene;

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
        SceneManager.LoadScene(sceneToLoad);
    }
}
