using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KidGame.Core
{
    [Serializable]
    public class ParticleData
    {
        public string effectName;
        public string effectPath;
    }
    
    [Serializable]
    public class ParticleInfo
    {
        public string effectName;
        public ParticleSystem particleSystem;
        public Coroutine autoDestroyCoroutine;
    }
    
    public class ParticleManager : SingletonPersistent<ParticleManager>
    {
        public List<ParticleInfo> activeParticleEffects = new List<ParticleInfo>();
        public ParticleDatas particleDatas;
        private GameObject _particleRootGO;

        protected override void Awake()
        {
            base.Awake();
            
            _particleRootGO = new GameObject("PARTICLE_ROOT");
            _particleRootGO.transform.SetParent(transform);
        }

        #region �������ŷ���

        /// <summary>
        /// ����������Ч
        /// </summary>
        /// <param name="effectName">��Ч����</param>
        /// <param name="position">����λ��</param>
        /// <param name="rotation">������ת</param>
        /// <param name="parent">������</param>
        /// <param name="autoDestroy">�Ƿ��Զ�����</param>
        /// <param name="destroyDelay">�����ӳ�ʱ��</param>
        /// <param name="scale">���ű���</param>
        public ParticleSystem PlayEffect(string effectName,
            Vector3 position,
            Quaternion rotation = default,
            Transform parent = null,
            bool autoDestroy = true,
            float destroyDelay = -1f,
            Vector3? scale = null)
        {
            // ����Դ����������Ч
            ParticleData effectData = particleDatas.particleDataList.Find(x => x.effectName == effectName);

            if (effectData == null)
            {
                Debug.LogWarning("δ�ҵ�������Ч��" + effectName);
                return null;
            }

            // ����������Чʵ��
            GameObject effectGO = new GameObject(effectName);
            effectGO.transform.SetParent(parent != null ? parent : _particleRootGO.transform);
            effectGO.transform.position = position;
            effectGO.transform.rotation = rotation == default ? Quaternion.identity : rotation;

            if (scale.HasValue)
            {
                effectGO.transform.localScale = scale.Value;
            }
            
            GameObject prefab = Resources.Load<GameObject>(effectData.effectPath);
            if (prefab == null)
            {
                Debug.LogWarning($"������ЧԤ�������ʧ�ܣ�{effectData.effectPath}");
                Destroy(effectGO);
                return null;
            }

            GameObject instance = Instantiate(prefab, effectGO.transform);
            ParticleSystem particleSystem = instance.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = instance.AddComponent<ParticleSystem>();
            }
            
            ParticleInfo info = new ParticleInfo
            {
                effectName = effectName,
                particleSystem = particleSystem
            };

            particleSystem.Play();
            
            if (autoDestroy)
            {
                float delay = destroyDelay >= 0 ? destroyDelay : particleSystem.main.duration;
                info.autoDestroyCoroutine = StartCoroutine(AutoDestroyEffect(info, delay));
            }

            activeParticleEffects.Add(info);
            return particleSystem;
        }

        #endregion

        #region ����

        // ����1��������Ч���ƺ�λ��
        public ParticleSystem PlayEffect(string effectName, Vector3 position)
        {
            return PlayEffect(effectName, position, default, null, true, -1f, null);
        }

        // ����2��ָ��λ�ú���ת
        public ParticleSystem PlayEffect(string effectName, Vector3 position, Quaternion rotation)
        {
            return PlayEffect(effectName, position, rotation, null, true, -1f, null);
        }

        // ����3��ָ��������
        public ParticleSystem PlayEffect(string effectName, Vector3 position, Transform parent)
        {
            return PlayEffect(effectName, position, default, parent, true, -1f, null);
        }

        // ����4����ȫ�Զ������
        public ParticleSystem PlayEffect(string effectName,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            bool autoDestroy,
            float destroyDelay)
        {
            return PlayEffect(effectName, position, rotation, parent, autoDestroy, destroyDelay, null);
        }

        // ����5���Զ�������
        public ParticleSystem PlayEffect(string effectName, Vector3 position, Vector3 scale)
        {
            return PlayEffect(effectName, position, default, null, true, -1f, scale);
        }

        #endregion

        #region ��Ч����

        /// <summary>
        /// ֹͣ������Ч
        /// </summary>
        /// <param name="effectName">��Ч����</param>
        /// <param name="immediate">�Ƿ���������</param>
        public void StopEffect(string effectName, bool immediate = false)
        {
            ParticleInfo info = activeParticleEffects.Find(x => x.effectName == effectName);
            if (info == null)
            {
                Debug.LogWarning("δ�ҵ���Ծ��������Ч��" + effectName);
                return;
            }

            if (info.autoDestroyCoroutine != null)
            {
                StopCoroutine(info.autoDestroyCoroutine);
            }

            if (immediate)
            {
                Destroy(info.particleSystem.gameObject);
                activeParticleEffects.Remove(info);
            }
            else
            {
                info.particleSystem.Stop();
                StartCoroutine(AutoDestroyEffect(info, info.particleSystem.main.duration));
            }
        }

        /// <summary>
        /// ��ͣ������Ч
        /// </summary>
        /// <param name="effectName">��Ч����</param>
        public void PauseEffect(string effectName)
        {
            ParticleInfo info = activeParticleEffects.Find(x => x.effectName == effectName);
            if (info == null)
            {
                Debug.LogWarning("δ�ҵ���Ծ��������Ч��" + effectName);
                return;
            }

            info.particleSystem.Pause();
        }

        /// <summary>
        /// �ָ�����������Ч
        /// </summary>
        /// <param name="effectName">��Ч����</param>
        public void ResumeEffect(string effectName)
        {
            ParticleInfo info = activeParticleEffects.Find(x => x.effectName == effectName);
            if (info == null)
            {
                Debug.LogWarning("δ�ҵ���Ծ��������Ч��" + effectName);
                return;
            }

            info.particleSystem.Play();
        }

        /// <summary>
        /// ֹͣ����������Ч
        /// </summary>
        /// <param name="immediate">�Ƿ���������</param>
        public void StopAllEffects(bool immediate = false)
        {
            foreach (var info in activeParticleEffects.ToArray())
            {
                StopEffect(info.effectName, immediate);
            }
        }

        #endregion

        #region ��������

        /// <summary>
        /// �Զ�����������Ч
        /// </summary>
        private IEnumerator AutoDestroyEffect(ParticleInfo info, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (info.particleSystem != null)
            {
                Destroy(info.particleSystem.transform.parent.gameObject);
            }

            activeParticleEffects.Remove(info);
        }

        #endregion
    }
}