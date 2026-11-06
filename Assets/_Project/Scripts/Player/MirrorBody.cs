using UnityEngine;

namespace MysticCircus
{
    // Keeps a MirrorOnly body standing where the player stands, so only mirrors can see it.
    public sealed class MirrorBody : MonoBehaviour
    {
        public FirstPersonPlayer Player;

        void LateUpdate()
        {
            if (Player == null) return;
            transform.position = new Vector3(Player.transform.position.x, 0, Player.transform.position.z);
            transform.rotation = Quaternion.Euler(0, Player.transform.eulerAngles.y, 0);
        }
    }
}
