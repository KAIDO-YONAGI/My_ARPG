using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Contracts.Base;
using Contracts.Enums;
using Newtonsoft.Json;
using Pipeline.Scene;
using Pipeline.SO;
using Pipeline.SO.Events;
using UnityEngine;

namespace Gameplay.Save
{
    //TODO每次进入新场景需要自动备份上个场景的信息
//需要备份的信息包括 敌人状态（位置、血量）。物品状态（位置、数量），序列化到json中，下次进场景加载
//点击备份或者退出游戏的时候额外备份当前玩家所在场景、位置、血量
//点击加载的时候加载玩家的手动存档
//玩家可选加载手动存档进度、继续游戏（加载默认存档）、重新游戏（清空状态、直接加载场景，不依赖存档）


//目前存档和加载太频繁，需要批量处理 done


//SaveDataManager 组装 SaveData 对象，此处打包成 SaveFile 写盘存档

    public class SaveFile
    {
        public SaveMetaData saveInfo;
        public SaveData data;
        public SaveFile(SaveMetaData saveInfo, SaveData data)
        {
            this.saveInfo = saveInfo;
            this.data = data;
        }
    }
    public class SaveSystem : YSingleton<SaveSystem>
    {
        public bool IsLoadingSaveRequest { get; private set; }

        [Header("Receive")]

        [SerializeField] private DataSaveEventSO dataSavedEvent;
        private void OnEnable()
        {
            dataSavedEvent.DataSaveEvent += OnSaveEvent;
        }
        private void OnDisable()
        {
            dataSavedEvent.DataSaveEvent -= OnSaveEvent;
        }
        public void OnSaveEvent(SaveType saveType)//通过事件确认存档：以收到的Data保存完成事件为准（带存档类型）
        {
            WriteSave(saveType);
        }
        public string WriteSave(SaveType saveType)
        {
            string saveID = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            SaveMetaData saveInfo = new(saveID, saveType);
            SaveFile save = new(saveInfo, SaveDataManager.Instance.GetData);

            string json = JsonConvert.SerializeObject(save, Formatting.Indented);
            string path=Application.persistentDataPath + $"/{saveType}_{saveID}.json";
            File.WriteAllText(path, json);

#if UNITY_EDITOR
            Debug.Log("存档路径：" + path);
#endif

            return path;

        }
        public bool DeleteSave(string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning("删除存档失败：路径为空。");
#endif
                return false;
            }

            string fullSavePath = Path.GetFullPath(savePath);
            string fullPersistentPath = Path.GetFullPath(Application.persistentDataPath);
            if (!fullSavePath.StartsWith(fullPersistentPath, StringComparison.OrdinalIgnoreCase))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"存档文件不在持久化目录内：{savePath}");
#endif
                return false;
            }

            if (!File.Exists(fullSavePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"未找到存档文件：{savePath}");
#endif
                return false;
            }

            try
            {
                File.Delete(fullSavePath);
#if UNITY_EDITOR
                Debug.Log("删除路径：" + fullSavePath);
#endif
                return true;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"删除存档失败：{savePath}（{e.Message}）");
#endif
                return false;
            }
        }


        public bool LoadSave(SaveType saveType, string savePath)
        {
            if (string.IsNullOrEmpty(savePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"读取存档失败：{saveType} 的路径为空。");
#endif
                return false;
            }

            if (!File.Exists(savePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"未找到存档文件：{savePath}");
#endif
                return false;
            }

            if (!IsLoadableSaveFile(savePath))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"存档文件不可加载：{savePath}");
#endif
                return false;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                SaveFile save = JsonConvert.DeserializeObject<SaveFile>(json);

                SaveDataManager.Instance.LoadFromData(save.data);
                //触发加载场景
                GameSceneSO gameScene = GetScene(save.data);
                if (gameScene != null)
                {
                    // 某些旧档只缺位置时，仍允许用场景默认出生点继续游戏。
                    Vector3 pos = save.data.sceneIDAndPlayerPos != null && save.data.sceneIDAndPlayerPos.position != null
                        ? save.data.sceneIDAndPlayerPos.position.ToVector3()//如果存档位置为空，会加载到场景的默认位置
                        : gameScene.initialPosition;
                    IsLoadingSaveRequest = true;
                    // 标志窗口包住整个入口调用：SaveDataManager.OnAutoSave 在广播段内读到 true，
                    // 读档触发的切换不会写自动存档
                    SceneChanger.Instance.RequestSceneLoad(new List<GameSceneSO> { gameScene }, pos, true);
                    IsLoadingSaveRequest = false;
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log("存档中的场景标识无效，无法加载。");
#endif
                    return false;
                }

#if UNITY_EDITOR
                Debug.Log("读取路径：" + savePath);
#endif
                return true;
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"读取存档失败：{savePath}（{e.Message}）");
#endif
            }


            return false;
        }
        public string GetLatestLoadableSavePath(SaveType saveType)
        {
            string[] files = GetSavesPath(saveType);
            if (files.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("未找到存档文件。");
#endif
                return null;
            }

            //文件名格式: {saveType}_{yyyyMMdd_HHmmss_fff}.json，按文件名排序最后一个即最新
            Array.Sort(files);
            // 最新文件可能是坏档，这里回退到最近一个结构完整且能定位场景的档。
            string latestFile = files.LastOrDefault(IsLoadableSaveFile);
            if (string.IsNullOrEmpty(latestFile))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"未找到可用的 {saveType} 存档。");
#endif
                return null;
            }

            return latestFile;
        }
        public string[] GetSavesPath(SaveType saveType)
        {
            return Directory.GetFiles(Application.persistentDataPath, $"{saveType}_*.json");
        }
        private bool IsLoadableSaveFile(string saveFile)
        {
            try
            {
                // Continue 按钮只尝试读取至少包含场景和角色数据的系统档。
                string json = File.ReadAllText(saveFile);
                SaveFile save = JsonConvert.DeserializeObject<SaveFile>(json);
                return save?.data != null
                       && save.data.playerStatsData != null
                       && GetScene(save.data) != null;
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"跳过无效存档：{saveFile}（{e.Message}）");
#endif
                return false;
            }
        }

        private GameSceneSO GetScene(SaveData data)
        {
            string sceneID = data?.sceneIDAndPlayerPos?.sceneID;
            if (string.IsNullOrEmpty(sceneID) || SceneDataForSave.Instance == null || SceneDataForSave.Instance.gameScenes == null)
            {
                return null;
            }

            foreach (GameSceneSO scene in SceneDataForSave.Instance.gameScenes.ToList())
            {
                if (scene != null && sceneID == scene.SaveKey) return scene;
            }
            return null;
        }
    }
}