using System.Collections;
using UnityEngine;

namespace MysticCircus
{
    public sealed class PuppetStageController : RoomController
    {
        public Transform Beam;
        public Transform Puppet;
        public GameObject[] LeftWeights;
        public GameObject[] RightWeights;
        int left;
        int right;
        bool performing;
        public override string Objective => IsSolved ? "The scale is balanced. Exit, or use the optional SHOW console." :
            "Use the puppet console to add weights. Balance both pans at 2 weights each. Left: " + left + " / Right: " + right;

        public override void Interact(string action, int value)
        {
            if (action == "show")
            {
                if (IsSolved && !performing) StartCoroutine(Show());
                else Message = IsSolved ? "The puppet is already performing." : "Balance the scale before the show.";
                return;
            }
            if (IsSolved) return;
            if (action == "left") left = (left + 1) % 4;
            if (action == "right") right = (right + 1) % 4;
            for (int i = 0; i < 3; i++) { LeftWeights[i].SetActive(i < left); RightWeights[i].SetActive(i < right); }
            Beam.localRotation = Quaternion.Euler(0, 0, (right - left) * 9);
            Message = "The puppet moves a weight. Left " + left + " / Right " + right;
            if (left == 2 && right == 2) Solve("The scale settles. The stage exit opens.");
        }

        IEnumerator Show()
        {
            performing = true;
            Message = "A short performance, just for you.";
            Quaternion origin = Puppet.localRotation;
            for (float time = 0; time < 5; time += Time.deltaTime)
            {
                Puppet.localRotation = origin * Quaternion.Euler(Mathf.Sin(time * 4) * 20, 0, Mathf.Sin(time * 2) * 15);
                yield return null;
            }
            Puppet.localRotation = origin;
            performing = false;
        }
    }
}
