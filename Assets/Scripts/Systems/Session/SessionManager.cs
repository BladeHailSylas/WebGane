using UnityEngine;

namespace Intents
{
    public class SessionManager
    {
        public SessionPlayerInfo playerInfo;
        public SessionManager(byte sessionID = 1)
        {
            Debug.Log($"Hello Player {sessionID}");
            playerInfo = new SessionPlayerInfo(sessionID);
        }
    }
}