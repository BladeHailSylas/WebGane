namespace Intents
{
    public class SessionManager
    {
        public SessionPlayerInfo playerInfo;
        public SessionManager(byte sessionID = 1)
        {
            playerInfo = new SessionPlayerInfo(sessionID);
        }
    }
}