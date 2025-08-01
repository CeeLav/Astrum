using Astrum.Client.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Astrum.Client
{
    public class NetWorkUI : MonoBehaviour
    {
        public GameObject First;
        public GameObject Second;
        public Text SelectRoom;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void Login()
        {
            
            GamePlayManager.Instance.Login("Player");
            First.SetActive(false);
        }

        public void CraeteRoom()
        {
            GamePlayManager.Instance.CreateRoom();
        }
        
        public void JoinRoom()
        {
            GamePlayManager.Instance.JoinRoom(SelectRoom.text);
        }
        
    }
}
