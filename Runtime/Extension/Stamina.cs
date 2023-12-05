using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GameEngine
{
    public class Stamina : MonoBehaviour
    {
        [HideInInspector]public bool alwaysGain;
        [HideInInspector]public float stamina = 1000f;
        [HideInInspector]public float minStamina = 0f;
        [HideInInspector]public float maxStamina = 1000f;
        [HideInInspector]public float staminaGainRate = 20f;
        [HideInInspector]public float staminaLossRate = 15f;
        [HideInInspector]public float staminaThreshold = 1f;
        [HideInInspector]public AnimationCurve staminaGainCurve;
        [HideInInspector]public AnimationCurve staminaLossCurve;
        [HideInInspector]public float cooldown = 1.5f;
        [HideInInspector]public float cooldownFactor = 2f;

        public float gain 
        {
            get
            {
                float gain = 0;
                foreach (var elem in m_gainData)
                {
                    gain += elem.Value;
                }

                return staminaGainRate + gain;
            }
        }

        public float loss 
        {
            get
            {
                float loss = 0;
                foreach(var elem in m_drainData) 
                { 
                    loss += elem.Value; 
                }

                return staminaLossRate + loss;
            }
        }


        [Header("Events")]
        [HideInInspector]public UnityEvent<float> onStaminaChange = new UnityEvent<float>();
        [HideInInspector]public UnityEvent<float> onStaminaInsufficient = new UnityEvent<float>();

        public bool isChanging { get; internal set; }  
        public bool isEmpty { get { return stamina <= 0f; } }
        public bool isCooldown { get; internal set; }

        private float m_lastStamina;
        private Dictionary<string, float> m_drainData = new Dictionary<string, float>();
        private Dictionary<string, float> m_gainData = new Dictionary<string, float>();
        private int m_alwaysGainState = 1;

        private void Update()
        {
            CalculateStamina();
        }

        #region PUBLIC
        public void SetRandom()
        {
            stamina = Random.Range(minStamina, maxStamina);
        }

        public bool Use(float stamina)
        {
            if(this.stamina >= stamina && !isCooldown)
            {
                this.stamina -= stamina;
                return true;
            }
            else
            {
                onStaminaInsufficient.Invoke(this.stamina - stamina);
                return false;
            }
        }

        public bool Use(float stamina, float seconds)
        {
            if(this.stamina > stamina && !isCooldown)
            {
                StartCoroutine(Loop(stamina, seconds));
                return true;
            }
            else
            {
                onStaminaInsufficient.Invoke(this.stamina - stamina);
                return false;
            }
        }

        public float GetStaminaFillAmount(float scale = 100)
        {
            return Mathf.Clamp01(stamina / maxStamina) * scale;
        }
        #endregion

        #region GAIN
        public void AddGain(string key, float value)
        {
            if(value == 0 || isCooldown) return;

            if(!m_gainData.ContainsKey(key))
                m_gainData.Add(key, value);
        }

        public bool ContainGain(string key)
        {
            return m_gainData.ContainsKey(key) && !isCooldown;
        }

        public void RemoveGain(string key)
        {
            if (!m_gainData.ContainsKey(key)) return;

            m_gainData.Remove(key);
        }
        #endregion

        #region DRAIN
        public void AddDrain(string key, float value)
        {
            if (value == 0 || isCooldown) return;

            if(!m_drainData.ContainsKey(key))
                m_drainData.Add(key, value);

            m_alwaysGainState = m_drainData.Count > 0 ? 0 : 1;
        }

        public bool ContainDrain(string key)
        {
            return m_drainData.ContainsKey(key) && !isCooldown;
        }

        public void RemoveDrain(string key) 
        {
            if (!m_drainData.ContainsKey(key)) return;

            m_drainData.Remove(key);
            m_alwaysGainState = m_drainData.Count > 0 ? 0 : 1;
        }
        #endregion

        #region PRIVATE
        private void CalculateStamina()
        {
            if (isEmpty && !isCooldown)
            {
                StartCoroutine(StartCoolDown());
            }

            if (staminaGainRate != staminaLossRate)
            {
                float gainFactor = staminaGainCurve.Evaluate(GetStaminaFillAmount(1f));
                float lossFactor = staminaLossCurve.Evaluate(GetStaminaFillAmount(1f));

                m_alwaysGainState = alwaysGain ? 1 : m_alwaysGainState;

                stamina += staminaGainRate * Time.unscaledDeltaTime * gainFactor * m_alwaysGainState;
                stamina -= staminaLossRate * Time.unscaledDeltaTime * lossFactor;
            }

            foreach (var element in m_drainData)
            {
                stamina -= element.Value * Time.unscaledDeltaTime;
            }

            foreach (var element in m_gainData)
            {
                stamina += element.Value * Time.unscaledDeltaTime;
            }

            stamina = Mathf.Clamp(stamina, minStamina, maxStamina);

            float delta = Mathf.Abs(m_lastStamina - stamina);
            float thresholdDelta = staminaThreshold * Time.deltaTime;

            if (m_lastStamina != stamina && delta > thresholdDelta)
            {
                isChanging = true;
                onStaminaChange.Invoke(delta);
            }
            else
            {
                isChanging = false;
            }

            m_lastStamina = stamina;
        }

        private IEnumerator Loop(float v, float t)
        {
            float amount = v;

            while(amount > 0) { 
                yield return new WaitForSecondsRealtime(v/t);
                amount -= v / t;
                stamina -= v / t;
            }
        }

        private IEnumerator StartCoolDown()
        {
            isCooldown = true;
            m_drainData.Clear();
            m_alwaysGainState = 1;
            yield return new WaitForSecondsRealtime(cooldown);
            isCooldown = false;

            Debug.Log("On CoolDown",gameObject);
        }
        #endregion
    }
}
