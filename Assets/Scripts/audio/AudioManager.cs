using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AudioInfo
{
    public string audioName;

    public AudioSource audioSource;
}
public class AudioManager : SingletonPersistent<AudioManager>
{
    // 保存所有BGM的音频信息
    public List<AudioInfo> bgmAudioInfoList;

    // 保存所有SFX的音频信息
    public List<AudioInfo> sfxAudioInfoList;

    // 音频管理的全局音量
    public float mainVolume;

    // BGM的音量因子，最终音量 = mainVolume * bgmVolumeFactor
    public float bgmVolumeFactor;

    // SFX的音量因子，最终音量 = mainVolume * sfxVolumeFactor
    public float sfxVolumeFactor;

    // 保存音频数据的资源
    public AudioDatas audioDatas;

    private GameObject _bgmSourcesRootGO;

    private GameObject _sfxSourcesRootGO;

    protected override void Awake()
    {
        base.Awake();

        // 创建BGM和SFX的AudioSource父级容器
        _bgmSourcesRootGO = new GameObject("BGM_ROOT");

        _sfxSourcesRootGO = new GameObject("SFX_ROOT");

        _bgmSourcesRootGO.transform.SetParent(transform);

        _sfxSourcesRootGO.transform.SetParent(transform);
    }

    /// <summary>
    /// 播放BGM
    /// </summary>
    /// <param name="fadeInMusicName">需要淡入的音乐名称</param>
    /// <param name="fadeOutMusicName">需要淡出的音乐名称，默认值为空字符串</param>
    /// <param name="fadeInDuration">淡入持续时间</param>
    /// <param name="fadeOutDuration">淡出持续时间</param>
    /// <param name="loop">是否循环播放</param>
    public void PlayBgm(string fadeInMusicName, string fadeOutMusicName = "", float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f, bool loop = true)
    {
        // 创建DoTween序列，用于播放过渡动画
        Sequence s = DOTween.Sequence();

        // 如果需要淡出某个BGM
        if (fadeOutMusicName != "")
        {
            AudioInfo fadeOutInfo = bgmAudioInfoList.Find(x => x.audioName == fadeOutMusicName);

            if (fadeOutInfo == null)
            {
                Debug.LogWarning("当前未加载" + fadeOutMusicName + ",无法进行淡出操作");
                return;
            }

            s.Append(fadeOutInfo.audioSource.DOFade(0, fadeOutDuration).OnComplete(() =>
            {
                fadeOutInfo.audioSource.Pause();
            }));
        }

        // 如果需要播放的音乐已经存在，则直接恢复播放
        AudioInfo audioInfo = bgmAudioInfoList.Find(x => x.audioName == fadeInMusicName);

        if (audioInfo != null)
        {
            s.Append(audioInfo.audioSource.DOFade(mainVolume * bgmVolumeFactor, fadeInDuration).OnComplete(() =>
            {
                audioInfo.audioSource.Play();
            }));

            return;
        }

        // 从资源中查找需要播放的音乐数据
        AudioData fadeInData = audioDatas.audioDataList.Find(x => x.audioName == fadeInMusicName);

        if (fadeInData == null)
        {
            Debug.LogWarning("音频资源SO中未找到名称为" + fadeInMusicName + "的音频数据");
            return;
        }

        // 动态创建一个GameObject并添加AudioSource，加载音频资源
        GameObject fadeInAudioGO = new GameObject(fadeInMusicName);

        fadeInAudioGO.transform.SetParent(_bgmSourcesRootGO.transform);

        AudioSource fadeInAudioSource = fadeInAudioGO.AddComponent<AudioSource>();

        fadeInAudioSource.clip = Resources.Load<AudioClip>(fadeInData.audioPath);

        fadeInAudioSource.loop = loop;

        // 设置音效的音量
        fadeInAudioSource.volume = mainVolume * bgmVolumeFactor;

        fadeInAudioSource.Play();
        
        if (fadeInDuration > 0)
        {
            fadeInAudioSource.volume = 0;
            s.Append(fadeInAudioSource.DOFade(mainVolume * bgmVolumeFactor, fadeInDuration));
        }
        AudioInfo info = new AudioInfo();

        // 将新创建的BGM信息添加到列表
        info.audioName = fadeInMusicName;

        info.audioSource = fadeInAudioSource;

        bgmAudioInfoList.Add(info);

        StartCoroutine(DetectingAudioPlayState(info, true));
    }


    /// <summary>
    /// 暂停BGM
    /// </summary>
    /// <param name="pauseBgmName">需要暂停的BGM名称</param>
    /// <param name="fadeOutDuration">淡出持续时间</param>
    public void PauseBgm(string pauseBgmName, float fadeOutDuration = 0.5f)
    {
        AudioInfo audioInfo = bgmAudioInfoList.Find(x => x.audioName == pauseBgmName);

        if (audioInfo == null)
        {
            Debug.LogWarning("当前未加载" + pauseBgmName + ",无法进行停止操作");
            return;
        }

        Sequence s = DOTween.Sequence();

        s.Append(audioInfo.audioSource.DOFade(0, fadeOutDuration).OnComplete(() =>
        {
            audioInfo.audioSource.Pause();
        }));
    }


    /// <summary>
    /// 停止BGM
    /// </summary>
    /// <param name="stopBgmName">需要停止的BGM名称</param>
    /// <param name="fadeOutDuration">淡出持续时间</param>
    public void StopBgm(string stopBgmName, float fadeOutDuration = 0.5f)
    {
        AudioInfo audioInfo = bgmAudioInfoList.Find(x => x.audioName == stopBgmName);

        if (audioInfo == null)
        {
            Debug.LogWarning("当前未加载" + stopBgmName + ",无法进行停止操作");
            return;
        }

        Sequence s = DOTween.Sequence();

        s.Append(audioInfo.audioSource.DOFade(0, fadeOutDuration).OnComplete(() =>
        {
            audioInfo.audioSource.Stop();

            Destroy(audioInfo.audioSource.gameObject);
        }));

        bgmAudioInfoList.Remove(audioInfo);

    }
    
