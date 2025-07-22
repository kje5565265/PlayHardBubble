using UnityEngine;

namespace PlayHard
{
    public class Singleton<T> where T : class, new()
    {
        private static T _inst;

        public static T Instance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new T();
                }
                return _inst;
            }
        }

        public virtual void Initialize() { }
    }
    
    public class SingletonMonobehaviour<T> : MonoBehaviour where T : SingletonMonobehaviour<T>
    {
        public bool OptionDontDestory = false;
        private static T _inst;

        public static T Instance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = GameObject.FindObjectOfType(typeof(T)) as T;
                    if (_inst == null)
                    {
                        GameObject singleton = new GameObject();
                        _inst = singleton.AddComponent<T>();
                        _inst.name = string.Format("[{0}]", _inst.GetType().Name);
                    }
                }
                
                return _inst;
            }
        }

        virtual protected void Awake()
        {
            if (_inst == null)
            {
                _inst = this as T;
            }

            if (OptionDontDestory)
            {
                DontDestroyOnLoad(gameObject);
            }
        }        

        private void OnApplicationQuit()
        {
            _inst = null;
        }

        private void OnDestroy()
        {
            _inst = null;
        }
    }
}



