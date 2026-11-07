using UnityEngine;

namespace MysticCircus
{
    // The rule sheet on a wall. Using it opens the full-screen reading view; every unit's copy shows the same marks.
    [RequireComponent(typeof(Interactable))]
    public sealed class RuleBoard : MonoBehaviour
    {
        void Awake() => GetComponent<Interactable>().Used.AddListener(RuleSheetView.Open);
    }
}