    /// <summary>
    /// 停止所有音乐
    /// </summary>
    /// <param name="fadeOutDuration">淡出持续时间</param>
    public void StopAllBGM(float fadeOutDuration = 0.5f)
    {
        // 停止所有 BGM
        foreach (var bgmInfo in bgmAudioInfoList.ToArray())
        {
            StopBgm(bgmInfo.audioName, fadeOutDuration);
        }
        StopAllCoroutines();
    }



    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="sfxName">需要播放的音效名称</param>
    /// <param name="fadeInDuration">音效淡入时间</param>
    /// <param name="loop">是否循环播放</param>
    public void PlaySfx(string sfxName, float fadeInDuration = 0, bool loop = false)
    {
        Sequence s = DOTween.Sequence();

        // 从资源中查找需要播放的音效数据
        AudioData sfxData = audioDatas.audioDataList.Find(x => x.audioName == sfxName);

        if (sfxData == null)
        {
            Debug.LogWarning("音频资源SO中未找到名称为" + sfxName + "的音频数据");
            return;
        }


        // 动态创建一个GameObject并添加AudioSource，加载音效资源
        GameObject sfxAudioGO = new GameObject(sfxName);

        sfxAudioGO.transform.SetParent(_sfxSourcesRootGO.transform);

        AudioSource sfxAudioSource = sfxAudioGO.AddComponent<AudioSource>();

        sfxAudioSource.clip = Resources.Load<AudioClip>(sfxData.audioPath);

        sfxAudioSource.loop = loop;

        // 设置音效的音量
        sfxAudioSource.volume = mainVolume * sfxVolumeFactor;

        sfxAudioSource.Play();
        
        if (fadeInDuration > 0)
        {
            sfxAudioSource.volume = 0;
            s.Append(sfxAudioSource.DOFade(mainVolume * sfxVolumeFactor, fadeInDuration));
        }
        
        AudioInfo info = new AudioInfo();

        // 将音效信息添加到列表
        info.audioName = sfxName;

        info.audioSource = sfxAudioSource;

        sfxAudioInfoList.Add(info);

        StartCoroutine(DetectingAudioPlayState(info, false));
        //ThreadPool.QueueUserWorkItem(new WaitCallback(DetectingAudioPlayState), sfxAudioGO);//?????????????????????????
    }

    /// <summary>
    /// ?????Ч
    /// </summary>
    /// <param name="pauseSfxName">???????Ч??</param>
    public void PauseSfx(string pauseSfxName)
    {
        AudioInfo audioInfo = sfxAudioInfoList.Find(x => x.audioName == pauseSfxName);

        if (audioInfo == null)
        {
            Debug.LogWarning("?????δ????" + pauseSfxName + ",??????????Ч");
            return;
        }

        audioInfo.audioSource.Pause();
    }


    /// <summary>
    /// ????Ч
    /// </summary>
    /// <param name="stopSfxName">??????Ч??</param>
    public void StopSfx(string stopSfxName)
    {
        AudioInfo audioInfo = bgmAudioInfoList.Find(x => x.audioName == stopSfxName);

        if (audioInfo == null)
        {
            Debug.LogWarning("?????δ????" + stopSfxName + ",???????????Ч");
            return;
        }

        audioInfo.audioSource.Stop();

        bgmAudioInfoList.Remove(audioInfo);

        Destroy(audioInfo.audioSource.gameObject);
    }


    /// <summary>
    /// ?????????
    /// </summary>
    /// <param name="volume"></param>
    public void ChangeMainVolume(float volume)
    {
        mainVolume = volume;

        foreach (var info in bgmAudioInfoList)
        {
            info.audioSource.volume = mainVolume * bgmVolumeFactor;

        }
        foreach (var info in sfxAudioInfoList)
        {
            info.audioSource.volume = mainVolume * sfxVolumeFactor;
        }
    }

    /// <summary>
    /// ???Bgm????????
    /// </summary>
    /// <param name="factor"></param>
    public void ChangeBgmVolume(float factor)
    {
        bgmVolumeFactor = factor;

        foreach (var info in bgmAudioInfoList)
        {
            info.audioSource.volume = mainVolume * bgmVolumeFactor;
        }
    }


    /// <summary>
    /// ???Sfx????????
    /// </summary>
    /// <param name="factor"></param>
    public void ChangeSfxVolume(float factor)
    {
        sfxVolumeFactor = factor;

        foreach (var info in sfxAudioInfoList)
        {
            info.audioSource.volume = mainVolume * sfxVolumeFactor;
        }
    }
    
    /// <summary>
    /// 监测音频播放状态，播放结束后销毁
    /// </summary>
    /// <param name="info">音频信息</param>
    /// <param name="isBgm">是否是BGM</param>
    IEnumerator DetectingAudioPlayState(AudioInfo info, bool isBgm)
    {
        AudioSource audioSource = info.audioSource;
        while (audioSource.isPlaying)
        {
            //if (audioSource == null)
            //{
            //    if (isBgm)
            //    {
            //        bgmAudioInfoList.Remove(info);
            //    }
            //    else
            //    {
            //        sfxAudioInfoList.Remove(info);
            //    }
            //    Destroy(info.audioSource.gameObject);
            //    yield return null;

            //}
            yield return null;
        }
        if (isBgm)
        {
            bgmAudioInfoList.Remove(info);
        }
        else
        {
            sfxAudioInfoList.Remove(info);
        }

    }
}
