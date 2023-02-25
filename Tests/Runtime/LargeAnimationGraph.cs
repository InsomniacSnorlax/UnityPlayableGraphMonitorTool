using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Tests
{
    [RequireComponent(typeof(Animator))]
    public class LargeAnimationGraph : MonoBehaviour
    {
        public AnimationClip Clip;

        public byte Depth = 4;

        public byte Branch = 3;

        public bool ExtraLabel = false;

        private PlayableGraph _graph;

        private readonly Dictionary<PlayableHandle, string> _extraLabelTable = new Dictionary<PlayableHandle, string>();


        public void RecreatePlayableGraph()
        {
            _extraLabelTable.Clear();

            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _graph = PlayableGraph.Create(nameof(LargeAnimationGraph));

            var animOutput = AnimationPlayableOutput.Create(_graph, "AnimOutput", GetComponent<Animator>());
            if (Depth == 0)
            {
                _graph.Play();
                return;
            }

            if (Depth == 1)
            {
                var animPlayable = AnimationClipPlayable.Create(_graph, Clip);
                animOutput.SetSourcePlayable(animPlayable);
                _extraLabelTable.Add(animPlayable.GetHandle(), "Depth=0,Branch=0");
                _graph.Play();

                return;
            }

            var rootMixer = AnimationMixerPlayable.Create(_graph);
            animOutput.SetSourcePlayable(rootMixer);
            _extraLabelTable.Add(rootMixer.GetHandle(), "Depth=0,Branch=0");

            CreatePlayableTree(rootMixer, 1);

            _graph.Play();
        }

        private void CreatePlayableTree(Playable parent, int parentDepth)
        {
            // Don't handle root node
            Assert.IsTrue(parent.IsValid());
            Assert.IsTrue(parentDepth > 0);

            // No leaf node
            if (parentDepth == Depth)
            {
                return;
            }

            // Leaf nodes
            if (parentDepth == Depth - 1)
            {
                for (int b = 0; b < Branch; b++)
                {
                    var anim = AnimationClipPlayable.Create(_graph, Clip);
                    parent.AddInput(anim, 0, 1f / Branch);
                    _extraLabelTable.Add(anim.GetHandle(), $"Depth={parentDepth + 1},Branch={b}");
                }

                return;
            }

            for (int b = 0; b < Branch; b++)
            {
                var mixer = AnimationMixerPlayable.Create(_graph);
                parent.AddInput(mixer, 0, 1f / Branch);
                _extraLabelTable.Add(mixer.GetHandle(), $"Depth={parentDepth + 1},Branch={b}");

                CreatePlayableTree(mixer, parentDepth + 1);
            }
        }

        private void UpdateNodeExtraLabelTable()
        {
#if UNITY_EDITOR
            var table = ExtraLabel ? _extraLabelTable : null;
            PlayableGraphMonitorWindow.TrySetNodeExtraLabelTable(table);
#endif
        }


        private void OnValidate()
        {
            if (_graph.IsValid())
            {
                RecreatePlayableGraph();
                UpdateNodeExtraLabelTable();
            }
        }

        private void Start()
        {
            RecreatePlayableGraph();
            UpdateNodeExtraLabelTable();
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
    }
}