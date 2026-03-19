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
    private void OnTriggerEnter2D(Collider2D other)

    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;

            GetComponent<CanvasGroup>().alpha = 1;

            foreach (Animator transitionImage in transitionImages)
            {
                if (transitionImage != null)
                {
                    transitionImage.Play("Fade");
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
