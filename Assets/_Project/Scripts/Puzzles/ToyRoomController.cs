using UnityEngine;

namespace MysticCircus
{
    public sealed class ToyRoomController : RoomController
    {
        public AudioSource MusicBox;
        AudioClip[] notes;
        readonly int[] melody = { 0, 2, 4, 2 };
        readonly bool[] switches = new bool[3];
        int step;
        bool musicSolved;
        public override string Objective => IsSolved ? base.Objective :
            "Music box: C > E > G > E. Then follow the colored strings and activate RED, BLUE and YELLOW switches.";

        void Awake()
        {
            notes = new AudioClip[5];
            float[] frequencies = { 261.63f, 293.66f, 329.63f, 349.23f, 392f };
            for (int n = 0; n < notes.Length; n++)
            {
                float[] samples = new float[22050];
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = Mathf.Sin(2 * Mathf.PI * frequencies[n] * i / 44100f) * Mathf.Exp(-i / 5000f) * 0.25f;
                notes[n] = AudioClip.Create("Music box note " + n, samples.Length, 1, 44100, false);
                notes[n].SetData(samples, 0);
            }
        }

        public override void Interact(string action, int value)
        {
            if (IsSolved) return;
            if (action == "note")
            {
                MusicBox.PlayOneShot(notes[value]);
                if (!musicSolved)
                {
                    step = value == melody[step] ? step + 1 : 0;
                    musicSolved = step == melody.Length;
                }
                Message = musicSolved ? "The music box opens. Follow the colored strings." : "Melody: " + step + "/4";
            }
            if (action == "switch")
            {
                switches[value] = true;
                Message = "String switch activated. " + (switches[0] ? "RED " : "") +
                    (switches[1] ? "BLUE " : "") + (switches[2] ? "YELLOW" : "");
            }
            if (musicSolved && switches[0] && switches[1] && switches[2]) Solve("The final door opens. The night air waits outside.");
        }

        void OnDestroy()
        {
            if (notes != null) foreach (var note in notes) if (note != null) Destroy(note);
        }
    }
}
